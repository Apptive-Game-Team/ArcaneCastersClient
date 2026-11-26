using System;
using System.Collections.Generic;
using Global.Serialization;

namespace GameScene.Dto.Event
{
    public sealed class GameEventConverter : JsonSubtypeConverter<GameEvent>
    {
        private static readonly IReadOnlyDictionary<string, Type> Subtypes = new Dictionary<string, Type>
        {
            { "hit", typeof(HitEvent) },
            { "shock", typeof(ShockEvent) },
            { "summonAttack", typeof(SummonAttackEvent) }
        };

        public GameEventConverter() : base("type", Subtypes)
        {
        }
    }
}
