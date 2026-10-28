using System.Linq;
using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class PveEvilEntParityTests {
        private const string Source = "game/domain/object/component/mob/statemachine/attacker/pve_evil_ent_mob.rs";
        [PortedTest(Source)] public static void card_evil_ent_is_unchanged() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; EvilEntPrefabs.Register(c);
            foreach (var name in new[] { "FireField", "LeafField" }) c.RegisterPrefab(name, _ => { });
            string[] keys = { "mass", "radius", "hp", "speed", "damage", "attack_interval", "attack_range", "projectile_speed", "sub_damage", "sub_attack_range", "sub_attack_interval", "pull_mass_limit" };
            decimal[] values = { 10, 1.2m, 180, 0.45m, 9, 1.8m, 5, 14, 28, 6, 12, 5 };
            for (int i = 0; i < keys.Length; i++) c.GameParameters["evil_ent." + keys[i]] = (Fix64)values[i];
            var ent = c.Spawn(Master.RightPlayer, "EvilEnt", new Vector3(14, 0, 5)); c.FlushObjects(); var mob = ent.GetComponent<MobComponent>();
            Assert.True(mob is EvilEntMob && !(mob is PveEvilEntMob)); Assert.Equal((Fix64)0.45m, mob.Speed.Total); Assert.True(!ent.HasComponent<IPveObjectiveTarget>());
            mob.ApplyDamage(new Hit(5000, 0, Element.None)); Assert.True(c.PendingObjects.Any(o => o.Prefab == "FireField" || o.Prefab == "LeafField"));
        }
        private sealed class Fixture {
            public readonly SimulationContext Context = new SimulationContext { DeltaTime = (Fix64)0.05m };
            public readonly SimObject Boss;
            public PveEvilEntMob Mob => Boss.GetComponent<PveEvilEntMob>();
            public Fixture(bool cardRanges = false) {
                Context.InitializationFailed += (o, e) => throw e; EvilEntPrefabs.Register(Context); Context.RegisterPrefab("Target", _ => { });
                string[] keys = { "mass", "radius", "hp", "damage", "attack_interval", "attack_range", "projectile_speed", "sub_damage", "sub_attack_range", "sub_attack_interval", "pull_mass_limit" };
                decimal[] values = { 10, 1.2m, 3000, 9, 1.8m, cardRanges ? 5 : 20, 14, 28, cardRanges ? 6 : 20, 12, 5 };
                for (int i = 0; i < keys.Length; i++) Context.GameParameters["pve_evil_ent." + keys[i]] = (Fix64)values[i];
                Boss = Context.Spawn(Master.RightPlayer, "PveEvilEnt", new Vector3(14, 0, 5)); Context.FlushObjects();
            }
            public SimObject Target(int x = 1, bool body = false) {
                var target = Context.Spawn(Master.LeftPlayer, "Target", new Vector3(x, 0, 5)); target.AddCollider(new CircleCollider(target, 1));
                if (body) target.AddComponentImmediately(new RigidBody(target, 1));
                target.AddComponentImmediately(new BehaviorMob(target, 1000, 0, 3, 0, 0, null)); Context.FlushObjects(); return target;
            }
            public void Tick(int frames) { for (int i = 0; i < frames; i++) foreach (var o in Context.Objects) o.Update(); }
        }
        [PortedTest(Source)] public static void is_stationary_and_is_the_health_mob_the_installer_finds() {
            var f = new Fixture(); f.Target(); f.Tick(600); Assert.Equal(new Vector3(14, 0, 5), f.Boss.Position); Assert.Equal(Fix64.Zero, f.Mob.Speed.Total); Assert.Equal(3000, f.Mob.MaxHp);
            Assert.True(f.Boss.HasComponent<ICollidable>() && f.Boss.Elements.NativeHas(Element.Nature) && f.Boss.Elements.NativeHas(Element.Fire));
        }
        [PortedTest(Source)] public static void max_hp_override_applies() { var f = new Fixture(); f.Mob.OverrideMaxHp(400); Assert.Equal(400, f.Mob.Hp); Assert.Equal(400, f.Mob.MaxHp); }
        [PortedTest(Source)] public static void destroyed_boss_is_terminal_and_leaves_no_death_field() { var f = new Fixture(); Assert.True(!PveObjectiveTarget.IsTerminal(f.Boss)); f.Mob.ApplyDamage(new Hit(5000, 0, Element.None)); Assert.True(f.Boss.IsDestroyed && PveObjectiveTarget.IsTerminal(f.Boss)); Assert.True(!f.Context.PendingObjects.Any(o => o.Prefab == "FireField" || o.Prefab == "LeafField")); }
        [PortedTest(Source)] public static void hp_percent_trigger_reads_the_boss_hp() { var f = new Fixture(); f.Mob.ApplyDamage(new Hit(1500, 0, Element.None)); Assert.Equal(1500, f.Mob.Hp); Assert.Equal((Fix64)0.5m, (Fix64)f.Mob.Hp / f.Mob.MaxHp); }
        [PortedTest(Source)] public static void punches_the_player_from_x14_when_the_range_covers_the_arena() { var f = new Fixture(); var p = f.Target(); f.Tick(200); Assert.True(p.GetComponent<MobComponent>().Hp < 1000); Assert.Equal((Fix64)14, f.Boss.Position.X); }
        [PortedTest(Source)] public static void the_card_unit_ranges_do_not_reach_across_the_arena() { var f = new Fixture(true); var p = f.Target(); f.Tick(600); Assert.Equal(1000, p.GetComponent<MobComponent>().Hp); }
        [PortedTest(Source)] public static void drags_a_player_unit_across_the_arena_and_hits_it_with_the_fire_fist() { var f = new Fixture(); var p = f.Target(2, true); f.Tick(320); Assert.True(p.Position.X > 10); Assert.True(p.GetComponent<MobComponent>().Hp < 991); }
        [PortedTest(Source)] public static void switches_its_punches_to_a_unit_that_appears_closer_than_the_player() { var f = new Fixture(); var p = f.Target(); f.Tick(80); int hp = p.GetComponent<MobComponent>().Hp; Assert.True(hp < 1000); var unit = f.Target(10); f.Tick(240); Assert.True(unit.GetComponent<MobComponent>().Hp < 1000); Assert.True(p.GetComponent<MobComponent>().Hp >= hp - 9); }
        [PortedTest(Source)] public static void returns_to_the_player_when_the_units_are_gone() { var f = new Fixture(); var p = f.Target(); var unit = f.Target(10); f.Tick(60); Assert.Equal(1000, p.GetComponent<MobComponent>().Hp); unit.Destroy(); f.Context.RemoveObject(unit); f.Tick(120); Assert.True(p.GetComponent<MobComponent>().Hp < 1000); }
    }
}
