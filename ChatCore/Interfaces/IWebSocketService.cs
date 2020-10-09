using System;
using System.Reflection;

namespace ChatCore.Interfaces
{
    public interface IWebSocketService : IDisposable
    {
        bool IsConnected { get; }
        bool AutoReconnect { get; set; }
        int ReconnectDelay { get; set; }

        event Action OnOpen;
        event Action OnClose;
        event Action OnError;
        event Action<Assembly, string> OnMessageReceived;

        void Connect(string uri, bool forceReconnect = false);
        void Disconnect();
        void SendMessage(string message);
    }
}
