using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PriorityUnitChecks {
        private sealed class PlayerMarker : SimComponent, IPlayerHealth { public PlayerMarker(SimObject owner) : base(owner) { } }
        public static void Run() {
            var context = new SimulationContext(0); StandardPrefabs.Register(context);
            foreach (var key in new[] { "storm_rider", "zap_mouse", "tree_golem" }) {
                context.GameParameters[key + ".mass"] = 3; context.GameParameters[key + ".radius"] = (Fix64)0.4m;
                context.GameParameters[key + ".hp"] = 100; context.GameParameters[key + ".speed"] = 2;
                context.GameParameters[key + ".damage"] = 7; context.GameParameters[key + ".attack_interval"] = 1;
            }
            context.GameParameters["tree_golem.heal_amount"] = 4; context.GameParameters["tree_golem.heal_interval"] = 1;
            context.GameParameters["leaf_field.radius"] = 1; context.GameParameters["leaf_field.duration"] = 10;
            context.RegisterPrefab("TestTarget", owner => { owner.AddCollider(new CircleCollider(owner, 1)); owner.AddComponentImmediately(new MeleeAttackMob(owner, 100, 1, 1, 1, 1)); });
            var rider = context.Spawn(Master.LeftPlayer, "StormRider", new Vector3(1, 0, 5));
            var mouse = context.Spawn(Master.LeftPlayer, "ZapMouse", new Vector3(1, 0, 5));
            var tree = context.Spawn(Master.LeftPlayer, "TreeGolem", new Vector3(1, 0, 5));
            var unit = context.Spawn(Master.RightPlayer, "TestTarget", new Vector3(2, 0, 5));
            context.RegisterPrefab("TestBuilding", owner => { owner.AddCollider(new CircleCollider(owner, 1)); owner.AddComponentImmediately(new DummyMob(owner, 100)); });
            var building = context.Spawn(Master.RightPlayer, "TestBuilding", new Vector3(5, 0, 5));
            var player = context.Spawn(Master.RightPlayer, "TestTarget", new Vector3(9, 0, 5)); player.AddComponentImmediately(new PlayerMarker(player));
            context.FlushObjects();
            foreach (var owner in new[] { rider, mouse, tree }) { Assert.Equal(false, owner.IsDestroyed); Assert.True(owner.GetComponent<BehaviorMob>().Body != null); }
            Assert.True(rider.Elements.Has(Element.Lightning) && rider.Elements.Has(Element.Water));
            Assert.True(mouse.HasComponent<LightningSummonEffectReceiver>() && mouse.HasComponent<ElectricDeathEnergy>());
            var riderMob = rider.GetComponent<PlayerPrioMob>(); var mouseMob = mouse.GetComponent<BuildingPrioMob>();
            Assert.True(ReferenceEquals(player, riderMob.Detector.Detect(rider)));
            Assert.True(ReferenceEquals(unit, riderMob.Detector.Detect(rider, target => target != player)));
            Assert.True(ReferenceEquals(building, mouseMob.Detector.Detect(mouse)));
            Assert.True(ReferenceEquals(player, mouseMob.Detector.Detect(mouse, target => target != building)));
            Assert.True(mouseMob.Detector.Detect(mouse, target => target == unit) == null);
            Assert.True(riderMob.Behavior(player)); Assert.Equal(93, player.GetComponent<MobComponent>().Hp);
            var treeMob = tree.GetComponent<MeleeAttackMob>(); treeMob.OnDamaged(new Hit(10, 0));
            context.DeltaTime = (Fix64)1.1m; tree.GetComponent<SelfHealer>().Update(); Assert.Equal(94, treeMob.Hp);
            tree.SetPosition(new Vector3(3, 0, 5)); tree.GetComponent<PathSpawner>().Update();
            var field = context.PendingObjects.Single(owner => owner.Prefab == "LeafField"); Assert.Equal(Master.None, field.Master); Assert.Equal(tree.Position, field.Position); Assert.Equal(false, field.IsDestroyed);
        }
    }
}
