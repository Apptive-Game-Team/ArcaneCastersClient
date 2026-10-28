using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class LightningCloudParityTests {
        private const string Source = "game/domain/object/component/magic/lightning_cloud.rs";
        private sealed class Recorder : SimComponent, IDamageable {
            public readonly List<Hit> Hits = new List<Hit>(); public Recorder(SimObject owner) : base(owner) { }
            public void OnDamaged(Hit hit) { Hits.Add(hit); } public void OnDamagedWithDelay(Hit hit, Fix64 delay) { Hits.Add(hit); }
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext(1);
            public readonly SimObject Owner;
            public readonly LightningCloud Cloud;
            public int Attacks;
            public Fixture(Fix64 interval, int count) {
                Context.GameParameters["electric_field.radius"] = 4; Context.GameParameters["electric_field.duration"] = 5;
                FieldPrefabs.RegisterElectricField(Context); Context.RegisterPrefab("Target", _ => { });
                Owner = new SimObject(Context, Master.LeftPlayer, "LightningCloud", new Vector3(4, 2, 6)); Owner.Elements.AddNative(Element.Lightning);
                Owner.SubscribeStatusChange(change => { if (change.Current == ObjectStatus.Attack) Attacks++; }); Cloud = new LightningCloud(Owner, interval, count, 8, (Fix64)0.5m);
            }
            public SimObject Target(Master master) { var result = Context.Spawn(master, "Target", new Vector3(4, 0, 6)); Context.FlushObjects(); return result; }
        }
        [PortedTest(Source)] public static void strikes_immediately_then_twice_at_two_second_intervals() {
            var f = new Fixture(2, 3); f.Cloud.Start(); Assert.Equal(1, f.Attacks);
            f.Context.DeltaTime = (Fix64)1.9m; f.Cloud.Update(); Assert.Equal(1, f.Attacks);
            f.Context.DeltaTime = (Fix64)0.1m; f.Cloud.Update(); Assert.Equal(2, f.Attacks);
            f.Context.DeltaTime = 2; f.Cloud.Update(); Assert.Equal(3, f.Attacks); Assert.True(!f.Owner.IsDestroyed);
            f.Context.DeltaTime = (Fix64)0.3m; f.Cloud.Update(); Assert.True(f.Owner.IsDestroyed);
        }
        [PortedTest(Source)] public static void a_cloud_with_no_strikes_is_destroyed_on_start() { var f = new Fixture(2, 0); f.Cloud.Start(); Assert.True(f.Owner.IsDestroyed); }
        [PortedTest("game/domain/object/prefab/implement/lightning/electric_field_prefab_initializer.rs")] public static void is_not_a_building_so_a_repair_totem_cannot_freeze_it() {
            var f = new Fixture(2, 1); var field = f.Context.Spawn(Master.None, "ElectricField", new Vector3(7, 0, 5));
            Assert.True(field.HasComponent<TimedSelfDestroyer>()); Assert.Equal(TargetCategory.Unknown, PriorityEnemyDetector.CategoryOf(field));
        }
        [PortedTest(Source)] public static void creates_neutral_electric_field_under_the_cloud_on_every_strike() {
            var f = new Fixture(2, 1); f.Cloud.Start(); var created = f.Context.PendingObjects; Assert.Equal(1, created.Length); Assert.Equal("ElectricField", created[0].Prefab); Assert.Equal(Master.None, created[0].Master); Assert.Equal(new Vector3(4, 0, 6), created[0].Position);
        }
        [PortedTest(Source)] public static void damages_enemies_in_the_column_below_the_cloud() {
            var f = new Fixture(2, 1); var target = f.Target(Master.RightPlayer); var receiver = new Recorder(target); target.AddComponentImmediately(receiver); f.Cloud.Start();
            Assert.Equal(ObjectStatus.Damaged, target.Status); Assert.Equal(1, receiver.Hits.Count); Assert.Equal(8, receiver.Hits[0].Damage);
            Assert.Equal(new Vector3(4, 5, 6), f.Cloud.BoxCenter); Assert.Equal(new Vector3(1, 10, 1), f.Cloud.BoxSize);
        }
        [PortedTest(Source)] public static void leaves_the_casters_own_objects_alone() {
            var f = new Fixture(2, 1); var target = f.Target(Master.LeftPlayer); var status = target.Status; var receiver = new Recorder(target); target.AddComponentImmediately(receiver);
            f.Cloud.Start(); Assert.Equal(0, receiver.Hits.Count); Assert.Equal(status, target.Status);
        }
        [PortedTest(Source)] public static void overcharges_the_casters_own_lightning_summons_in_the_column_below() {
            var f = new Fixture(2, 1); var ally = f.Target(Master.LeftPlayer); var enemy = f.Target(Master.RightPlayer);
            foreach (var target in new[] { ally, enemy }) { target.Elements.AddNative(Element.Lightning); target.AddComponentImmediately(new LightningSummonEffectReceiver(target)); }
            f.Cloud.Start(); Assert.Equal(1, ally.GetPendingComponents<OverchargeStatusEffect>().Length); Assert.Equal(0, enemy.GetPendingComponents<OverchargeStatusEffect>().Length);
        }
        public static void CheckPrefabCatchUpAndNeutral() {
            var f = new Fixture(2, 1); LightningCloud.Register(f.Context);
            f.Context.GameParameters["lightning_cloud.attack_interval"] = 1; f.Context.GameParameters["lightning_cloud.quantity"] = 3;
            f.Context.GameParameters["lightning_drop.damage"] = 8; f.Context.GameParameters["lightning_drop.radius"] = (Fix64)0.5m;
            var target = f.Target(Master.None); var receiver = new Recorder(target); target.AddComponentImmediately(receiver);
            var outside = f.Target(Master.RightPlayer); outside.SetPosition(new Vector3(5, 0, 6)); var excluded = new Recorder(outside); outside.AddComponentImmediately(excluded);
            var cloud = f.Context.Spawn(Master.None, "LightningCloud", new Vector3(4, 2, 6)); Assert.True(!cloud.IsDestroyed); Assert.Equal(1, receiver.Hits.Count);
            f.Context.DeltaTime = 3; cloud.GetComponent<LightningCloud>().Update(); Assert.Equal(3, receiver.Hits.Count); Assert.Equal(0, excluded.Hits.Count); Assert.True(!cloud.IsDestroyed);
            f.Context.DeltaTime = (Fix64)0.3m; cloud.GetComponent<LightningCloud>().Update(); Assert.True(cloud.IsDestroyed);
        }
    }
}
