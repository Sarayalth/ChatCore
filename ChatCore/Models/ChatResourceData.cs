using ChatCore.Interfaces;

namespace ChatCore.Models
{
    public class ChatResourceData : IChatResourceData
    {
        public string Uri { get; internal set; } = null!;
        public bool IsAnimated { get; internal set; }
        public string Type { get; internal set; } = null!;
    }
}
