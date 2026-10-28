using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IBossMagic {
        long Id { get; }
        bool IsSpawnMagic { get; }
        bool Cast(SimulationContext context, Master master, Vector3 target, Vector3 origin);
    }
    public class PveBossMob : BehaviorMob, IPveObjectiveTarget, IFrenzyImmune, IGaugeComponent {
        private readonly Dictionary<long, Fix64> cooldowns = new Dictionary<long, Fix64>();
        protected readonly IBossMagic[] magics;
        public IReadOnlyList<IBossMagic> Magics => magics;
        public bool IsTerminal => Owner.IsDestroyed || Hp <= 0;
        public override bool IsObjectiveTarget => true;
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
        public PveBossMob(SimObject owner, int hp, Fix64 speed, int mask, Fix64 interval, Fix64 range, IEnumerable<IBossMagic> magics) : base(owner, hp, speed, mask, interval, range, null) {
            this.magics = magics.ToArray(); Behavior = target => CastMagic(target);
        }
        public override void Update() {
            foreach (var id in cooldowns.Keys.ToArray()) cooldowns[id] = MathHelper.Max(0, cooldowns[id] - Context.DeltaTime);
            base.Update();
        }
        public virtual bool CastMagic(SimObject target) => CastMagicsInOrder(target, magics);
        public bool CastMagicsInOrder(SimObject target, IEnumerable<IBossMagic> order) {
            foreach (var magic in order) if (TryCastMagic(magic, target)) return true;
            return false;
        }
        public bool TryCastMagic(IBossMagic magic, SimObject target) {
            if (magic == null || cooldowns.TryGetValue(magic.Id, out var remaining) && remaining > (Fix64)0.0001m) return false;
            var castTarget = magic.IsSpawnMagic ? Owner : target; if (castTarget == null) return false;
            if (!magic.Cast(Context, Owner.Master, castTarget.Position, Owner.Position)) return false;
            cooldowns[magic.Id] = 2; return true;
        }
    }
    public static class StandardMagics {
        public static void RegisterAll(MagicRegistry registry) {
            BuildMagics.Register(registry); DropMagics.Register(registry); AreaMagics.Register(registry); RegisterShots(registry); SpawnMagics.Register(registry);
        }
        public static void RegisterShots(MagicRegistry registry) {
            ShotMagics.Register(registry); SpiritBombMagic.Register(registry); TidalWarheadMagic.Register(registry); VineMagics.Register(registry);
        }
    }
}
