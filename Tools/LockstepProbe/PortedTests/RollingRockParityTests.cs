using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class RollingRockParityTests {
        private const string Source = "game/domain/object/component/magic/rolling_rock.rs";
        private sealed class Fixture {
            public readonly SimulationContext C = new SimulationContext { DeltaTime = (Fix64)0.5m };
            public readonly SimObject Owner;
            public readonly RollingRock Rock;
            public Fixture() { C.RegisterPrefab("Object", _ => { }); Owner = C.Spawn(Master.LeftPlayer, "Object", new Vector3(5, 3, 5)); Rock = new RollingRock(Owner, 7, 4, (Fix64)0.5m); Owner.AddComponentImmediately(Rock); }
        }
        [PortedTest(Source)] public static void the_direction_is_flattened_onto_the_ground() { var f = new Fixture(); f.Rock.SetTarget(new Vector3(9, 8, 5)); Assert.Equal(new Vector3(1, 0, 0), f.Rock.Direction.Value); }
        [PortedTest(Source)] public static void moves_along_the_direction_on_the_ground() { var f = new Fixture(); f.Rock.SetTarget(new Vector3(9, 3, 5)); f.Rock.Update(); Assert.Equal(new Vector3(7, 0, 5), f.Owner.Position); }
        [PortedTest(Source)] public static void bounces_off_the_border_twice_and_is_destroyed_on_the_third() {
            var f = new Fixture(); f.C.DeltaTime = 1; f.Owner.SetPosition(new Vector3(16, 0, 5)); f.Rock.SetTarget(new Vector3(18, 0, 5)); f.Rock.Update(); Assert.Equal(new Vector3(-1, 0, 0), f.Rock.Direction.Value); Assert.True(!f.Owner.IsDestroyed);
            f.Owner.SetPosition(new Vector3(2, 0, 5)); f.Rock.Update(); Assert.Equal(new Vector3(1, 0, 0), f.Rock.Direction.Value); Assert.True(!f.Owner.IsDestroyed); f.Owner.SetPosition(new Vector3(16, 0, 5)); f.Rock.Update(); Assert.True(f.Owner.IsDestroyed);
        }
        [PortedTest(Source)] public static void reflects_off_an_enemy_and_ignores_it_while_still_touching() {
            var f = new Fixture(); f.Owner.SetPosition(new Vector3(5, 0, 5)); f.Owner.AddCollider(new CircleCollider(f.Owner, (Fix64)0.5m)); f.Rock.SetTarget(new Vector3(9, 0, 5)); var target = f.C.Spawn(Master.RightPlayer, "Object", new Vector3((Fix64)5.5m, 0, 5)); var mob = new DummyMob(target, 100); target.AddComponentImmediately(mob);
            f.Rock.OnCollisionWithEnemy(target); f.Rock.OnCollisionWithEnemy(target); Assert.Equal(93, mob.Hp); Assert.True(f.Rock.Direction.Value.X < 0);
        }
        public static void CheckSpecialPrefabs() {
            var c = new SimulationContext { DeltaTime = (Fix64)0.25m }; c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); SpecialShotPrefabs.Register(c); FieldPrefabs.RegisterElectricField(c);
            foreach (var key in new[] { "wind_blade", "rock_rolling", "boulder_strike", "tide_call" }) { c.GameParameters[key + ".radius"] = (Fix64)0.5m; c.GameParameters[key + ".damage"] = 9; c.GameParameters[key + ".speed"] = 4; }
            c.GameParameters["boulder_strike.sub_damage"] = 3; c.GameParameters["boulder_strike.push_force"] = 4; c.GameParameters["tide_call.duration"] = 1;
            foreach (var key in new[] { "water_field", "electric_field" }) { c.GameParameters[key + ".radius"] = 1; c.GameParameters[key + ".duration"] = 3; }
            var registry = new MagicRegistry(); ShotMagics.Register(registry);
            SimObject Cast(string key) { registry.GetBean(key).Bind(1, key, Element.None).Run(c, Master.LeftPlayer, new Vector3(4, 0, 5), new Vector3(10, 0, 5)); return c.PendingObjects.Last(); }
            DummyMob Target(Fix64 x) { var o = c.Spawn(Master.RightPlayer, "Object", new Vector3(x, 0, 5)); o.AddCollider(new CircleCollider(o, (Fix64)0.5m)); o.AddComponentImmediately(new RigidBody(o, 1)); var mob = new DummyMob(o, 100); o.AddComponentImmediately(mob); return mob; }
            var blade = Cast("wind_blade"); var first = Target(5); var second = Target(6); c.FlushObjects(); var piercing = blade.GetComponent<WindBladeShot>(); piercing.OnCollisionWithEnemy(first.Owner); piercing.OnCollisionWithEnemy(first.Owner); piercing.OnCollisionWithEnemy(second.Owner); Assert.Equal(91, first.Hp); Assert.Equal(94, second.Hp); Assert.True(!blade.IsDestroyed && blade.Elements.NativeHas(Element.Wind));
            var boulder = Cast("boulder_strike"); var strike = boulder.GetComponent<BoulderStrikeShot>(); strike.OnCollisionWithEnemy(first.Owner); Assert.Equal(82, first.Hp); Assert.True(boulder.IsDestroyed); Assert.Equal(1, first.Owner.GetPendingComponents<BoulderStrikeKnockback>().Length); Assert.True(boulder.Elements.NativeHas(Element.Rock) && boulder.Elements.NativeHas(Element.Wind));
            var rock = Cast("rock_rolling"); var rolling = rock.GetComponent<RollingRock>(); c.FlushObjects(); rolling.OnCollisionWithEnemy(first.Owner); Assert.Equal(73, first.Hp); c.DeltaTime = 0; rolling.Update(); rolling.OnCollisionWithEnemy(first.Owner); Assert.Equal(64, first.Hp);
            // 0.05 분리 이동 후에도 겹치는 대상은 update 뒤 재타격하지 않는다.
            first.Owner.SetPosition(rock.Position); rolling.Update(); rolling.OnCollisionWithEnemy(first.Owner); Assert.Equal(64, first.Hp);
            first.Owner.SetPosition(new Vector3(12, 0, 5)); rolling.Update(); rolling.OnCollisionWithEnemy(first.Owner); Assert.Equal(55, first.Hp); Assert.True(rock.IsDestroyed);
            var tide = Cast("tide_call"); c.FlushObjects(); c.DeltaTime = (Fix64)0.25m; tide.Update(); Assert.Equal(new Vector3(5, 0, 5), tide.Position); var field = c.PendingObjects.Single(); Assert.Equal("WaterField", field.Prefab); Assert.Equal(Master.None, field.Master); Assert.Equal(VisualEffect.Wet, field.GetComponent<EffectProvider>().Effect);
            field.GetComponent<WaterFieldEffectReceiver>().Receive(VisualEffect.Burn); Assert.True(!field.IsDestroyed); field.GetComponent<WaterFieldEffectReceiver>().Receive(VisualEffect.Shock); Assert.True(field.IsDestroyed); var electric = c.PendingObjects.Last(); Assert.Equal("ElectricField", electric.Prefab); Assert.Equal(Master.None, electric.Master); Assert.Equal(field.Position, electric.Position);
            var push = tide.GetComponent<PushShot>(); push.OnCollisionWithEnemy(second.Owner); push.OnCollisionWithEnemy(second.Owner); Assert.Equal(85, second.Hp); c.DeltaTime = (Fix64)0.75m; tide.GetComponent<TimedSelfDestroyer>().Update(); Assert.True(tide.IsDestroyed);
        }
    }
}
