using System.Collections.Generic;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class VineWitchParityTests {
        private sealed class Fan : IVineFan {
            public readonly List<(Vector3 origin, Vector3 target)> Casts = new List<(Vector3, Vector3)>();
            public void Run(SimulationContext c, Master m, Vector3 origin, Vector3 target) { Casts.Add((origin, target)); }
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext { DeltaTime = (Fix64)0.05m };
            public readonly Fan Fan = new Fan(); public readonly VineWitchMob Witch; public readonly SimObject Target;
            public Fixture(IVineFan fan = null, bool noFan = false, int hp = 100) {
                Context.RegisterPrefab("Object", _ => { }); var owner = Context.Spawn(Master.RightPlayer, "Object", Vector3.Zero);
                Witch = new VineWitchMob(owner, hp, 1, 3, 1, 5, new IBossMagic[0], noFan ? null : fan ?? Fan); owner.AddComponentImmediately(Witch);
                Target = Context.Spawn(Master.LeftPlayer, "Object", new Vector3(2, 0, 0)); Target.AddComponentImmediately(new DummyMob(Target, 50)); Context.FlushObjects();
            }
        }
        [PortedTest("tests/mob_simple.rs")] public static void a_vine_witch_starts_its_rage_fans_when_it_drops_to_half_hp() {
            var f = new Fixture(); f.Witch.Update(); Assert.Equal(0, f.Fan.Casts.Count); f.Witch.OnDamaged(new Hit(40, 0, Element.None)); Assert.Equal(60, f.Witch.Hp); f.Witch.Update(); Assert.Equal(0, f.Fan.Casts.Count);
            f.Witch.OnDamaged(new Hit(10, 0, Element.None)); Assert.Equal(50, f.Witch.Hp); f.Witch.Update(); Assert.Equal(1, f.Fan.Casts.Count); Assert.Equal((Vector3.Zero, new Vector3(2, 0, 0)), f.Fan.Casts[0]); Assert.True(f.Witch.CastMagic(f.Target));
        }
        public static void CheckRageEdges() {
            var f = new Fixture(); f.Witch.OnDamaged(new Hit(50, 0, Element.None)); f.Target.SetPosition(new Vector3(10, 0, 0)); f.Context.DeltaTime = 10; f.Witch.Update(); Assert.Equal(0, f.Fan.Casts.Count);
            f.Target.SetPosition(new Vector3(2, 0, 0)); f.Context.DeltaTime = (Fix64)0.05m; f.Witch.Update(); Assert.Equal(1, f.Fan.Casts.Count);
            f.Context.DeltaTime = (Fix64)0.4m; f.Witch.Update(); Assert.Equal(2, f.Fan.Casts.Count); f.Context.DeltaTime = (Fix64)0.9m; f.Witch.Update(); Assert.Equal(4, f.Fan.Casts.Count); Assert.True(!f.Witch.CastMagic(f.Target));
            f.Witch.OnDamaged(new Hit(-50, 0, Element.None)); f.Witch.OnDamaged(new Hit(50, 0, Element.None)); f.Witch.Update(); Assert.Equal(4, f.Fan.Casts.Count);
            var dead = new Fixture(); dead.Witch.OnDamaged(new Hit(100, 0, Element.None)); dead.Witch.Update(); Assert.Equal(0, dead.Fan.Casts.Count);
            var missing = new Fixture(noFan: true); missing.Witch.OnDamaged(new Hit(50, 0, Element.None)); Assert.True(!missing.Witch.HasVineFan); Assert.True(!missing.Witch.CastMagic(missing.Target));
            var odd = new Fixture(hp: 101); odd.Witch.OnDamaged(new Hit(50, 0, Element.None)); odd.Witch.Update(); Assert.Equal(0, odd.Fan.Casts.Count); odd.Witch.OnDamagedWithDelay(new Hit(1, 0, Element.None), (Fix64)0.05m); odd.Witch.Update(); Assert.Equal(1, odd.Fan.Casts.Count);
            Assert.True(odd.Witch.IsObjectiveTarget); Assert.Equal(50, odd.Witch.Hp);
            var real = new Fixture(new VineFanMagic()); VinePrefabs.Register(real.Context); real.Context.GameParameters["vine.radius"] = (Fix64)0.25m; real.Context.GameParameters["vine.damage"] = 10; real.Context.GameParameters["vine.duration"] = 4; real.Context.GameParameters["vine_toss.range"] = 6;
            real.Context.GameParameters["vine_toss.vine_count"] = 3; real.Context.GameParameters["vine_toss.vine_spacing"] = 1; real.Context.GameParameters["vine_toss.vine_spawn_interval"] = (Fix64)0.1m;
            real.Witch.OnDamaged(new Hit(50, 0, Element.None)); real.Witch.Update(); Assert.Equal(3, real.Context.PendingObjects.Length);
            var timed = new Fixture(); timed.Witch.OnDamaged(new Hit(50, 0, Element.None));
            for (int frame = 1; frame <= 28; frame++) { timed.Witch.Update(); int expected = frame < 9 ? 1 : frame < 18 ? 2 : frame < 27 ? 3 : 4; Assert.Equal(expected, timed.Fan.Casts.Count); }
            var reacquire = new Fixture(); reacquire.Witch.Target = reacquire.Target; reacquire.Target.Destroy(); var next = reacquire.Context.Spawn(Master.LeftPlayer, "Object", new Vector3(2, 8, 0)); next.AddComponentImmediately(new DummyMob(next, 20)); next.AddCollider(new CircleCollider(next, 1)); reacquire.Context.FlushObjects(); reacquire.Witch.OnDamaged(new Hit(50, 0, Element.None)); reacquire.Witch.Update(); Assert.Equal(0, reacquire.Fan.Casts.Count); Assert.Equal(next, reacquire.Witch.Target); Assert.Equal((Fix64)1, reacquire.Witch.TargetRadius);
            next.SetPosition(new Vector3(2, 6, 0)); reacquire.Witch.Update(); Assert.Equal(1, reacquire.Fan.Casts.Count); Assert.Equal(next.Position, reacquire.Fan.Casts[0].target); Assert.True(reacquire.Witch.Owner.HasComponent<IGaugeComponent>());
        }
    }
}
