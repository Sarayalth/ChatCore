using ChatCore.SimpleJSON;

namespace ChatCore.Interfaces
{
    public interface IChatBadge
    {
        string Id { get; }
        string Name { get; }
        string Uri { get; }
        JSONObject ToJson();
    }
}
