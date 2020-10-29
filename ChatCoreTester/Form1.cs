using System;
using System.Diagnostics;
using System.Windows.Forms;
using ChatCore;
using ChatCore.Interfaces;
using ChatCore.Models.Twitch;
using ChatCore.Services;
using ChatCore.Services.Twitch;

namespace ChatCoreTester
{
    public partial class Form1 : Form
    {
        private readonly ChatServiceMultiplexer _streamingService;
        private TwitchService _twitchService;
        public Form1()
        {
            InitializeComponent();

            var chatCore = ChatCoreInstance.Create();
            chatCore.OnLogReceived += (level, category, message) => Debug.WriteLine($"{level} | {category} | {message}");
            _streamingService = chatCore.RunAllServices();
            _twitchService = _streamingService.GetTwitchService();
            _streamingService.OnLogin += StreamingService_OnLogin;
            _streamingService.OnTextMessageReceived += StreamServiceProvider_OnMessageReceived;
            _streamingService.OnJoinChannel += StreamServiceProvider_OnChannelJoined;
            _streamingService.OnLeaveChannel += StreamServiceProvider_OnLeaveChannel;
            _streamingService.OnRoomStateUpdated += StreamServiceProvider_OnChannelStateUpdated;
            //Console.WriteLine($"StreamService is of type {streamServiceProvider.ServiceType.Name}");
        }

        private void StreamingService_OnLogin(IChatService svc)
        {
            if(svc is TwitchService twitchService)
            {
                twitchService.JoinChannel("realeris");
            }
        }

        private void StreamServiceProvider_OnChannelStateUpdated(IChatService svc, IChatChannel channel)
        {
            Console.WriteLine($"Channel state updated for {channel.GetType().Name} {channel.Id}");
            if (channel is TwitchChannel twitchChannel)
            {
                Console.WriteLine($"RoomId: {twitchChannel.Roomstate.RoomId}");
            }
        }

        private void StreamServiceProvider_OnLeaveChannel(IChatService svc, IChatChannel channel)
        {
            Console.WriteLine($"Left channel {channel.Id}");
        }

        private void StreamServiceProvider_OnChannelJoined(IChatService svc, IChatChannel channel)
        {
            Console.WriteLine($"Joined channel {channel.Id}");
        }

        private void StreamServiceProvider_OnMessageReceived(IChatService svc, IChatMessage msg)
        {
            Console.WriteLine($"{msg.Sender.DisplayName}: {msg.Message}");
            //Console.WriteLine(msg.ToJson().ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _streamingService.GetTwitchService().PartChannel("xqcow");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _streamingService.GetTwitchService().JoinChannel("xqcow");
        }

		private void button3_Click(object sender, EventArgs e)
		{
			_twitchService.SendTextMessage("Heya", "realeris");
		}
	}
}
