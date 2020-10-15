using ChatCore.Interfaces;
using ChatCore.Utilities;

namespace ChatCore.Models.Twitch
{
    public class TwitchChannel : IChatChannel
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public TwitchRoomstate? Roomstate { get; internal set; }

        public TwitchChannel() { }

        public TwitchChannel(string json)
        {
            var obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(Roomstate), out var roomState)) { Roomstate = new TwitchRoomstate(roomState.ToString()); }
        }

        public JSONObject ToJson()
        {
            var obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id));
            obj.Add(nameof(Roomstate), Roomstate.ToJson());
            return obj;
        }
    }
}
