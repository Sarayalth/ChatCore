using System.Collections.Generic;

namespace ChatCore.Interfaces
{
    public interface IEmojiParser
    {
        List<IChatEmote> FindEmojis(string str);
    }
}
