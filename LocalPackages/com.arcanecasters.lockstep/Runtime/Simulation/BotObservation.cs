using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public readonly struct BotVisibleObject {
        public readonly int Id, Hp;
        public readonly Master Master;
        public readonly string Prefab;
        public readonly Vector3 Position;
        public readonly bool Mob, Targetable;
        public BotVisibleObject(int id, Master master, string prefab, Vector3 position, int hp, bool mob, bool targetable) {
            Id = id; Master = master; Prefab = prefab; Position = position; Hp = hp; Mob = mob; Targetable = targetable;
        }
        public static BotVisibleObject Of(SimObject owner, Func<SimObject, int?> mobHp) {
            bool mob = owner.HasComponent<MobComponent>();
            int? hp = mob ? mobHp(owner) : null;
            return new BotVisibleObject(owner.Id, owner.Master, owner.Prefab, owner.Position, hp.HasValue ? Math.Max(1, hp.Value) : 10, mob, owner.IsActive && !owner.IsDying);
        }
    }
    public sealed class BotEye {
        public IReadOnlyList<BotVisibleObject> Objects { get; }
        public IReadOnlyList<long> Cards { get; }
        public int Mana { get; }
        public int EnemyPlayerHp { get; }
        // 원본의 NaN(관측 시각 없음)을 nullable로 나타내고 모든 유효 시각은 고정 소수점으로 저장한다.
        public Fix64? ObservedAtSeconds { get; }
        public BotEye(IEnumerable<BotVisibleObject> objects, IEnumerable<long> cards, int mana, int enemyPlayerHp, Fix64? observedAtSeconds = null) {
            Objects = Array.AsReadOnly(objects.ToArray()); Cards = Array.AsReadOnly(cards.ToArray());
            Mana = mana; EnemyPlayerHp = enemyPlayerHp; ObservedAtSeconds = observedAtSeconds;
        }
        public static BotEye Observe(SimulationContext context, Master side, Func<SimObject, int?> mobHp, Fix64? time = null) {
            var player = context.Players[side == Master.LeftPlayer ? Master.LeftPlayer : Master.RightPlayer];
            var enemy = context.Players[side == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer];
            return new BotEye(context.Objects.Select(o => BotVisibleObject.Of(o, mobHp)), player.Cards, player.Mana, enemy.Hp, time);
        }
    }
    public sealed class MotionTracker {
        public static readonly Fix64 MaxSampleGapSeconds = 2;
        public static readonly Fix64 MaxTrackedSpeed = 8;
        private Dictionary<int, Vector3> lastPositions = new Dictionary<int, Vector3>();
        private Fix64? lastSeenAtSeconds;
        public Dictionary<int, Vector3> Observe(BotEye eye) {
            var positions = new Dictionary<int, Vector3>();
            foreach (var item in eye.Objects) positions[item.Id] = item.Position;
            var velocities = new Dictionary<int, Vector3>();
            if (eye.ObservedAtSeconds.HasValue && lastSeenAtSeconds.HasValue) {
                var gap = eye.ObservedAtSeconds.Value - lastSeenAtSeconds.Value;
                if (gap > Fix64.Zero && gap <= MaxSampleGapSeconds) {
                    foreach (var pair in positions) {
                        if (!lastPositions.TryGetValue(pair.Key, out var previous)) continue;
                        var velocity = (pair.Value - previous) * (Fix64.One / gap);
                        if (velocity.Length() <= MaxTrackedSpeed) velocities[pair.Key] = velocity;
                    }
                }
            }
            if (eye.ObservedAtSeconds.HasValue) { lastPositions = positions; lastSeenAtSeconds = eye.ObservedAtSeconds; }
            return velocities;
        }
    }
}
