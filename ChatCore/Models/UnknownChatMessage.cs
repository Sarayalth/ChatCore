using ChatCore.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ChatCore.Utilities;

namespace ChatCore.Models
{
    public class UnknownChatMessage : IChatMessage
    {
        public string Id { get; internal set; }
        public bool IsSystemMessage { get; internal set; }
        public bool IsActionMessage { get; internal set; }
        public bool IsHighlighted { get; internal set; }
        public bool IsPing { get; internal set; }
        public string Message { get; internal set; }
        public IChatUser Sender { get; internal set; }
        public IChatChannel Channel { get; internal set; }
        public IChatEmote[] Emotes { get; internal set; }
        public ReadOnlyDictionary<string, string> Metadata { get; internal set; }

        public UnknownChatMessage(string json)
        {
            var obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(IsSystemMessage), out var isSystemMessage)) { IsSystemMessage = isSystemMessage.AsBool; }
            if (obj.TryGetKey(nameof(IsActionMessage), out var isActionMessage)) { IsActionMessage = isActionMessage.AsBool; }
            if (obj.TryGetKey(nameof(IsHighlighted), out var isHighlighted)) { IsHighlighted = isHighlighted.AsBool; }
            if (obj.TryGetKey(nameof(IsPing), out var isPing)) { IsPing = isPing.AsBool; }
            if (obj.TryGetKey(nameof(Message), out var message)) { Message = message.Value; }
            if (obj.TryGetKey(nameof(Sender), out var sender)) { Sender = new UnknownChatUser(sender.ToString()); }
            if (obj.TryGetKey(nameof(Channel), out var channel)) { Channel = new UnknownChatChannel(channel.ToString()); }
            if (obj.TryGetKey(nameof(Emotes), out var emotes))
            {
                var emoteList = new List<IChatEmote>();
                if (emotes.AsArray is not null)
                {
	                foreach (var emote in emotes.AsArray)
	                {
		                if (emote.Value.TryGetKey(nameof(IChatEmote.Id), out var emoteNode))
		                {
			                emoteList.Add(new UnknownChatEmote(emoteNode.Value));
		                }
	                }
                }

                Emotes = emoteList.ToArray();
            }
        }
        public JSONObject ToJson()
        {
            var obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id));
            obj.Add(nameof(IsSystemMessage), new JSONBool(IsSystemMessage));
            obj.Add(nameof(IsActionMessage), new JSONBool(IsActionMessage));
            obj.Add(nameof(IsActionMessage), new JSONBool(IsActionMessage));
            obj.Add(nameof(IsHighlighted), new JSONBool(IsHighlighted));
            obj.Add(nameof(IsPing), new JSONBool(IsPing));
            obj.Add(nameof(Message), new JSONString(Message));
            obj.Add(nameof(Sender), Sender.ToJson());
            obj.Add(nameof(Channel), Channel.ToJson());
            var emotes = new JSONArray();
            foreach (var emote in Emotes)
            {
                emotes.Add(emote.ToJson());
            }
            obj.Add(nameof(Emotes), emotes);
            return obj;
        }
    }
}
