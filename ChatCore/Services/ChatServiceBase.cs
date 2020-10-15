using ChatCore.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using ChatCore.Utilities;

namespace ChatCore.Services
{
    public class ChatServiceBase
    {
        protected readonly ConcurrentDictionary<Assembly, Action<IChatService, IChatMessage>> TextMessageReceivedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, IChatMessage>>();
        protected readonly ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>> JoinRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>>();
        protected readonly ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>> LeaveRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>>();
        protected readonly ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>> RoomStateUpdatedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel>>();
        protected readonly ConcurrentDictionary<Assembly, Action<IChatService>> LoginCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService>>();
        protected readonly ConcurrentDictionary<Assembly, Action<IChatService, string>> ChatClearedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, string>>();
        protected readonly ConcurrentDictionary<Assembly, Action<IChatService, string>> MessageClearedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatService, string>>();
        protected readonly ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel, Dictionary<string, IChatResourceData>>> ChannelResourceDataCached = new ConcurrentDictionary<Assembly, Action<IChatService, IChatChannel, Dictionary<string, IChatResourceData>>>();

        public event Action<IChatService, IChatMessage> OnTextMessageReceived
        {
            add => TextMessageReceivedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => TextMessageReceivedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        public event Action<IChatService, IChatChannel> OnJoinChannel
        {
            add => JoinRoomCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => JoinRoomCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        public event Action<IChatService, IChatChannel> OnLeaveChannel
        {
            add => LeaveRoomCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => LeaveRoomCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        public event Action<IChatService, IChatChannel> OnRoomStateUpdated
        {
            add => RoomStateUpdatedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => RoomStateUpdatedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        public event Action<IChatService> OnLogin
        {
            add => LoginCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => LoginCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        public event Action<IChatService, string> OnChatCleared
        {
            add => ChatClearedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => ChatClearedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        public event Action<IChatService, string> OnMessageCleared
        {
            add => MessageClearedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => MessageClearedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        public event Action<IChatService, IChatChannel, Dictionary<string, IChatResourceData>> OnChannelResourceDataCached
        {
            add => ChannelResourceDataCached.AddAction(Assembly.GetCallingAssembly(), value);
            remove => ChannelResourceDataCached.RemoveAction(Assembly.GetCallingAssembly(), value);
        }
    }
}
