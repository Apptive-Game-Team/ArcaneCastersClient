using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public readonly struct EnemyThreat {
        public readonly int ObjectId, Hp;
        public readonly Vector3 Position;
        public readonly Fix64 DistanceToDefendedPosition, Strength;
        public readonly bool PlayerCore;
        public EnemyThreat(int objectId, Vector3 position, int hp, Fix64 distance, bool playerCore) : this(objectId, position, hp, distance, playerCore, Fix64.One) { }
        public EnemyThreat(int objectId, Vector3 position, int hp, Fix64 distance, bool playerCore, Fix64 strength) {
            ObjectId = objectId; Position = position; Hp = hp; DistanceToDefendedPosition = distance; PlayerCore = playerCore; Strength = strength;
        }
        public EnemyThreat MovedTo(Vector3 position, Vector3 defended) => new EnemyThreat(ObjectId, position, Hp, (position - defended).Length(), PlayerCore, Strength);
    }
    public sealed class ThreatAssessment {
        public static readonly Fix64 PressureHorizon = 9, PressureSaturation = 3, LaneGroupingGap = (Fix64)2.5m, AllyCoverWeight = (Fix64)0.8m;
        private readonly EnemyThreat[] threats;
        private readonly EnemyThreat[] defenders;
        private readonly Vector3 defended;
        public IReadOnlyList<EnemyThreat> Threats => Array.AsReadOnly(threats);
        public bool HasThreats => threats.Length > 0;
        private ThreatAssessment(IEnumerable<EnemyThreat> threats, EnemyThreat[] defenders, Vector3 defended) {
            this.threats = threats.OrderBy(t => t.DistanceToDefendedPosition).ToArray(); this.defenders = defenders; this.defended = defended;
        }
        public static ThreatAssessment Observe(IEnumerable<BotVisibleObject> objects, Master enemySide, Vector3 defended, int enemyPlayerHp, Func<BotVisibleObject, Fix64> strength = null, bool allyCover = false) {
            strength = strength ?? (_ => Fix64.One);
            var allySide = enemySide == Master.LeftPlayer ? Master.RightPlayer : Master.LeftPlayer;
            var threats = new List<EnemyThreat>(); var defenders = new List<EnemyThreat>();
            foreach (var item in objects) {
                if (!item.Targetable) continue;
                bool core = item.Prefab == "Player"; var distance = (item.Position - defended).Length();
                if (item.Master == enemySide) threats.Add(new EnemyThreat(item.Id, item.Position, core ? enemyPlayerHp : item.Hp, distance, core, core ? Fix64.Zero : strength(item)));
                else if (allyCover && item.Master == allySide && item.Mob && !core) defenders.Add(new EnemyThreat(item.Id, item.Position, item.Hp, distance, false, strength(item)));
            }
            return new ThreatAssessment(threats, defenders.ToArray(), defended);
        }
        private static Fix64 ProximityAt(Fix64 distance) => MathHelper.Clamp(Fix64.One - distance / PressureHorizon, Fix64.Zero, Fix64.One);
        private static Fix64 Proximity(EnemyThreat threat) => threat.PlayerCore ? Fix64.Zero : ProximityAt(threat.DistanceToDefendedPosition);
        private static Fix64 LaneWeight(EnemyThreat threat) => Proximity(threat) * threat.Strength;
        private static Fix64 TotalWeight(IEnumerable<EnemyThreat> lane) { Fix64 sum = 0; foreach (var threat in lane) sum += LaneWeight(threat); return sum; }
        public Fix64 Pressure {
            get {
                Fix64 weighted = TotalWeight(threats), cover = 0;
                foreach (var defender in defenders)
                    if (threats.Any(t => Proximity(t) > Fix64.Zero && Fix64.Abs(t.Position.Z - defender.Position.Z) <= LaneGroupingGap))
                        cover += ProximityAt(defender.DistanceToDefendedPosition) * defender.Strength;
                return MathHelper.Clamp((weighted - AllyCoverWeight * cover) / PressureSaturation, Fix64.Zero, Fix64.One);
            }
        }
        private Fix64 WeightedLane(IEnumerable<EnemyThreat> lane) {
            Fix64 sum = 0, weightedOffset = 0; Fix64? origin = null;
            // 절대 좌표 대신 기준점에서의 차이를 누적해 같은 Z의 적은 그 Z를 정확히 유지한다.
            foreach (var threat in lane) {
                var weight = LaneWeight(threat); if (weight == Fix64.Zero) continue;
                if (!origin.HasValue) origin = threat.Position.Z;
                sum += weight; weightedOffset += weight * (threat.Position.Z - origin.Value);
            }
            return sum == Fix64.Zero ? defended.Z : origin.Value + weightedOffset / sum;
        }
        public Fix64 ContestedLaneZ => WeightedLane(threats);
        public Fix64 StrongestLaneZ {
            get {
                var pressing = threats.Where(t => LaneWeight(t) > Fix64.Zero).OrderBy(t => t.Position.Z).ToArray();
                if (pressing.Length == 0) return defended.Z;
                var heaviest = new List<EnemyThreat>(); var lane = new List<EnemyThreat>(); Fix64 heaviestWeight = -1;
                foreach (var threat in pressing) {
                    if (lane.Count > 0 && threat.Position.Z - lane[lane.Count - 1].Position.Z > LaneGroupingGap) {
                        var weight = TotalWeight(lane); if (weight > heaviestWeight) { heaviest = lane; heaviestWeight = weight; }
                        lane = new List<EnemyThreat>();
                    }
                    lane.Add(threat);
                }
                if (TotalWeight(lane) > heaviestWeight) heaviest = lane;
                return WeightedLane(heaviest);
            }
        }
        public ThreatAssessment LedBy(IReadOnlyDictionary<int, Vector3> velocities, Fix64 seconds) {
            if (velocities.Count == 0 || seconds <= Fix64.Zero) return this;
            return new ThreatAssessment(threats.Select(t => velocities.TryGetValue(t.ObjectId, out var velocity) ? t.MovedTo(t.Position + velocity * seconds, defended) : t), defenders, defended);
        }
    }
}
