using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class SummonerParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/summoner_mob.rs";
        private static SimulationContext Context() { var c = new SimulationContext(); foreach (var p in new[] { "MagmaSpirit", "MagmaFist", "Target" }) c.RegisterPrefab(p, _ => { }); return c; }
        private static SimObject Summon(SimulationContext c, SimObject owner, SimObject target, Fix64 radius, Fix64 interval, Fix64 range, Fix64 delta) {
            c.FlushObjects(); c.DeltaTime = delta; var mob = new SummonerMob(owner, 10, 1, 1, interval, range, "MagmaFist"); mob.Target = target; mob.TargetRadius = radius; mob.SetAttack(); mob.Update();
            var summoned = c.PendingObjects.Single(); Assert.Equal("MagmaFist", summoned.Prefab); Assert.Equal(Master.LeftPlayer, summoned.Master); return summoned;
        }
        [PortedTest(Source)] public static void summons_owned_prefab_at_target_position_when_target_is_within_range() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "MagmaSpirit", new Vector3(2, 0, 3)); var target = c.Spawn(Master.RightPlayer, "Target", new Vector3((Fix64)3.2m, 0, (Fix64)4.6m));
            Assert.Equal(target.Position, Summon(c, owner, target, 0, 1, 5, (Fix64)1.1m).Position);
        }
        [PortedTest(Source)] public static void summons_owned_prefab_at_maximum_range_when_target_center_is_outside_range() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "MagmaSpirit", new Vector3(2, 0, 3)); var target = c.Spawn(Master.RightPlayer, "Target", new Vector3(8, 0, 11)); target.AddCollider(new CircleCollider(target, 6));
            var fist = Summon(c, owner, target, 6, 1, 5, (Fix64)1.1m); Assert.Near((Fix64)5, fist.Position.X, (Fix64)0.0001m); Assert.Equal(Fix64.Zero, fist.Position.Y); Assert.Near((Fix64)7, fist.Position.Z, (Fix64)0.0001m); Assert.Near((Fix64)5, (fist.Position - owner.Position).Length(), (Fix64)0.0001m);
        }
        private static void Reaches(Fix64 radius) {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "MagmaSpirit", Vector3.Zero); owner.AddCollider(new CircleCollider(owner, (Fix64)1.5m)); var target = c.Spawn(Master.RightPlayer, "Target", new Vector3((Fix64)2.9m + radius, 0, 0)); target.AddCollider(new CircleCollider(target, radius));
            Assert.True(CombatRange.Contains(owner.Position, (Fix64)1.5m, target.Position, radius, (Fix64)1.5m)); var fist = Summon(c, owner, target, radius, 3, (Fix64)1.5m, (Fix64)3.5m); Assert.True((fist.Position - target.Position).Length() - radius <= 1);
        }
        [PortedTest(Source)] public static void summoned_fist_reaches_a_small_target_the_summoner_engaged() => Reaches((Fix64)0.5m);
        [PortedTest(Source)] public static void summoned_fist_reaches_a_large_target_the_summoner_engaged() => Reaches((Fix64)1.5m);
        [PortedTest(Source)] public static void summon_point_is_clamped_to_the_edge_of_the_summoners_reach() {
            var c = Context(); var owner = c.Spawn(Master.LeftPlayer, "MagmaSpirit", new Vector3(2, 0, 3)); owner.AddCollider(new CircleCollider(owner, (Fix64)1.5m)); var target = c.Spawn(Master.RightPlayer, "Target", new Vector3((Fix64)6.5m, 0, 3)); target.AddCollider(new CircleCollider(target, (Fix64)1.5m));
            var fist = Summon(c, owner, target, (Fix64)1.5m, 3, (Fix64)1.5m, (Fix64)3.5m); Assert.Near((Fix64)3, (fist.Position - owner.Position).Length(), (Fix64)0.0001m); Assert.True((fist.Position - target.Position).Length() - (Fix64)1.5m <= (Fix64)0.0001m);
        }
        public static void CheckActualMagmaSpirit() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; SummonerMob.RegisterPrefabs(c); c.RegisterPrefab("Target", _ => { });
            string[] keys = { "mass", "radius", "hp", "speed", "attack_interval", "attack_range", "sub_attack_range" }; decimal[] values = { 10, 1.5m, 100, 0.5m, 3, 1.5m, 2 };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["magma_spirit." + keys[i]] = (Fix64)values[i];
            c.GameParameters["magma_fist.radius"] = 1; c.GameParameters["magma_fist.damage"] = 20; c.GameParameters["magma_fist.duration"] = 1;
            var owner = c.Spawn(Master.LeftPlayer, "MagmaSpirit", new Vector3(2, 0, 5)); var target = c.Spawn(Master.RightPlayer, "Target", new Vector3((Fix64)6.4m, 0, 5)); target.AddCollider(new CircleCollider(target, (Fix64)1.5m)); var victim = new DummyMob(target, 100); target.AddComponentImmediately(victim); c.FlushObjects();
            var mob = owner.GetComponent<SummonerMob>(); Assert.Equal(1, mob.TargetMask); Assert.True(owner.HasComponent<AreaEffectProvider>() && owner.HasComponent<RockDeathRemnant>() && owner.HasComponent<CommonEffectReceiver>());
            Assert.True(mob.Behavior(target)); var fist = c.PendingObjects.Single(); Assert.Equal(new Vector3(5, 0, 5), fist.Position); Assert.Equal(80, victim.Hp); Assert.True(fist.Colliders.Single().IsTrigger); Assert.Equal(VisualEffect.Burn, fist.GetComponent<EffectProvider>().Effect); Assert.True(fist.Elements.NativeHas(Element.Fire) && fist.Elements.NativeHas(Element.Rock));
            c.DeltaTime = 1; fist.GetComponent<TimedSelfDestroyer>().Update(); Assert.True(fist.IsDestroyed);
        }
    }
}
