using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class HealingParityTests {
        private static SimObject Object(SimulationContext context, Master master, Vector3 position) {
            return context.Spawn(master, "ZapMouse", position);
        }
        private static SimulationContext Context(Fix64 delta) {
            var context = new SimulationContext(1) { DeltaTime = delta }; context.RegisterPrefab("ZapMouse", _ => { }); return context;
        }
        [PortedTest("tests/mob_simple.rs")] public static void a_totem_applies_its_damage_to_allies_in_range_once_per_interval() {
            var context = Context(1); var owner = Object(context, Master.LeftPlayer, Vector3.Zero);
            var totem = new Totem(owner, 10, -3, 1, 5, 3);
            var ally = Object(context, Master.LeftPlayer, new Vector3(1, 0, 0)); var enemy = Object(context, Master.RightPlayer, new Vector3(1, 0, 0));
            var allyMob = new DummyMob(ally, 20); var enemyMob = new DummyMob(enemy, 20); ally.AddComponentImmediately(allyMob); enemy.AddComponentImmediately(enemyMob);
            allyMob.ApplyDamage(new Hit(10, 0, Element.None)); enemyMob.ApplyDamage(new Hit(10, 0, Element.None)); context.FlushObjects();
            totem.Update(); Assert.Equal(13, allyMob.Hp); Assert.Equal(10, enemyMob.Hp); Assert.Equal(ObjectStatus.Attack, owner.Status);
        }
        [PortedTest("tests/mob_simple.rs")] public static void a_self_healer_heals_its_own_mob_when_the_interval_has_passed() {
            var context = Context((Fix64)0.6m); var owner = Object(context, Master.LeftPlayer, Vector3.Zero);
            var mob = new DummyMob(owner, 20); owner.AddComponentImmediately(mob); mob.ApplyDamage(new Hit(10, 0, Element.None));
            var healer = new SelfHealer(owner, 4, 1, Element.Nature); owner.AddComponentImmediately(healer); healer.Start();
            healer.Update(); Assert.Equal(10, mob.Hp); healer.Update(); Assert.Equal(14, mob.Hp);
        }
        private sealed class Objective : SimComponent, IPveObjectiveTarget {
            public Objective(SimObject owner) : base(owner) { }
            public bool IsTerminal { get; set; }
        }
        [PortedTest("tests/mob_simple.rs")] public static void remove_lifetime_components_removes_the_timed_self_destroyer_at_the_next_flush() {
            var context = Context(1); var owner = Object(context, Master.LeftPlayer, Vector3.Zero);
            owner.AddComponentImmediately(new TimedSelfDestroyer(owner, 5)); owner.AddComponentImmediately(new DummyMob(owner, 20));
            new PveScenarioComponents().RemoveLifetimeComponents(owner); owner.FlushComponents();
            Assert.True(!owner.HasComponent<TimedSelfDestroyer>()); Assert.True(owner.HasComponent<DummyMob>());
        }
        public static void CheckScenarioComponents() {
            var context = Context(1); var owner = Object(context, Master.LeftPlayer, Vector3.Zero); var mob = new DummyMob(owner, 20); owner.AddComponentImmediately(mob);
            bool up = true; var components = new PveScenarioComponents(); components.AddShield(owner, () => up); owner.FlushComponents();
            Assert.True(owner.HasEffect(VisualEffect.Bubble)); mob.OnDamaged(new Hit(3, 0, Element.None)); mob.ApplyDamage(new Hit(3, 0, Element.None)); Assert.Equal(20, mob.Hp);
            up = false; mob.ApplyDamage(new Hit(5, 0, Element.None)); Assert.Equal(15, mob.Hp);
            up = true; mob.OnDamaged(new Hit(-2, 0, Element.None)); Assert.Equal(17, mob.Hp);
            mob.OnDamagedWithDelay(new Hit(5, 0, Element.None), 1); mob.Update(); Assert.Equal(17, mob.Hp);
            up = false; owner.GetComponent<PveShieldComponent>().Update(); Assert.True(!owner.HasEffect(VisualEffect.Bubble));
            owner.AddComponentImmediately(new RandomAreaSpawner(owner, "ZapMouse", 10, 1, 1)); components.RemoveLifetimeComponents(owner); owner.FlushComponents(); Assert.True(!owner.HasComponent<RandomAreaSpawner>());
            components.AddSpawner(owner, "ZapMouse", 1, 2); owner.FlushComponents(); var spawner = owner.GetComponent<Spawner>(); Assert.True(spawner != null); Assert.Equal((Fix64)(-1), spawner.GetGauge().MaxValue);
            context.FlushObjects(); spawner.Update(); Assert.Equal(2, context.PendingObjects.Length); components.RemoveSpawner(owner); owner.FlushComponents(); Assert.True(!owner.HasComponent<Spawner>());
            components.AddSpawner(owner, null, 1, 2); owner.FlushComponents(); Assert.True(!owner.HasComponent<Spawner>());
        }
        public static void CheckLifeTreeAndPveHealth() {
            var context = Context(1); NatureUnitPrefabs.Register(context);
            foreach (var name in new[] { "mass", "radius", "attack_interval", "duration" }) context.GameParameters["life_tree." + name] = 1;
            context.GameParameters["life_tree.hp"] = 20; context.GameParameters["life_tree.damage"] = -3; context.GameParameters["life_tree.effect_radius"] = 5;
            var tree = context.Spawn(Master.LeftPlayer, "LifeTree", new Vector3(5, 0, 5));
            Assert.True(!tree.IsDestroyed); Assert.True(tree.HasComponent<IFrenzyImmune>() && tree.HasComponent<IMindControlImmune>());
            var totem = tree.GetComponent<Totem>(); totem.ApplyDamage(new Hit(5, 0, Element.None));
            var aerial = Object(context, Master.LeftPlayer, new Vector3(5, 3, 5)); var mob = new DummyMob(aerial, 20); aerial.AddComponentImmediately(mob); mob.ApplyDamage(new Hit(10, 0, Element.None));
            var far = Object(context, Master.LeftPlayer, new Vector3(11, 0, 5)); var farMob = new DummyMob(far, 20); far.AddComponentImmediately(farMob); farMob.ApplyDamage(new Hit(10, 0, Element.None));
            context.FlushObjects(); totem.Update(); Assert.Equal(18, totem.Hp); Assert.Equal(13, mob.Hp); Assert.Equal(10, farMob.Hp);
            var health = tree.GetComponent<IPveHealthMob>(); health.OverrideMaxHp(30); Assert.Equal(30, health.Hp); Assert.Equal(30, health.MaxHp); health.OverrideMaxHp(0); Assert.Equal(30, health.MaxHp);
            Assert.True(!health.IsSpawner && !health.IsObjectiveTarget); Assert.True(new Spawner(tree, 1, "ZapMouse").IsSpawner);
            Assert.True(PveObjectiveTarget.IsTerminal(null)); Assert.True(!PveObjectiveTarget.IsTerminal(tree));
            var objective = new Objective(tree); tree.AddComponentImmediately(objective); objective.IsTerminal = true; Assert.True(PveObjectiveTarget.IsTerminal(tree));
            objective.IsTerminal = false; tree.Destroy(); Assert.True(PveObjectiveTarget.IsTerminal(tree));
            var healer = new SelfHealer(aerial, 2, 1, Element.None); healer.Start(); healer.Update(); Assert.Equal(13, mob.Hp); healer.Update(); Assert.Equal(15, mob.Hp);
            Assert.True(!aerial.HasComponent<SelfAttacker>());
        }
    }
}
