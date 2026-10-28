using ArcaneCasters.Simulation;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    public static class LightningPrefabParityTests {
        [PortedTest("game/domain/object/prefab/implement/lightning/electric_field_prefab_initializer.rs")] public static void uses_configured_electric_field_radius_and_duration() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; FieldPrefabs.RegisterElectricField(c); c.GameParameters["electric_field.radius"] = (Fix64)0.5m; c.GameParameters["electric_field.duration"] = 3;
            var field = c.Spawn(Master.LeftPlayer, "ElectricField", new Vector3(1, 2, 3)); Assert.Equal(new Vector3(1, 0, 3), field.Position); Assert.Equal((Fix64)0.5m, field.Colliders[0].CircleRadius.Value); Assert.True(field.Elements.Has(Element.Lightning)); Assert.Equal((Fix64)3, field.GetComponent<TimedSelfDestroyer>().GetGauge().MaxValue);
        }
        [PortedTest("game/domain/object/prefab/implement/lightning/storm_stag_prefab_initializer.rs")] public static void starts_permanently_overcharged() {
            var c = new SimulationContext(); c.InitializationFailed += (o, e) => throw e; StormStagPrefabs.Register(c);
            foreach (var key in new[] { "hp", "speed", "mass", "radius", "damage", "acceleration", "detection_range", "panic_duration" }) c.GameParameters["storm_stag." + key] = 0;
            var stag = c.Spawn(Master.LeftPlayer, "StormStag", Vector3.Zero); var effect = stag.GetComponent<OverchargeStatusEffect>(); Assert.True(effect.IsPermanent && effect.Remaining > 0); Assert.True(stag.HasEffect(VisualEffect.Overcharge));
            c.FlushObjects(); c.DeltaTime = 10; effect.Update(); Assert.Equal(Fix64.MaxValue, effect.Remaining); OverchargeStatusEffect.Apply(stag, 3); Assert.Equal(Fix64.MaxValue, effect.Remaining);
            Assert.True(stag.HasComponent<StormStagMob>() && stag.HasComponent<MovementSpeedTracker>() && stag.HasComponent<CommonEffectReceiver>() && stag.HasComponent<ZPhysics>());
        }
    }
}
