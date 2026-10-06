using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class ObjectSfxTests
    {
        private const string CatalogPath = "Assets/Resources/Sound/Config/ObjectSfxCatalog.asset";
        private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
        private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static Object Catalog => AssetDatabase.LoadAssetAtPath<Object>(CatalogPath);
        private static object Entry(string runtimeType) => ((IEnumerable)Property(Catalog, "Entries"))
            .Cast<object>().Single(entry => (string)Property(entry, "RuntimeType") == runtimeType);

        [TestCase("GroundTower", "TowerAttack")]
        [TestCase("DragonTower", "TowerAttack")]
        [TestCase("Towerback", "TowerAttack")]
        [TestCase("AquaArcher", "WaterLaunch")]
        [TestCase("WaterSlime", "WaterLaunch")]
        [TestCase("CloudDragon", "WaterLaunch")]
        [TestCase("FireChildSpirit", "FireLaunch")]
        [TestCase("FireSpirit", "FireLaunch")]
        [TestCase("FireSlime", "SmallAttack")]
        [TestCase("FireTadpole", "SmallAttack")]
        [TestCase("ThunderSpirit", "SmallAttack")]
        [TestCase("MiniRock", "SmallAttack")]
        [TestCase("StormStag", "SwingAttack")]
        [TestCase("RockGolem", "SwingAttack")]
        [TestCase("SeaSerpent", "SwingAttack")]
        public void AttackClassificationMatchesApprovedRules(string runtimeType, string expectedProfile)
        {
            object entry = Entry(runtimeType);
            object attack = Property(entry, "AttackProfile");
            Assert.AreEqual(expectedProfile, Property(attack, "ProfileId"));
            Assert.IsTrue((bool)Property(Property(entry, "EffectiveAttack"), "Enabled"));
        }

        [TestCase("FireLordSpirit")]
        [TestCase("DimensionToad")]
        [TestCase("HealingTotem")]
        [TestCase("RepairTotem")]
        public void NonAttackingProducersAndSupportDoNotGetFakeAttacks(string runtimeType)
        {
            Assert.IsNull(Property(Entry(runtimeType), "AttackProfile"));
            Assert.IsFalse((bool)Property(Property(Entry(runtimeType), "EffectiveAttack"), "Enabled"));
        }

        [TestCase("FireShot")]
        [TestCase("WaterShot")]
        [TestCase("MagmaFist")]
        [TestCase("DragonFlame")]
        public void UnitProjectileSpawnDoesNotDuplicateLaunch(string runtimeType)
        {
            object profile = Property(Entry(runtimeType), "Profile");
            Assert.IsFalse((bool)Property(Property(profile, "Spawn"), "Enabled"));
        }

        [TestCase("ChainLightning")]
        [TestCase("TideCall")]
        [TestCase("WindBlade")]
        [TestCase("FireExplode")]
        public void StandaloneReleasesAndExplosionsRemainAudible(string runtimeType)
        {
            object slot = Property(Property(Entry(runtimeType), "Profile"), "Spawn");
            Assert.IsTrue((bool)Property(slot, "Enabled"));
            Assert.IsNotNull(Property(slot, "Clip"));
        }

        [Test]
        public void EffectiveAttackUsesFallbackButDisabledOverrideAndSilenceWin()
        {
            Type entryType = RuntimeType("Sound.Config.ObjectSfxCatalogEntry");
            object entry = Activator.CreateInstance(entryType);
            Object enabledProfile = AssetDatabase.LoadAssetAtPath<Object>(
                "Assets/Resources/Sound/Config/Profiles/WaterLaunch.asset");
            ScriptableObject disabledProfile = ScriptableObject.CreateInstance(RuntimeType("Sound.Config.ObjectSfxProfile"));
            try
            {
                Set(entry, "profile", enabledProfile);
                Assert.AreSame(Property(enabledProfile, "Attack"), Property(entry, "EffectiveAttack"));
                Set(entry, "attackProfile", disabledProfile);
                Assert.AreSame(Property(disabledProfile, "Attack"), Property(entry, "EffectiveAttack"));
                Assert.IsFalse((bool)Property(Property(entry, "EffectiveAttack"), "Enabled"));
                Set(entry, "profile", null);
                Assert.AreSame(Property(disabledProfile, "Attack"), Property(entry, "EffectiveAttack"));
                Set(entry, "intentionalSilent", true);
                Assert.IsNull(Property(entry, "EffectiveAttack"));
            }
            finally { Object.DestroyImmediate(disabledProfile); }
        }

        [UnityTest]
        public IEnumerator ControllerAttachesOneAttackSubscriptionAndRemovesItOnDestroy()
        {
            // Ordinary MonoBehaviour destruction callbacks are not executed in Edit Mode.
            yield return new EnterPlayMode();
            var target = new GameObject("SFX test");
            try
            {
                Type servedType = RuntimeType("GameScene.ServedObjectComponent.ServedObject");
                Component served = target.AddComponent(servedType);
                Type controllerType = RuntimeType("GameScene.ServedObjectComponent.Sound.ServedObjectSfxController");
                MethodInfo attach = controllerType.GetMethod("Attach");
                attach.Invoke(null, new object[] { served, "AquaArcher", false });
                attach.Invoke(null, new object[] { served, "AquaArcher", false });
                Assert.AreEqual(1, target.GetComponents(controllerType).Length);
                FieldInfo attackEvent = servedType.GetField("OnAttack", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.AreEqual(1, ((Delegate)attackEvent.GetValue(served)).GetInvocationList().Length);
                Object.Destroy(target.GetComponent(controllerType));
                yield return null;
                Assert.IsNull(attackEvent.GetValue(served));
            }
            finally { Object.Destroy(target); }
            yield return new ExitPlayMode();
        }

        [Test]
        public void BuilderPreservesAttackTuningAndAliasRows()
        {
            Object catalog = Catalog;
            string original = EditorJsonUtility.ToJson(catalog);
            try
            {
                Set(Entry("WaterSlime"), "attackProfile", AssetDatabase.LoadAssetAtPath<Object>(
                    "Assets/Resources/Sound/Config/Profiles/SmallAttack.asset"));
                RuntimeType("ObjectSfxCatalogBuilder").GetMethod("CreateOrUpdate").Invoke(null, null);
                Assert.AreEqual("SmallAttack", Property(Property(Entry("WaterSlime"), "AttackProfile"), "ProfileId"));
                foreach (string alias in new[] { "ElectricSummon", "FireSummon", "RockSummon", "WindSummon", "ElectricAbsorb", "LightningDrop" })
                    Assert.IsTrue((bool)Property(Entry(alias), "ServerAlias"), alias);
            }
            finally
            {
                EditorJsonUtility.FromJsonOverwrite(original, catalog);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
        }

        [Test]
        public void CatalogCoversPrefabsAndHasNoLegacySoundOwnersOrMissingScripts()
        {
            var errors = (IEnumerable)RuntimeType("ObjectSfxCatalogValidator").GetMethod("Validate").Invoke(null, null);
            CollectionAssert.IsEmpty(errors);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BgmPlayer.prefab")
                .GetComponentInChildren<AudioSource>(true), "BGM must retain its source.");
        }
    }
}
