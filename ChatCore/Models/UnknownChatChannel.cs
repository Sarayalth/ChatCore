using ChatCore.Interfaces;
using ChatCore.Utilities;

namespace ChatCore.Models
{
    public class UnknownChatChannel : IChatChannel
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }

        public UnknownChatChannel() { }
        public UnknownChatChannel(string json)
        {
            var obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(Name), out var name)) { Name = name.Value; }
        }
        public JSONObject ToJson()
        {
            var obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id));
            obj.Add(nameof(Name), new JSONString(Name));
            return obj;
        }
    }
}
