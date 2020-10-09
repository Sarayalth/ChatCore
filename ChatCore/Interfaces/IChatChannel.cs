using ChatCore.SimpleJSON;

namespace ChatCore.Interfaces
{
    public interface IChatChannel
    {
        string Name { get; }
        string Id { get; }
        JSONObject ToJson();
    }
}
