using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ChatCore.Utilities;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace ChatCore.Services
{
    public class WebSocket4NetServiceProvider : IWebSocketService
    {
	    private readonly ILogger _logger;
	    private readonly object _lock;
	    private readonly SemaphoreSlim _reconnectLock;

	    private WebSocket? _client;
	    private string _uri = string.Empty;
	    private CancellationTokenSource? _cancellationToken;
	    private DateTime _startTime;

        public bool IsConnected => !(_client is null) && (_client.State == WebSocketState.Open || _client.State == WebSocketState.Connecting);
        public bool AutoReconnect { get; set; } = true;
        public int ReconnectDelay { get; set; } = 500;

        public event Action? OnOpen;
        public event Action? OnClose;
        public event Action? OnError;
        public event Action<Assembly, string>? OnMessageReceived;

        public WebSocket4NetServiceProvider(ILogger<WebSocket4NetServiceProvider> logger)
        {
	        _logger = logger;

	        _lock = new object();
	        _reconnectLock = new SemaphoreSlim(1, 1);
        }

        public void Connect(string uri, bool forceReconnect = false)
        {
            lock (_lock)
            {
                if (forceReconnect)
                {
                    Dispose();
                }

                if (_client is not null)
                {
	                return;
                }

                _logger.LogDebug($"Connecting to {uri}");
                _uri = uri;
                _cancellationToken = new CancellationTokenSource();
                Task.Run(async () =>
                {
	                try
	                {
		                _client = new WebSocket(uri);
		                _client.Opened += _client_Opened;
		                _client.Closed += _client_Closed;
		                _client.Error += _client_Error;
		                _client.MessageReceived += _client_MessageReceived;
		                _startTime = DateTime.UtcNow;

		                await _client.OpenAsync();
	                }
	                catch (TaskCanceledException)
	                {
		                _logger.LogInformation("WebSocket client task was cancelled");
	                }
	                catch (Exception ex)
	                {
		                _logger.LogError(ex, $"An exception occurred in WebSocket while connecting to {_uri}");
		                OnError?.Invoke();
		                TryHandleReconnect();
	                }
                }, _cancellationToken.Token);
            }
        }

        private void _client_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _logger.LogDebug($"Message received from {_uri}: {e.Message}");
            OnMessageReceived?.Invoke(null!, e.Message);
        }

        private void _client_Opened(object sender, EventArgs e)
        {
            _logger.LogDebug($"Connection to {_uri} opened successfully!");
            OnOpen?.Invoke();
        }

        private void _client_Error(object sender, ErrorEventArgs e)
        {
            _logger.LogError(e.Exception, $"An error occurred in WebSocket while connected to {_uri}");
            OnError?.Invoke();
            TryHandleReconnect();
        }

        private void _client_Closed(object sender, EventArgs e)
        {
            _logger.LogDebug($"WebSocket connection to {_uri} was closed");
            OnClose?.Invoke();
            TryHandleReconnect();
        }

        private async void TryHandleReconnect()
        {
            _logger.LogInformation($"Connection was closed after {(DateTime.UtcNow - _startTime).ToShortString()}.");
            if (!await _reconnectLock.WaitAsync(0))
            {
                //_logger.LogInformation("Not trying to reconnect, connectLock already locked.");
                return;
            }

            if (_client!= null)
            {
	            _client.Opened -= _client_Opened;
	            _client.Closed -= _client_Closed;
	            _client.Error -= _client_Error;
	            _client.MessageReceived -= _client_MessageReceived;
	            _client.Dispose();
	            _client = null;
            }

            if (AutoReconnect && (!_cancellationToken!.IsCancellationRequested))
            {
                _logger.LogInformation($"Trying to reconnect to {_uri} in {(int)TimeSpan.FromMilliseconds(ReconnectDelay).TotalSeconds} sec");
                try
                {
                    await Task.Delay(ReconnectDelay, _cancellationToken.Token);
                    Connect(_uri);
                    ReconnectDelay *= 2;
                    if (ReconnectDelay > 60000)
                    {
                        ReconnectDelay = 60000;
                    }
                }
                catch (TaskCanceledException) { }
            }
            _reconnectLock.Release();
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                _logger.LogInformation("Disconnecting");
                Dispose();
            }
        }

        public void SendMessage(string message)
        {
            try
            {
                if (IsConnected)
                {
#if DEBUG
					_logger.LogDebug($"Sending {message}"); // Only log this in debug builds, since it can potentially contain sensitive auth data
#endif
                    _client!.Send(message);
                }
                else
                {
                    _logger.LogDebug("WebSocket not connected, can't send message!");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred while trying to send message to {_uri}");
            }
        }

        public void Dispose()
        {
	        if (_client == null)
	        {
		        return;
	        }

	        if(IsConnected)
	        {
		        _cancellationToken?.Cancel();
		        _client.Close();
	        }

	        _client.Dispose();
	        _client = null;
        }
    }
}
