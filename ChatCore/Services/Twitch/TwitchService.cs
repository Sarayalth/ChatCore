using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using System.Timers;
using ChatCore.Interfaces;
using ChatCore.Models;
using ChatCore.Models.Twitch;
using ChatCore.Utilities;
using Microsoft.Extensions.Logging;

namespace ChatCore.Services.Twitch
{
    public class TwitchService : ChatServiceBase, IChatService
    {
	    private readonly ConcurrentDictionary<Assembly, Action<IChatService, string>> _rawMessageReceivedCallbacks;
	    private readonly ConcurrentDictionary<string, IChatChannel> _channels;

	    private readonly ILogger _logger;
	    private readonly TwitchMessageParser _messageParser;
	    private readonly TwitchDataProvider _dataProvider;
	    private readonly IWebSocketService _websocketService;
		private readonly IWebSocketService _pubsubWssService;
		private readonly IUserAuthProvider _authManager;

	    private readonly object _messageReceivedLock;
	    private readonly object _initLock;

	    private readonly string _anonUsername;
	    private string? _loggedInUsername;
	    private bool _isStarted;

	    private int _currentMessageCount;
	    private DateTime _lastResetTime = DateTime.UtcNow;
	    private readonly ConcurrentQueue<KeyValuePair<Assembly, string>> _textMessageQueue = new ConcurrentQueue<KeyValuePair<Assembly, string>>();
		private Timer _pubsubPingTimer = new Timer();
		private TwitchMessage _rewardMessage;

		private string UserName => string.IsNullOrEmpty(_authManager.Credentials.Twitch_OAuthToken) ? _anonUsername : "@";
	    private string OAuthToken => string.IsNullOrEmpty(_authManager.Credentials.Twitch_OAuthToken) ? string.Empty : _authManager.Credentials.Twitch_OAuthToken;

	    public ReadOnlyDictionary<string, IChatChannel> Channels { get; }
        public TwitchUser? LoggedInUser { get; internal set; }

        public string DisplayName { get; } = "Twitch";

