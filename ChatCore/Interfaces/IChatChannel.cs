using ChatCore.Utilities;

namespace ChatCore.Interfaces
{
    public interface IChatChannel
    {
        string Name { get; }
        string Id { get; }
        JSONObject ToJson();
    }
}
