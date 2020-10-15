using ChatCore.Utilities;

namespace ChatCore.Models
{
    public struct ImageRect
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public ImageRect(string json)
        {
            var obj = JSON.Parse(json);
            if (obj != null)
            {
                X = obj.TryGetKey(nameof(X), out var xJson) ? xJson.AsInt : 0;
                Y = obj.TryGetKey(nameof(Y), out var yJson) ? yJson.AsInt : 0;
                Width = obj.TryGetKey(nameof(Width), out var w) ? w.AsInt : 0;
                Height = obj.TryGetKey(nameof(X), out var h) ? h.AsInt : 0;
            }

            X = 0;
            Y = 0;
            Width = 0;
            Height = 0;
        }
        public JSONObject ToJson()
        {
            var json = new JSONObject();
            json.Add(nameof(X), new JSONNumber(X));
            json.Add(nameof(Y), new JSONNumber(Y));
            json.Add(nameof(Width), new JSONNumber(Width));
            json.Add(nameof(Height), new JSONNumber(Height));
            return json;
        }

        public static ImageRect None = new ImageRect
        {
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0
        };
    }
}