        public event Action<IChatService, string> OnRawMessageReceived
        {
            add => _rawMessageReceivedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _rawMessageReceivedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        public TwitchService(ILogger<TwitchService> logger, TwitchMessageParser messageParser, TwitchDataProvider twitchDataProvider, IWebSocketService websocketService, IWebSocketService pubsubWssService, IUserAuthProvider authManager, Random rand)
        {
	        _logger = logger;
            _messageParser = messageParser;
            _dataProvider = twitchDataProvider;
            _websocketService = websocketService;
			_pubsubWssService = pubsubWssService;
			_authManager = authManager;

            _rawMessageReceivedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, string>>();
            _channels = new ConcurrentDictionary<string, IChatChannel>();
            _messageReceivedLock = new object();
            _initLock = new object();

            _anonUsername = $"justinfan{rand.Next(10000, 1000000)}".ToLower();

            Channels = new ReadOnlyDictionary<string, IChatChannel>(_channels);

            _authManager.OnCredentialsUpdated += _authManager_OnCredentialsUpdated;
            _websocketService.OnOpen += _websocketService_OnOpen;
            _websocketService.OnClose += _websocketService_OnClose;
            _websocketService.OnError += _websocketService_OnError;
            _websocketService.OnMessageReceived += _websocketService_OnMessageReceived;

			_pubsubWssService.OnOpen += _pubsubWssService_OnOpen;
			_pubsubWssService.OnClose += _pubsubWssService_OnClose;
			_pubsubWssService.OnError += _pubsubWssService_OnError;
			_pubsubWssService.OnMessageReceived += _pubsubWssService_OnMessageReceived;
		}

        private void _authManager_OnCredentialsUpdated(LoginCredentials credentials)
        {
            if (_isStarted)
            {
                Start(true);
            }
        }

        internal void Start(bool forceReconnect = false)
        {
            if(forceReconnect)
            {
                Stop();
            }
            lock (_initLock)
            {
                if (!_isStarted)
                {
                    _isStarted = true;
                    _websocketService.Connect("wss://irc-ws.chat.twitch.tv:443", forceReconnect);
					Task.Run(ProcessQueuedMessages);
                }
            }
        }

        internal void Stop()
        {
            lock (_initLock)
            {
	            if (!_isStarted)
	            {
		            return;
	            }

	            _isStarted = false;
	            _channels.Clear();

	            LoggedInUser = null;
	            _loggedInUsername = null;

	            _websocketService.Disconnect();
				_pubsubWssService.Disconnect();
			}
        }

        private void _websocketService_OnMessageReceived(Assembly assembly, string rawMessage)
        {
            lock (_messageReceivedLock)
            {
                //_logger.LogInformation("RawMessage: " + rawMessage);
                _rawMessageReceivedCallbacks?.InvokeAll(assembly, this, rawMessage);
                if (_messageParser.ParseRawMessage(rawMessage, _channels, LoggedInUser, out var parsedMessages))
                {
                    foreach (var chatMessage in parsedMessages)
                    {
	                    var twitchMessage = (TwitchMessage)chatMessage;
	                    if(assembly != null)
                        {
                            twitchMessage.Sender = LoggedInUser;
                        }

                        var twitchChannel = (twitchMessage.Channel as TwitchChannel);
                        if (twitchChannel!.Roomstate == null)
                        {
                            twitchChannel.Roomstate = _channels.TryGetValue(twitchMessage.Channel.Id, out var channel) ? (channel as TwitchChannel)?.Roomstate : new TwitchRoomstate();
                        }

                        switch (twitchMessage.Type)
                        {
                            case "PING":
                                SendRawMessage("PONG :tmi.twitch.tv");
                                continue;
                            case "376":  // successful login
                                _dataProvider.TryRequestGlobalResources();
                                _loggedInUsername = twitchMessage.Channel.Id;
                                // This isn't a typo, when you first sign in your username is in the channel id.
                                _logger.LogInformation($"Logged into Twitch as {_loggedInUsername}");
								_pubsubWssService.Connect("wss://pubsub-edge.twitch.tv"); // Here because it needs _loggedInUsername to not be null
								_websocketService.ReconnectDelay = 500;
                                LoginCallbacks?.InvokeAll(assembly!, this, _logger);
                                foreach (var channel in _authManager.Credentials.Twitch_Channels)
                                {
                                    JoinChannel(channel);
                                }
                                continue;
                            case "NOTICE":
                                switch (twitchMessage.Message)
                                {
                                    case "Login authentication failed":
                                    case "Invalid NICK":
                                        _websocketService.Disconnect();
                                        break;
                                }
                                goto case "PRIVMSG";
                            case "USERNOTICE":
                            case "PRIVMSG":
                                TextMessageReceivedCallbacks?.InvokeAll(assembly!, this, twitchMessage, _logger);
                                continue;
							case "REWARD":
								_rewardMessage = twitchMessage;
								continue;
							case "JOIN":
                                //_logger.LogInformation($"{twitchMessage.Sender.Name} JOINED {twitchMessage.Channel.Id}. LoggedInuser: {LoggedInUser.Name}");
                                if (twitchMessage.Sender.UserName == _loggedInUsername)
                                {
                                    if (!_channels.ContainsKey(twitchMessage.Channel.Id))
                                    {
                                        _channels[twitchMessage.Channel.Id] = twitchMessage.Channel.AsTwitchChannel()!;
                                        _logger.LogInformation($"Added channel {twitchMessage.Channel.Id} to the channel list.");
                                        JoinRoomCallbacks?.InvokeAll(assembly!, this, twitchMessage.Channel, _logger);
                                    }
                                }
                                continue;
                            case "PART":
                                //_logger.LogInformation($"{twitchMessage.Sender.Name} PARTED {twitchMessage.Channel.Id}. LoggedInuser: {LoggedInUser.Name}");
                                if (twitchMessage.Sender.UserName == _loggedInUsername)
                                {
                                    if (_channels.TryRemove(twitchMessage.Channel.Id, out var channel))
                                    {
                                        _dataProvider.TryReleaseChannelResources(twitchMessage.Channel);
                                        _logger.LogInformation($"Removed channel {channel.Id} from the channel list.");
                                        LeaveRoomCallbacks?.InvokeAll(assembly!, this, twitchMessage.Channel, _logger);
                                    }
                                }
                                continue;
                            case "ROOMSTATE":
                                _channels[twitchMessage.Channel.Id] = twitchMessage.Channel;
                                _dataProvider.TryRequestChannelResources(twitchMessage.Channel.AsTwitchChannel()!, resources =>
                                {
                                    ChannelResourceDataCached.InvokeAll(assembly!, this, twitchMessage.Channel, resources);
                                });
                                RoomStateUpdatedCallbacks?.InvokeAll(assembly!, this, twitchMessage.Channel, _logger);
                                continue;
                            case "USERSTATE":
                            case "GLOBALUSERSTATE":
                                LoggedInUser = twitchMessage.Sender!.AsTwitchUser()!;
                                if(string.IsNullOrEmpty(LoggedInUser.DisplayName))
                                {
                                    LoggedInUser.DisplayName = _loggedInUsername;
                                }
                                continue;
                            case "CLEARCHAT":
                                twitchMessage.Metadata.TryGetValue("target-user-id", out var targetUser);
                                ChatClearedCallbacks?.InvokeAll(assembly!, this, targetUser, _logger);
                                continue;
                            case "CLEARMSG":
                                if (twitchMessage.Metadata.TryGetValue("target-msg-id", out var targetMessage))
                                {
                                    MessageClearedCallbacks?.InvokeAll(assembly!, this, targetMessage, _logger);
                                }
                                continue;
                            //case "MODE":
                            //case "NAMES":
                            //case "HOSTTARGET":
                            //case "RECONNECT":
                            //    _logger.LogInformation($"No handler exists for type {twitchMessage.Type}. {rawMessage}");
                            //    continue;
                        }
                    }
                }
            }
        }

        private void _websocketService_OnClose()
        {
            _logger.LogInformation("Twitch connection closed");
        }

        private void _websocketService_OnError()
        {
            _logger.LogError("An error occurred in Twitch connection");
        }

        private void _websocketService_OnOpen()
        {
            _logger.LogInformation("Twitch connection opened");
            _websocketService.SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");
            TryLogin();
        }

		private void _pubsubWssService_OnMessageReceived(Assembly assembly, string rawMessage)
		{
			lock (_messageReceivedLock)
			{
				try
				{
					//_logger.LogInformation("RawMessage: " + rawMessage);
					_rawMessageReceivedCallbacks?.InvokeAll(assembly, this, rawMessage);
					var rawJson = JSON.Parse(rawMessage);
					var messageType = rawJson["type"].Value;

					if (messageType == "PONG")
					{
						_logger.LogInformation("PubSub PONG");
						return;
					}
					else if (messageType == "RESPONSE")
					{
						if (rawJson["error"].Value != "")
						{
							_logger.LogError($"{rawJson["error"].Value} on PubSub RESPONSE");
							if (rawJson["error"].Value == "ERR_BADAUTH")
							{
								_logger.LogError("Your OAuth token doesn't have the required topic to access Channel Point Rewards");
							}
							if (rawJson["error"].Value == "ERR_BADTOPIC" || rawJson["error"].Value == "Invalid Topic")
							{
								_logger.LogError("Invalid topic detected on you OAuth token");
							}
							_pubsubPingTimer.Stop();
							_pubsubWssService.Disconnect();
						}
						return;
					}
					else if (messageType == "RECONNECT")
					{
						_pubsubWssService.Disconnect();
						_pubsubWssService.Connect("wss://pubsub-edge.twitch.tv");
						return;
					}
					else if (messageType == "MESSAGE")
					{
						//_logger.LogInformation(rawJson["data"]["message"].Value);
						if (_messageParser.ParsePubSubMessage(rawJson, _rewardMessage, out var parsedMessages))
						{
							_rewardMessage = null;
							foreach (TwitchMessage twitchMessage in parsedMessages)
							{
								TextMessageReceivedCallbacks?.InvokeAll(assembly, this, twitchMessage, _logger);
							}
						}
					}
				}
				catch (Exception e)
				{
					_logger.LogError(e.ToString());
				}
			}
		}

		private void _pubsubWssService_OnClose()
		{
			_logger.LogInformation("PubSub connection closed");
		}

		private void _pubsubWssService_OnError()
		{
			_logger.LogError("An error occurred in PubSub connection");
		}

		private void _pubsubWssService_OnOpen()
		{
			Task.Run(async () => await _dataProvider.GetChannelIdFromUsername(_loggedInUsername)).ContinueWith(x =>
			{
				var oauth = OAuthToken;
				if (OAuthToken.StartsWith("oauth:"))
				{
					oauth = OAuthToken.Replace("oauth:", "");
				}
				_logger.LogInformation("PubSub connection opened");
				_pubsubPingTimer.Interval = 180000;
				_pubsubPingTimer.Elapsed += _pubsubPingTimer_Elapsed;
				_pubsubPingTimer.Start();

				var data = new JSONObject();
				data["type"] = "LISTEN";
				JSONNode topics = new JSONArray();
				topics.AsArray.Add($"channel-points-channel-v1.{x.Result}");
				data["data"].Add("topics", topics);
				data["data"]["auth_token"] = oauth;

				_pubsubWssService.SendMessage(data.ToString());
			});
		}

		private void _pubsubPingTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			_logger.LogInformation("PubSub PING");
			var data = new JSONObject();
			data.Add("type", new JSONString("PING"));
			_pubsubWssService.SendMessage(data.ToString());
		}

