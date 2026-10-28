using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class ElectricDeathParityTests {
        private const string Source = "game/domain/object/component/effect/electric_death_energy.rs";
        private static SimulationContext Context() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; c.RegisterPrefab("Object", _ => { }); c.GameParameters["electric_field.radius"] = 5; c.GameParameters["electric_field.duration"] = 6; return c;
        }
        private static SimObject Candidate(SimulationContext c, Fix64 x, Master side = Master.LeftPlayer, bool lightning = true, bool mob = true, bool active = true, bool eligible = true) {
            var o = c.Spawn(side, "Object", new Vector3(x, 0, 4)); if (lightning) o.Elements.AddNative(Element.Lightning);
            if (mob) o.AddComponentImmediately(new DummyMob(o, 10)); if (eligible) o.AddComponentImmediately(new ElectricDeathEnergy(o)); if (!active) o.SetStatus(ObjectStatus.Destroyed); return o;
        }
        [PortedTest(Source)] public static void leaves_the_electric_field_to_death_field_without_an_absorber() {
            var c = Context(); var o = Candidate(c, 3); o.SetPosition(new Vector3(3, 2, 4)); c.FlushObjects(); o.GetComponent<ElectricDeathEnergy>().OnCombatDeath(); Assert.Equal(0, c.PendingObjects.Length); Assert.Equal(0, c.DrainEvents().Length);
        }
        [PortedTest(Source)] public static void selects_nearest_eligible_ally_and_emits_position_to_reference_absorption() {
            var c = Context(); var o = Candidate(c, 3);
            var excluded = new[] { Candidate(c, (Fix64)3.2m, Master.RightPlayer), Candidate(c, (Fix64)3.3m, lightning:false), Candidate(c, (Fix64)3.4m, active:false), Candidate(c, (Fix64)3.5m, eligible:false), Candidate(c, 9), Candidate(c, 6), Candidate(c, (Fix64)3.1m, mob:false) };
            var nearest = Candidate(c, 4); c.FlushObjects(); o.GetComponent<ElectricDeathEnergy>().OnCombatDeath();
            Assert.Equal(1, nearest.GetPendingComponents<OverchargeStatusEffect>().Length); foreach (var candidate in excluded) Assert.Equal(0, candidate.GetPendingComponents<OverchargeStatusEffect>().Length);
            var events = c.DrainEvents(); Assert.Equal(1, events.Length); Assert.Equal("ElectricAbsorb", events[0].Type); Assert.Equal((Fix64)0.35m, events[0].Duration); Assert.Equal(new Vector3(3, 0, 4), events[0].StartPosition.Value); Assert.Equal(nearest.Id, events[0].TargetId); Assert.True(!events[0].EndPosition.HasValue);
        }
        [PortedTest(Source)] public static void handles_repeated_combat_death_only_once() {
            var c = Context(); var o = Candidate(c, 3); var target = Candidate(c, 4); c.FlushObjects(); var energy = o.GetComponent<ElectricDeathEnergy>(); energy.OnCombatDeath(); energy.OnCombatDeath();
            Assert.Equal(1, target.GetPendingComponents<OverchargeStatusEffect>().Length); Assert.Equal((Fix64)6, target.GetPendingComponents<OverchargeStatusEffect>()[0].Remaining); Assert.Equal(1, c.DrainEvents().Length);
        }
        [PortedTest(Source)] public static void extends_pending_overcharge_by_the_configured_electric_field_duration() {
            var c = Context(); var o = Candidate(c, 3); var target = Candidate(c, 4); c.FlushObjects(); var effect = new OverchargeStatusEffect(target, 1); target.AddComponent(effect); o.GetComponent<ElectricDeathEnergy>().OnCombatDeath();
            Assert.Equal((Fix64)7, effect.Remaining); Assert.Equal(1, target.GetPendingComponents<OverchargeStatusEffect>().Length);
        }
        public static void CheckNestDeath() {
            var c = Context(); ElementalNests.Register(c); FieldPrefabs.RegisterElectricField(c);
            c.GameParameters["electric_summon.radius"] = 1; c.GameParameters["electric_summon.hp"] = 20;
            c.GameParameters["electric_slime.mass"] = 1; c.GameParameters["electric_slime.radius"] = (Fix64)0.4m; c.GameParameters["electric_slime.hp"] = 30;
            c.GameParameters["electric_slime.speed"] = 2; c.GameParameters["electric_slime.damage"] = 4; c.GameParameters["electric_slime.attack_interval"] = 1;
            var registry = new MagicRegistry(); BuildMagics.Register(registry); registry.GetBean("lightning_slime_nest").Bind(1, "lightning_slime_nest", Element.Lightning).Run(c, Master.LeftPlayer, null, new Vector3(5, 0, 5));
            var nest = c.PendingObjects[0]; c.FlushObjects(); c.DeltaTime = 2; nest.Update(); var first = c.PendingObjects[0]; c.FlushObjects(); nest.Update(); var second = c.PendingObjects[0]; c.FlushObjects();
            Assert.Equal("ElectricSlime", first.Prefab); Assert.True(first.HasComponent<LightningSummonEffectReceiver>()); Assert.Equal(Master.LeftPlayer, second.Master);
            first.GetComponent<Slime>().ApplyDamage(new Hit(30, 0, Element.None)); Assert.True(first.IsDestroyed); Assert.Equal(1, c.PendingObjects.Length); Assert.Equal("ElectricField", c.PendingObjects[0].Prefab);
            Assert.Equal(1, second.GetPendingComponents<OverchargeStatusEffect>().Length); Assert.Equal((Fix64)6, second.GetPendingComponents<OverchargeStatusEffect>()[0].Remaining); c.FlushObjects(); Assert.Equal((Fix64)3, second.GetComponent<Slime>().Speed.Total);
        }
    }
}
