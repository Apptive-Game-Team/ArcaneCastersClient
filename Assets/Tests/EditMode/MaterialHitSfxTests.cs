using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameScene.Dto;
using GameScene.Dto.Event;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class MaterialHitSfxTests
    {
        private const string ProfileRoot = "Assets/Resources/Sound/Config/Profiles/";
        private static Type Named(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
        private static object Property(object target, string name) => target.GetType().GetProperty(name).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static Object Catalog => AssetDatabase.LoadAssetAtPath<Object>(
            "Assets/Resources/Sound/Config/ObjectSfxCatalog.asset");
        private static IEnumerable<object> Entries => ((IEnumerable)Property(Catalog, "Entries")).Cast<object>();
        private static object Entry(string name) => Entries.Single(entry => (string)Property(entry, "RuntimeType") == name);
        private static AudioClip Clip(string material) => Resources.Load<AudioClip>("Sound/Game/Hit/" + material + "_onhit_1");

        [TestCase("TreeGolem", "WoodHit")]
        [TestCase("BeanShooter", "WoodHit")]
        [TestCase("PveCorruptedVineSpirit", "WoodHit")]
        [TestCase("LifeTree", "WoodHit")]
        [TestCase("HealingTotem", "WoodHit")]
        [TestCase("FireRune", "WoodHit")]
        [TestCase("RockSummon", "WoodHit")]
        [TestCase("RockGolem", "RockHit")]
        [TestCase("RockMage", "RockHit")]
        [TestCase("PveBrickGolem", "RockHit")]
        [TestCase("DimensionToad", "RockHit")]
        [TestCase("CraterCarrier", "RockHit")]
        [TestCase("MagmaSpirit", "RockHit")]
        [TestCase("WindTotem", "RockHit")]
        [TestCase("ElectricTower", "RockHit")]
        [TestCase("Towerback", "RockHit")]
        [TestCase("Player", "CreatureHit")]
        [TestCase("ArcaneInfantry", "CreatureHit")]
        [TestCase("PveVineWitch", "CreatureHit")]
        [TestCase("SeedSpirit", "CreatureHit")]
        [TestCase("LeafSlime", "CreatureHit")]
        [TestCase("CloudDragon", "CreatureHit")]
        [TestCase("StormStag", "CreatureHit")]
        public void PhysicalMaterialOverridesElement(string runtimeType, string expected)
        {
            object entry = Entry(runtimeType);
            Assert.AreEqual(expected, Property(Property(entry, "HitProfile"), "ProfileId"));
            object hit = Property(entry, "EffectiveHit");
            Assert.IsTrue((bool)Property(hit, "Enabled"));
            Assert.IsNotNull(Property(hit, "Clip"));
        }

        [Test]
        public void TargetsHaveOneHitOwnerAndNonTargetsStaySilent()
        {
            int targets = 0;
            foreach (object entry in Entries)
            {
                object profile = Property(entry, "Profile");
                bool target = profile != null && !(bool)Property(profile, "IsProjectileOrTransient") &&
                    (string)Property(entry, "RuntimeType") != "PveWaterSlimeNest";
                object slot = Property(entry, "EffectiveHit");
                bool audible = slot != null && (bool)Property(slot, "Enabled") && Property(slot, "Clip") != null;
                Assert.AreEqual(target, audible, (string)Property(entry, "RuntimeType"));
                if (target) targets++;
            }
            Assert.AreEqual(82, targets);
        }

        [Test]
        public void HitFallbackDisabledOverrideAndIntentionalSilenceMatchAttackPolicy()
        {
            object entry = Activator.CreateInstance(Named("Sound.Config.ObjectSfxCatalogEntry"));
            Object enabled = AssetDatabase.LoadAssetAtPath<Object>(ProfileRoot + "WoodHit.asset");
            ScriptableObject disabled = ScriptableObject.CreateInstance(Named("Sound.Config.ObjectSfxProfile"));
            try
            {
                Set(entry, "profile", enabled);
                Assert.AreSame(Property(enabled, "Hit"), Property(entry, "EffectiveHit"));
                Set(entry, "hitProfile", disabled);
                Assert.AreSame(Property(disabled, "Hit"), Property(entry, "EffectiveHit"));
                Assert.IsFalse((bool)Property(Property(entry, "EffectiveHit"), "Enabled"));
                Set(entry, "profile", null);
                Assert.AreSame(Property(disabled, "Hit"), Property(entry, "EffectiveHit"));
                Set(entry, "intentionalSilent", true);
                Assert.IsNull(Property(entry, "EffectiveHit"));
            }
            finally { Object.DestroyImmediate(disabled); }
        }

        [Test]
        public void BuilderKeepsHitTuningDisabledOverridesAndAliasMaterials()
        {
            Object catalog = Catalog;
            string original = EditorJsonUtility.ToJson(catalog);
            try
            {
                Object disabled = AssetDatabase.LoadAssetAtPath<Object>(ProfileRoot + "NeutralCreature.asset");
                Set(Entry("RockGolem"), "hitProfile", disabled);
                Named("ObjectSfxCatalogBuilder").GetMethod("CreateOrUpdate").Invoke(null, null);
                Assert.AreSame(disabled, Property(Entry("RockGolem"), "HitProfile"));
                Assert.IsFalse((bool)Property(Property(Entry("RockGolem"), "EffectiveHit"), "Enabled"));
                foreach (string alias in new[] { "ElectricSummon", "FireSummon", "RockSummon", "WindSummon" })
                    Assert.AreEqual("WoodHit", Property(Property(Entry(alias), "HitProfile"), "ProfileId"), alias);
            }
            finally
            {
                EditorJsonUtility.FromJsonOverwrite(original, catalog);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
        }

        [TestCase("creature")]
        [TestCase("wood")]
        [TestCase("rock")]
        public void ImportedClipsAreMonoAndPreloaded(string material)
        {
            AudioClip clip = Clip(material);
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual(44100, clip.frequency);
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            Assert.IsTrue(importer.forceToMono);
            Assert.IsTrue(importer.defaultSampleSettings.preloadAudioData);
            Assert.IsFalse(importer.loadInBackground);
        }

        private static int Voices(AudioClip clip)
        {
            Type type = Named("Sound.GameSfxPlayer");
            object player = type.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            if (player == null) return 0;
            return ((IEnumerable)type.GetField("voices", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(player)).Cast<object>().Count(voice =>
                    (AudioClip)voice.GetType().GetField("Clip").GetValue(voice) == clip);
        }

        private static Component Target(Component container, int id, string runtimeType)
        {
            GameObject host = new GameObject(runtimeType + " hit test");
            host.transform.SetParent(container.transform);
            Component served = host.AddComponent(Named("GameScene.ServedObjectComponent.ServedObject"));
            served.GetType().GetField("id").SetValue(served, id);
            container.GetType().GetMethod("RegisterObject").Invoke(container, new object[] { served });
            Named("GameScene.ServedObjectComponent.Sound.ServedObjectSfxController").GetMethod("Attach")
                .Invoke(null, new object[] { served, runtimeType, false });
            return served;
        }

        private static void Hit(int targetId, int actorId = 999, Component world = null)
        {
            Type handler = Named("GameScene.Handler.GameEventHandler");
            handler.GetMethods().Single(method => method.Name == "Handler" && method.GetParameters().Length == 2)
                .Invoke(Activator.CreateInstance(handler), new object[]
                {
                    new List<GameEvent> { new HitEvent { type = "hit", actorId = actorId, targetId = targetId } }, world
                });
        }

        [UnityTest]
        public IEnumerator LiveHitEventsPlayTargetClipsWithoutActorsOrVisualComponents()
        {
            yield return new EnterPlayMode();
            Component container = new GameObject("Material hit targets").AddComponent(Named("GameScene.Object.ObjectContainer"));
            Target(container, 1, "TreeGolem");
            Target(container, 2, "RockGolem");
            Target(container, 3, "CloudDragon");
            Hit(1);
            Hit(1);
            Hit(2);
            Hit(3);
            Hit(9999);
            Assert.AreEqual(1, Voices(Clip("wood")), "Repeated hits on one target coalesce.");
            Assert.AreEqual(1, Voices(Clip("rock")));
            Assert.AreEqual(1, Voices(Clip("creature")), "Cooldown is per target, not global.");
            Object.Destroy(container.gameObject);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator HitCooldownExpiresAndHpOnlyChangesDoNotPlayAnotherSound()
        {
            yield return new EnterPlayMode();
            Component container = new GameObject("Hit cooldown targets").AddComponent(Named("GameScene.Object.ObjectContainer"));
            Component target = Target(container, 1, "TreeGolem");
            MethodInfo gauge = target.GetType().GetMethod("HandleDamageEffect", BindingFlags.Instance | BindingFlags.NonPublic);
            gauge.Invoke(target, new object[] { new Gauge { category = "HP", value = 100, maxValue = 100 } });
            gauge.Invoke(target, new object[] { new Gauge { category = "HP", value = 90, maxValue = 100 } });
            Assert.AreEqual(0, Voices(Clip("wood")), "HP-only damage has no contact event.");
            Hit(1);
            gauge.Invoke(target, new object[] { new Gauge { category = "HP", value = 80, maxValue = 100 } });
            Assert.AreEqual(1, Voices(Clip("wood")), "The accompanying HP update must not double the hit.");
            yield return new WaitForSecondsRealtime(0.21f);
            int before = Voices(Clip("wood"));
            Hit(1);
            Assert.AreEqual(before + 1, Voices(Clip("wood")), "The target becomes audible after cooldown.");
            Object.Destroy(container.gameObject);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator PreviewHitEventsAndDirectControllerCallsStaySilent()
        {
            yield return new EnterPlayMode();
            Component container = new GameObject("Preview hit targets").AddComponent(Named("GameScene.Object.ObjectContainer"));
            Component target = Target(container, 1, "TreeGolem");
            Component world = new GameObject("Preview hit world").AddComponent(Named("GameScene.Object.PresentationWorld"));
            target.GetType().GetProperty("PresentationWorld").SetValue(target, world);
            world.GetType().GetMethod("Register").Invoke(world, new object[] { target });
            Hit(1, world: world);
            Component controller = target.GetComponent(Named("GameScene.ServedObjectComponent.Sound.ServedObjectSfxController"));
            controller.GetType().GetMethod("PlayHit").Invoke(controller, null);
            Assert.AreEqual(0, Voices(Clip("wood")));
            Object.Destroy(world.gameObject);
            Object.Destroy(container.gameObject);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ChargedStormStagVisualEarlyReturnStillPlaysTargetHit()
        {
            yield return new EnterPlayMode();
            Component container = new GameObject("Charge hit targets").AddComponent(Named("GameScene.Object.ObjectContainer"));
            Target(container, 1, "RockGolem");
            Component actor = Target(container, 2, "StormStag");
            actor.GetType().GetMethod("UpdateActiveEffects", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(actor, new object[] { new List<string> { "StormStagCharge3" } });
            Hit(1, 2);
            Assert.AreEqual(1, Voices(Clip("rock")));
            Object.Destroy(container.gameObject);
            yield return new ExitPlayMode();
        }
    }
}