		private void TryLogin()
        {
            _logger.LogInformation("Trying to login!");
            if (!string.IsNullOrEmpty(OAuthToken))
            {
                _websocketService.SendMessage($"PASS {OAuthToken}");
            }
            _websocketService.SendMessage($"NICK {UserName}");
        }

        private void SendRawMessage(Assembly assembly, string rawMessage, bool forwardToSharedClients = false)
        {
            if (_websocketService.IsConnected)
            {
                _websocketService.SendMessage(rawMessage);
                if (forwardToSharedClients)
                {
                    _websocketService_OnMessageReceived(assembly, rawMessage);
                }
            }
            else
            {
                _logger.LogWarning("WebSocket service is not connected!");
            }
        }

        private async Task ProcessQueuedMessages()
        {
            while(_isStarted)
            {
                if (_currentMessageCount >= 20)
                {
                    var remainingMilliseconds = (float)(30000 - (DateTime.UtcNow - _lastResetTime).TotalMilliseconds);
                    if (remainingMilliseconds > 0)
                    {
                        await Task.Delay((int)remainingMilliseconds);
                    }
                }
                if((DateTime.UtcNow - _lastResetTime).TotalSeconds >= 30)
                {
                    _currentMessageCount = 0;
                    _lastResetTime = DateTime.UtcNow;
                }

                if(_textMessageQueue.TryDequeue(out var msg))
                {
                    SendRawMessage(msg.Key, msg.Value, true);
                    _currentMessageCount++;
                }
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Sends a raw message to the Twitch server
        /// </summary>
        /// <param name="rawMessage">The raw message to send.</param>
        /// <param name="forwardToSharedClients">
        /// Whether or not the message should also be sent to other clients in the assembly that implement StreamCore, or only to the Twitch server.<br/>
        /// This should only be set to true if the Twitch server would rebroadcast this message to other external clients as a response to the message.
        /// </param>
        public void SendRawMessage(string rawMessage, bool forwardToSharedClients = false)
        {
            // TODO: rate limit sends to Twitch service
            SendRawMessage(Assembly.GetCallingAssembly(), rawMessage, forwardToSharedClients);
        }

        internal void SendTextMessage(Assembly assembly, string message, string channel)
        {
            _textMessageQueue.Enqueue(new KeyValuePair<Assembly, string>(assembly, $"@id={Guid.NewGuid().ToString()} PRIVMSG #{channel} :{message}"));
        }

        public void SendTextMessage(string message, string channel)
        {
           SendTextMessage(Assembly.GetCallingAssembly(), message, channel);
        }

        public void SendTextMessage(string message, IChatChannel channel)
        {
            if (channel is TwitchChannel)
            {
                SendTextMessage(Assembly.GetCallingAssembly(), message, channel.Id);
            }
        }

        public void SendCommand(string command, string channel)
        {
            SendRawMessage(Assembly.GetCallingAssembly(), $"PRIVMSG #{channel} :/{command}");
        }

        public void JoinChannel(string channel)
        {
            _logger.LogInformation($"Trying to join channel #{channel}");
            SendRawMessage(Assembly.GetCallingAssembly(), $"JOIN #{channel.ToLower()}");
        }

        public void PartChannel(string channel)
        {
            SendRawMessage(Assembly.GetCallingAssembly(), $"PART #{channel.ToLower()}");
        }
    }
}
