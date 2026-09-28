using System;
using System.Collections.Generic;
using Global.Serialization;

namespace GameScene.Dto
{
    public sealed class ServerMessageConverter : JsonSubtypeConverter<ServerMessage>
    {
        private static readonly IReadOnlyDictionary<string, Type> Subtypes = new Dictionary<string, Type>
        {
            { "frame", typeof(FrameInfoDto) },
            { "sync", typeof(SyncFrameInfo) },
            { "magicValid", typeof(MagicValidInfo) },
            { "result", typeof(ResultInfo) },
            { "botThought", typeof(BotThoughtInfo) },
            { "emote", typeof(EmoteInfo) },
            { "pveScript", typeof(PveScriptEventInfo) },
            { "pveScriptEvent", typeof(PveScriptEventInfo) }
        };

        public ServerMessageConverter() : base("type", Subtypes)
        {
        }
    }
}
