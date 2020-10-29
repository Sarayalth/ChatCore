using System.Collections.Concurrent;

namespace ChatCore.Interfaces
{
    public interface IChatMessageParser
    {
        bool ParseRawMessage(string rawMessage, ConcurrentDictionary<string, IChatChannel> channelInfo, IChatUser loggedInUser, out IChatMessage[] parsedMessage);
    }
}
