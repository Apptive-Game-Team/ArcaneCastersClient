using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class DropMagicParityTests {
        private sealed class Receiver : CommonEffectReceiver {
            public int Calls; public Fix64 Duration;
            public Receiver(SimObject owner) : base(owner) { }
            public override void Receive(VisualEffect effect, Fix64 duration) { Assert.Equal(VisualEffect.Frenzy, effect); Calls++; Duration = duration; }
        }
        private const string DropSource = "game/domain/magic/implement/drop/abstract_drop_magic.rs";
        private const string LightningSource = "game/domain/magic/implement/drop/lightning_drop_magic.rs";
        private const string RegistrySource = "game/domain/magic/implement/drop/mod.rs";
        private const string FrenzySource = "game/domain/object/component/magic/frenzy_totem.rs";
        [PortedTest(DropSource)] public static void drops_from_the_initial_height() {
            var c = new SimulationContext(); c.RegisterPrefab("MeteorShower", _ => { });
            DropMagics.Create("MeteorShower").Bind(0, "meteor_shower", Element.None).Run(c, Master.RightPlayer, null, new Vector3(2, 0, 3));
            Assert.Equal(new Vector3(2, 10, 3), c.PendingObjects[0].Position);
        }
        [PortedTest(LightningSource)] public static void spawns_storm_cloud_at_ground_height() {
            var c = new SimulationContext(); c.RegisterPrefab("LightningCloud", _ => { }); var registry = new MagicRegistry(); DropMagics.Register(registry);
            registry.GetBean("lightning_drop").Bind(1, "lightning_drop", Element.Lightning).Run(c, Master.LeftPlayer, null, new Vector3(4, 7, 6));
            Assert.Equal(1, c.PendingObjects.Length); Assert.Equal("LightningCloud", c.PendingObjects[0].Prefab); Assert.Equal(new Vector3(4, 0, 6), c.PendingObjects[0].Position);
        }
        [PortedTest(RegistrySource)] public static void registers_every_magic_under_its_java_bean_name() {
            var registry = new MagicRegistry(); DropMagics.Register(registry);
            foreach (var name in new[] { "fire_drop", "frenzy_totem", "leafair", "lightning_drop", "meteor_shower", "nature_drop", "rock_drop", "wind_drop", "chicken_commando", "rallying_totem" }) Assert.True(registry.ContainsBean(name));
        }
        private static SimObject Object(SimulationContext c, Master master, Vector3 position) => c.Spawn(master, "Object", position);
        [PortedTest(FrenzySource)] public static void falls_and_frenzies_every_active_receiver_in_range_when_it_reaches_the_ground() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.5m }; c.RegisterPrefab("Object", _ => { });
            var owner = Object(c, Master.LeftPlayer, new Vector3(5, 1, 5)); var totem = new FrenzyTotem(owner, 4, 3, 5);
            var near = Object(c, Master.RightPlayer, new Vector3(6, 0, 5)); near.AddComponentImmediately(new CommonEffectReceiver(near));
            var far = Object(c, Master.RightPlayer, new Vector3(15, 0, 5)); far.AddComponentImmediately(new CommonEffectReceiver(far)); c.FlushObjects();
            totem.Update(); Assert.Equal(new Vector3(5, -1, 5), owner.Position); Assert.True(owner.IsDestroyed);
        }
        [PortedTest(FrenzySource)] public static void a_collision_triggers_it_once() {
            var c = new SimulationContext(); c.RegisterPrefab("Object", _ => { }); var owner = Object(c, Master.LeftPlayer, new Vector3(5, 1, 5)); var totem = new FrenzyTotem(owner, 4, 3, 5);
            var enemy = Object(c, Master.RightPlayer, new Vector3(5, 0, 5)); totem.OnCollisionWithEnemy(enemy); Assert.True(owner.IsDestroyed); Assert.True(totem.Triggered);
        }
        public static void CheckRegisteredPrefabs() {
            var c = new SimulationContext { DeltaTime = 1 }; c.RegisterPrefab("Object", _ => { });
            c.InitializationFailed += (o, e) => throw e;
            SpecialDropPrefabs.Register(c); DropPrefabs.Register(c);
            c.GameParameters["frenzy_totem.radius"] = 1; c.GameParameters["frenzy_totem.speed"] = 12;
            c.GameParameters["frenzy_totem.attack_range"] = 4; c.GameParameters["frenzy_totem.buff_duration"] = 7;
            var ally = Object(c, Master.LeftPlayer, new Vector3(5, 0, 5)); var a = new Receiver(ally); ally.AddComponentImmediately(a);
            var enemy = Object(c, Master.RightPlayer, new Vector3(6, 0, 5)); var b = new Receiver(enemy); enemy.AddComponentImmediately(b);
            var far = Object(c, Master.RightPlayer, new Vector3(15, 0, 5)); var f = new Receiver(far); far.AddComponentImmediately(f); c.FlushObjects();
            var registry = new MagicRegistry(); DropMagics.Register(registry);
            registry.GetBean("frenzy_totem").Bind(1, "frenzy_totem", Element.None).Run(c, Master.LeftPlayer, null, new Vector3(5, 99, 5));
            var totem = c.PendingObjects[0]; Assert.Equal((Fix64)10, totem.Position.Y); totem.GetComponent<FrenzyTotem>().Update();
            totem.GetComponent<FrenzyTotem>().OnCollisionWithEnemy(enemy);
            Assert.Equal(1, a.Calls); Assert.Equal(1, b.Calls); Assert.Equal(0, f.Calls); Assert.Equal((Fix64)7, b.Duration);
            c.FlushObjects();
            c.GameParameters["meteor_shower.duration"] = (Fix64)2.9m; c.GameParameters["meteor_shower.radius"] = (Fix64)3.9m;
            c.GameParameters["meteor_shower.attack_interval"] = 1; c.GameParameters["meteor_drop.radius"] = 1; c.GameParameters["meteor_drop.damage"] = 4;
            registry.GetBean("meteor_shower").Bind(2, "meteor_shower", Element.Fire).Run(c, Master.RightPlayer, null, new Vector3(8, 0, 5));
            var shower = c.PendingObjects[0]; shower.GetComponent<RandomAreaSpawner>().Update();
            var meteor = c.PendingObjects[1]; Assert.Equal("MeteorDrop", meteor.Prefab); Assert.Equal(Master.RightPlayer, meteor.Master); Assert.Equal((Fix64)10, meteor.Position.Y);
            Assert.True(meteor.GetComponent<Drop>() != null); Assert.True((meteor.Position - shower.Position).Length() <= 3); Assert.True(!shower.IsDestroyed);
            shower.GetComponent<RandomAreaSpawner>().Update(); Assert.True(shower.IsDestroyed);
        }
    }
}
