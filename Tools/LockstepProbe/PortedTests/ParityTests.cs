using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PortedTestAttribute : Attribute {
        public string Source { get; }
        public PortedTestAttribute(string source) { Source = source; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequiresThreadsAttribute : Attribute { }

    public static class ParityTests {
        public static int Run() {
            int passed = 0;
            var failures = new List<string>();
            foreach (var suite in new[] { typeof(RemainingSessionParityTests), typeof(FrameOutputParityTests), typeof(RandomParityTests), typeof(ResultCheckerParityTests), typeof(BattleLoopParityTests), typeof(PrefabAllowlistParityTests), typeof(PrefabAllowlistParityTests.Drop), typeof(PrefabAllowlistParityTests.Explode), typeof(PrefabAllowlistParityTests.Subprefab), typeof(WaterPrefabParityTests), typeof(WindPrefabParityTests), typeof(CastPlacementParityTests), typeof(EconomyParityTests), typeof(ObjectParityTests), typeof(MobParityTests), typeof(EffectParityTests), typeof(NavigationParityTests), typeof(BehaviorParityTests), typeof(DetectorParityTests), typeof(TerrainParityTests), typeof(ColliderParityTests), typeof(PhysicsParityTests), typeof(SpeedTrackerParityTests), typeof(CowardParityTests), typeof(OverchargeParityTests), typeof(StormStagParityTests), typeof(ProjectileParityTests), typeof(SpecialShotParityTests), typeof(VineParityTests), typeof(SeedSpiritParityTests), typeof(NatureUnitParityTests), typeof(SpawnerParityTests), typeof(HealingParityTests), typeof(RepairAuraParityTests), typeof(WindPushParityTests), typeof(LightningCloudParityTests), typeof(ShockTrapParityTests), typeof(ExplosionParityTests), typeof(DropParityTests), typeof(ManaWellParityTests), typeof(MagicInputParityTests), typeof(MagicCatalogParityTests), typeof(DropMagicParityTests), typeof(RallyParityTests), typeof(BuildMagicParityTests), typeof(GrassSpreadParityTests), typeof(RockTowerParityTests), typeof(RockRemnantParityTests), typeof(RockUnitParityTests), typeof(TitanParityTests), typeof(FireworkParityTests), typeof(CraterParityTests), typeof(FlameParityTests), typeof(ElectricDeathParityTests), typeof(TowerbackParityTests), typeof(SpawnMagicParityTests), typeof(TornadoParityTests), typeof(CraterCarrierParityTests), typeof(BeanParityTests), typeof(SelfDestructParityTests), typeof(BombSpriteParityTests), typeof(ThunderBirdParityTests), typeof(CloudDragonParityTests), typeof(ShotMagicParityTests), typeof(RollingRockParityTests), typeof(VineMagicParityTests), typeof(RayParityTests), typeof(SpiritBombParityTests), typeof(TidalWarheadParityTests), typeof(RemnantVineMagicParityTests), typeof(AreaMagicParityTests), typeof(PveBossParityTests), typeof(VineWitchParityTests), typeof(SimpleBossPrefabParityTests), typeof(DimensionToadParityTests), typeof(CorruptedSpiritParityTests), typeof(EvilEntParityTests), typeof(PveEvilEntParityTests), typeof(PveEvilEntPrefabParityTests), typeof(SeaSerpentParityTests), typeof(SummonerParityTests), typeof(WaterSlimeParityTests), typeof(BrickGolemParityTests), typeof(WallGolemParityTests), typeof(VineColonyParityTests), typeof(BuildTowerPrefabParityTests), typeof(BuildEffectPrefabParityTests), typeof(LightningPrefabParityTests), typeof(NaturePrefabParityTests), typeof(PrefabCatalogParityTests), typeof(RuneParityTests), typeof(AuraParityTests), typeof(CardSelectedParityTests), typeof(CardVisualizerParityTests), typeof(GameActionParityTests), typeof(ContextParityTests), typeof(BotLifecycleParityTests), typeof(BotEyeParityTests), typeof(MotionTrackerParityTests), typeof(CastDeadlineParityTests), typeof(BotSkillParityTests), typeof(BotPersonaParityTests), typeof(UnitCatalogueParityTests), typeof(BotSpellStatsParityTests), typeof(HospitalityParityTests), typeof(ThreatParityTests), typeof(PlacementPlannerParityTests), typeof(BlastTargetParityTests), typeof(BotCounterParityTests), typeof(MagicMetadataParityTests), typeof(BotBrainParityTests), typeof(BotBrainCapabilityParityTests), typeof(BotActionParityTests), typeof(BotAgentParityTests), typeof(BotEmoteParityTests), typeof(BotAgentSystemParityTests), typeof(SessionEmoteParityTests), typeof(DeckSeedParityTests), typeof(SessionDefinitionParityTests), typeof(BattleFieldParityTests), typeof(PveInstallerParityTests), typeof(PveResultParityTests), typeof(PveObjectiveParityTests), typeof(PveStateParityTests), typeof(PveScriptParityTests), typeof(PveShieldParityTests), typeof(PveMatchParityTests), typeof(PveRowsParityTests), typeof(PveReplayParityTests), typeof(PveInstallActionParityTests), typeof(PvePullParityTests) }) {
                foreach (var method in suite.GetMethods(BindingFlags.Public | BindingFlags.Static).OrderBy(method => method.Name, StringComparer.Ordinal)) {
                    var origin = method.GetCustomAttribute<PortedTestAttribute>();
                    if (origin == null) continue;
                    string id = origin.Source + "::" + method.Name;
#if UNITY_WEBGL && !UNITY_EDITOR
                    if (method.IsDefined(typeof(RequiresThreadsAttribute), false)) {
                        Console.WriteLine("PARITY SKIP " + id + " (desktop thread contract)");
                        continue;
                    }
#endif
                    try { method.Invoke(null, null); passed++; Console.WriteLine("PARITY PASS " + id); }
                    catch (TargetInvocationException exception) { failures.Add(id + ": " + exception.InnerException); }
                }
            }
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
            Console.WriteLine("PASS ported tests=" + passed);
            return passed;
        }
    }

    internal static class Assert {
        public static void Equal<T>(T expected, T actual) {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; actual {actual}");
        }
        public static void True(bool actual) { Equal(true, actual); }
        public static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual) {
            if (!expected.SequenceEqual(actual)) throw new Exception("Sequence mismatch");
        }
        public static void Near(Fix64 expected, Fix64 actual, Fix64 tolerance) {
            if (Fix64.Abs(expected - actual) > tolerance) throw new Exception($"Expected {expected}; actual {actual}");
        }
    }
}









