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
                X = obj.TryGetKey("x", out var xJson) ? xJson.AsInt : 0;
                Y = obj.TryGetKey("y", out var yJson) ? yJson.AsInt : 0;
                Width = obj.TryGetKey("width", out var wJson) ? wJson.AsInt : 0;
                Height = obj.TryGetKey("height", out var hJson) ? hJson.AsInt : 0;
            }

            X = 0;
            Y = 0;
            Width = 0;
            Height = 0;
        }
        public JSONObject ToJson()
        {
            var json = new JSONObject();
            json.Add("x", new JSONNumber(X));
            json.Add("y", new JSONNumber(Y));
            json.Add("width", new JSONNumber(Width));
            json.Add("height", new JSONNumber(Height));
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
