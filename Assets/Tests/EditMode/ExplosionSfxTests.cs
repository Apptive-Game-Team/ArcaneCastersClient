using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using GameScene.Dto;
using GameScene.Dto.Projectile;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class ExplosionSfxTests
    {
        private static Type TypeNamed(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
        private static Object ImpactProfile => AssetDatabase.LoadAssetAtPath<Object>(
            "Assets/Resources/Sound/Config/Profiles/CosmeticImpact.asset");
        private static AudioClip LightClip => Resources.Load<AudioClip>("Sound/Game/light_explode");
        private static AudioClip ExplosionClip => Resources.Load<AudioClip>("Sound/Game/Shared/explosion_v1");

        private static int Voices(AudioClip clip)
        {
            Type playerType = TypeNamed("Sound.GameSfxPlayer");
            var player = (Object)playerType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            if (player == null) return 0;
            return ((IEnumerable)playerType.GetField("voices", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(player)).Cast<object>().Count(voice =>
                    (AudioClip)voice.GetType().GetField("Clip").GetValue(voice) == clip);
        }

        private static GameObject DestructionOwner(bool suppressed = false, Component world = null)
        {
            var target = new GameObject("Destruction impact test");
            Type type = TypeNamed("GameScene.ServedObjectComponent.OnDestroySpawner");
            Component spawner = target.AddComponent(type);
            type.GetField("prefab").SetValue(spawner, Resources.Load<GameObject>("Prefabs/Effects/Explode"));
            type.GetField("spawnSfxProfile", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(spawner, ImpactProfile);
            type.GetProperty("SuppressPresentation").SetValue(spawner, suppressed);
            if (world != null) world.GetType().GetMethod("Own").Invoke(world, new object[] { target });
            return target;
        }

        [TestCase("Abstract/AbstractDrop")]
        [TestCase("Abstract/AbstractShot")]
        [TestCase("Abstract/AbstractRune")]
        [TestCase("BombSprite")]
        [TestCase("WindSpirit")]
        [TestCase("Tutorial/FireShot")]
        [TestCase("FireShot")]
        [TestCase("WaterShot")]
        public void DestructionEffectsKeepOriginalLightExplosionProfile(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
            Type type = TypeNamed("GameScene.ServedObjectComponent.OnDestroySpawner");
            Component[] owners = prefab.GetComponentsInChildren(type, true);
            Assert.IsNotEmpty(owners);
            foreach (Component owner in owners)
                Assert.AreSame(ImpactProfile, type.GetProperty("SpawnSfxProfile").GetValue(owner));
            object slot = ImpactProfile.GetType().GetProperty("Spawn").GetValue(ImpactProfile);
            Assert.AreSame(LightClip, slot.GetType().GetProperty("Clip").GetValue(slot));
            Assert.AreEqual(1f, slot.GetType().GetProperty("Volume").GetValue(slot));
            Assert.AreEqual(1f, slot.GetType().GetProperty("Pitch").GetValue(slot));
            Assert.AreNotSame(LightClip, ExplosionClip);
        }

        [UnityTest]
        public IEnumerator DestructionActuallyDispatchesOriginalClipOnce()
        {
            yield return new EnterPlayMode();
            GameObject owner = DestructionOwner();
            yield return null;
            Assert.AreEqual(0, Voices(LightClip));
            Object.Destroy(owner);
            yield return null;
            Assert.AreEqual(1, Voices(LightClip));
            Assert.AreEqual(0, Voices(ExplosionClip));
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator SuppressedDestructionAndPreviewDestructionAndClearStaySilent()
        {
            yield return new EnterPlayMode();
            Object.Destroy(DestructionOwner(true));
            yield return null;
            Assert.AreEqual(0, Voices(LightClip));
            Component world = new GameObject("Preview impact test")
                .AddComponent(TypeNamed("GameScene.Object.PresentationWorld"));
            Object.Destroy(DestructionOwner(world: world));
            yield return null;
            Assert.AreEqual(0, Voices(LightClip));
            DestructionOwner(world: world);
            world.GetType().GetMethod("Clear").Invoke(world, null);
            yield return null;
            Assert.AreEqual(0, Voices(LightClip));
            Object.Destroy(world.gameObject);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator StandaloneProjectileExplosionsDispatchOnceAndPreviewsStaySilent()
        {
            yield return new EnterPlayMode();
            Component world = new GameObject("Preview projectile test")
                .AddComponent(TypeNamed("GameScene.Object.PresentationWorld"));
            MethodInfo spawn = TypeNamed("GameScene.Object.ProjectileSpawner").GetMethod("SpawnShared");
            int expected = 0;
            foreach (string type in new[] { "BoulderStrikeImpact", "ShockOverloadSecondary" })
            {
                var dto = new ProjectileDto
                {
                    type = type, duration = 0.5f,
                    start = new PositionProjectileTarget(), end = new PositionProjectileTarget()
                };
                spawn.Invoke(null, new object[] { dto, null });
                Assert.AreEqual(++expected, Voices(ExplosionClip), type);
                spawn.Invoke(null, new object[] { dto, world });
                Assert.AreEqual(expected, Voices(ExplosionClip), "Preview: " + type);
            }
            Assert.AreEqual(0, Voices(LightClip));
            Object.Destroy(world.gameObject);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator NormalServerExplosionStillDispatchesExactlyOnce()
        {
            yield return new EnterPlayMode();
            var host = new GameObject("Object container test");
            host.AddComponent(TypeNamed("GameScene.Object.ObjectContainer"));
            var dto = new CreatedObjectDto
            {
                id = 4242, type = "FireExplode", master = "LeftPlayer"
            };
            Component explosion = (Component)TypeNamed("GameScene.Object.ObjectSpawner").GetMethod("SpawnShared")
                .Invoke(null, new object[] { dto, null, true });
            Assert.AreEqual(1, Voices(ExplosionClip));
            yield return null;
            Assert.AreEqual(1, Voices(ExplosionClip));
            Assert.AreEqual(0, Voices(LightClip));
            Object.Destroy(explosion.gameObject);
            Object.Destroy(host);
            yield return new ExitPlayMode();
        }
    }
}
