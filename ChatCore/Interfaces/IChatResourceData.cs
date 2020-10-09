namespace ChatCore.Interfaces
{
    public interface IChatResourceData
    {
        public string Uri { get; }
        public bool IsAnimated { get; }
        public string Type { get; }
    }
}
