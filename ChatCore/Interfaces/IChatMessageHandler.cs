namespace ChatCore.Interfaces
{
    public interface IChatMessageHandler
    {
        void OnMessageReceived(IChatMessage messasge);
    }
}
