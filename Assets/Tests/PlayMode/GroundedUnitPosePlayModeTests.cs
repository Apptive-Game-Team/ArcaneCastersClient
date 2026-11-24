using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class GroundedUnitPosePlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int RenderRate = 120;
        private const float WalkSpeed = 0.5f;

        [UnityTest]
        public IEnumerator TreeGolemWalksAtBothTickRatesAndShowsTheHitBeforeRecovery() => CheckBothTickRates("TreeGolem", 8);

        [UnityTest]
        public IEnumerator RockGolemWalksAtBothTickRatesAndShowsTheHitBeforeRecovery() => CheckBothTickRates("RockGolem", 9);

        [UnityTest]
        public IEnumerator BeanShooterWalksAtBothTickRatesAndFiresWithTheBean() =>
            CheckBothTickRates("BeanShooter", 8, "BeanShooterAttack", 3, 0.6f, 0.08f, "Bean");

        [UnityTest]
        public IEnumerator EvilEntWalksWithBothRootsAndBracesAtBothTickRates() =>
            CheckBothTickRates("EvilEnt", 8, "EvilEnt2", 3, 2f, 0.08f);

        [UnityTest]
        public IEnumerator PveEvilEntBracesAndReturnsToItsSharedIdleWithoutMoving()
        {
            var root = new GameObject("PveEvilEnt test");
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = RenderRate;
            try
            {
                var world = root.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
                Type createdType = Runtime("GameScene.Dto.CreatedObjectDto");
                object created = Activator.CreateInstance(createdType);
                createdType.GetField("id").SetValue(created, 42);
                createdType.GetField("type").SetValue(created, "PveEvilEnt");
                createdType.GetField("master").SetValue(created, "RightPlayer");
                createdType.GetField("position").SetValue(created, new Vector3(14f, 0f, 5f));
                var boss = (Component)Runtime("GameScene.Object.ObjectSpawner").GetMethod("SpawnShared")
                    .Invoke(null, new[] { created, world, false });
                var pose = boss.GetComponents<MonoBehaviour>()
                    .Single(component => component.GetType().Name == "GroundedUnitPoseController");
                var renderer = (SpriteRenderer)pose.GetType().GetField("targetRenderer", Private).GetValue(pose);
                Vector3 stationary = boss.transform.position;
                Quaternion upright = renderer.transform.localRotation;
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEnt"));
                boss.GetType().GetMethod("PlayAttackPresentation").Invoke(boss, null);
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEnt2"));
                yield return new WaitForSeconds(.10f);
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEntBrace02"));
                yield return new WaitForSeconds(.08f);
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEntBrace03"));
                yield return new WaitForSeconds(.10f);
                Assert.That(renderer.sprite.name, Is.EqualTo("EvilEnt"));
                Assert.That(renderer.transform.localRotation, Is.EqualTo(upright));
                Assert.That(boss.transform.position, Is.EqualTo(stationary));
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator CorruptedVineCrawlsOnItsRootAndLashesAtBothTickRates()
        {
            const string prefabName = "PveCorruptedVineSpirit";
            GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
            Assert.That(prefab.GetComponents<MonoBehaviour>()
                .Any(component => component.GetType().Name == "HoppingMotionController"), Is.False);
            Type servedObject = Runtime("GameScene.ServedObjectComponent.ServedObject");
            Assert.That((bool)servedObject.GetField("_swingOnAttack", Private).GetValue(prefab.GetComponent(servedObject)), Is.False);
            yield return CheckBothTickRates(prefabName, 8, prefabName + "Lash01", secondsPerAttackPose: 0.06f);
        }

        private static IEnumerator CheckBothTickRates(string prefabName, int walkPoseCount,
            string attackSprite = null, int attackPoseCount = 4, float walkSpeed = WalkSpeed,
            float secondsPerAttackPose = 0.05f, string shotPrefab = null)
        {
            var gameConfig = Runtime("GameScene.GameConfig");
            var setTickRate = gameConfig.GetMethod("SetTickRate");
            int previousTickRate = (int)gameConfig.GetProperty("TickRate").GetValue(null);
            int previousRenderRate = Time.captureFramerate;
            try
            {
                Time.captureFramerate = RenderRate;
                foreach (int tickRate in new[] { 20, 60 })
                {
                    setTickRate.Invoke(null, new object[] { tickRate });
                    yield return CheckAtTickRate(prefabName, walkPoseCount, tickRate,
                        attackSprite ?? prefabName + "2", attackPoseCount, walkSpeed, secondsPerAttackPose, shotPrefab);
                }
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                setTickRate.Invoke(null, new object[] { previousTickRate });
            }
        }

        private static IEnumerator CheckAtTickRate(string prefabName, int walkPoseCount, int tickRate,
            string attackSprite, int attackPoseCount, float walkSpeed, float secondsPerAttackPose, string shotPrefab)
        {
            int direction = tickRate == 60 ? -1 : 1;
            var root = new GameObject($"{prefabName}-{tickRate}Hz");
            try
            {
                // 재생용 월드가 축소되어도 서버 좌표 거리로 보행 위상을 계산한다.
                root.transform.localScale = Vector3.one * 0.4f;
                var world = root.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
                Type createdType = Runtime("GameScene.Dto.CreatedObjectDto");
                object created = Activator.CreateInstance(createdType);
                createdType.GetField("id").SetValue(created, 42);
                createdType.GetField("type").SetValue(created, prefabName);
                createdType.GetField("master").SetValue(created, "LeftPlayer");
                var spawner = Runtime("GameScene.Object.ObjectSpawner");
                var unit = (Component)spawner.GetMethod("SpawnShared").Invoke(null, new[] { created, world, false });
                var presenter = unit.GetComponents<MonoBehaviour>()
                    .Single(component => component.GetType().Name == "GroundedUnitPoseController");
                Assert.That(unit.GetComponents<MonoBehaviour>()
                    .Any(component => component.GetType().Name == "OnAttackSpriteSwapper"), Is.False);
                var inheritedSwap = unit.GetComponents<MonoBehaviour>()
                    .SingleOrDefault(component => component.GetType().Name == "AttackSpriteSwapController");
                if (inheritedSwap != null)
                {
                    Assert.That(inheritedSwap.GetType().BaseType.GetField("swapSprite", Private).GetValue(inheritedSwap), Is.Null);
                }
                var renderer = (SpriteRenderer)presenter.GetType().GetField("targetRenderer", Private).GetValue(presenter);
                var walk = (Sprite[])presenter.GetType().GetField("walkFrames", Private).GetValue(presenter);
                var attack = (Sprite[])presenter.GetType().GetField("attackFrames", Private).GetValue(presenter);
                Sprite idle = renderer.sprite;
                Sprite hit = Resources.Load<Sprite>($"Game/sprites/{attackSprite}");
                Assert.That(idle.name, Is.EqualTo(prefabName));
                Assert.That(attack[0], Is.SameAs(hit));
                AssertFrames(walk, walkPoseCount, idle);
                AssertFrames(attack, attackPoseCount, hit);
                float attackDuration = secondsPerAttackPose * attackPoseCount;
                Assert.That((float)presenter.GetType().GetField("attackDuration", Private).GetValue(presenter),
                    Is.EqualTo(attackDuration).Within(0.0001f));

                Type updatedType = Runtime("GameScene.Dto.UpdatedObjectDto");
                object frame = Activator.CreateInstance(updatedType);
                FieldInfo position = updatedType.GetField("position");
                FieldInfo status = updatedType.GetField("status");
                MethodInfo update = unit.GetType().GetMethod("UpdateObject");
                var target = Vector3.zero;
                var seenWalk = new HashSet<Sprite>();
                yield return null;
                // 기존 방향 전환 대기 0.5초 뒤에도 보행 주기가 온전히 지나야 한다.
                for (int tick = 0; tick < tickRate * 3; tick++)
                {
                    target.x += direction * walkSpeed / tickRate;
                    position.SetValue(frame, target);
                    update.Invoke(unit, new[] { frame });
                    for (int render = 0; render < RenderRate / tickRate; render++)
                    {
                        yield return null;
                        seenWalk.Add(renderer.sprite);
                    }
                }

                CollectionAssert.IsSubsetOf(walk.Distinct().ToArray(), seenWalk,
                    $"{tickRate}Hz 보행이 모든 고유 자세를 지나야 한다.");
                Assert.That(renderer.flipX, Is.EqualTo(direction < 0));
                status.SetValue(frame, "Attack");
                if (shotPrefab == null)
                {
                    update.Invoke(unit, new[] { frame });
                }
                else
                {
                    ApplyShotFrame(shotPrefab, target, world, frame);
                    Assert.That((Component)world.GetType().GetMethod("FindById").Invoke(world, new object[] { 43 }), Is.Not.Null);
                }
                Assert.That(renderer.sprite, Is.SameAs(attack[0]));
                int nearFirstPoseEnd = Mathf.FloorToInt(secondsPerAttackPose * RenderRate) - 1;
                for (int render = 0; render < nearFirstPoseEnd; render++)
                {
                    yield return null;
                    Assert.That(renderer.sprite, Is.SameAs(attack[0]));
                }
                int secondPoseSample = Mathf.CeilToInt(secondsPerAttackPose * RenderRate) + 2;
                for (int render = nearFirstPoseEnd; render < secondPoseSample; render++)
                {
                    yield return null;
                }
                Assert.That(renderer.sprite, Is.SameAs(attack[1]));
                update.Invoke(unit, new[] { frame });
                Assert.That(renderer.sprite, Is.SameAs(attack[0]));

                var seenAttack = new List<Sprite>();
                status.SetValue(frame, null);
                int framesThroughRecovery = Mathf.CeilToInt((attackDuration + 0.04f) * RenderRate);
                for (int render = 0; render < framesThroughRecovery; render++)
                {
                    if (render % (RenderRate / tickRate) == 0)
                    {
                        target.x += direction * walkSpeed / tickRate;
                        position.SetValue(frame, target);
                        update.Invoke(unit, new[] { frame });
                    }
                    if (attack.Contains(renderer.sprite) && !seenAttack.Contains(renderer.sprite))
                    {
                        seenAttack.Add(renderer.sprite);
                    }
                    yield return null;
                }
                CollectionAssert.AreEqual(attack, seenAttack);
                Assert.That(walk.Contains(renderer.sprite), Is.True);

                yield return new WaitForSeconds(0.14f);
                Assert.That(renderer.sprite, Is.SameAs(idle));
                unit.transform.position += 3f * Vector3.right + Vector3.up;
                yield return null;
                yield return null;
                Assert.That(renderer.sprite, Is.SameAs(idle));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        private static void ApplyShotFrame(string shotPrefab, Vector3 position, Component world, object update)
        {
            Type createdType = Runtime("GameScene.Dto.CreatedObjectDto");
            object shot = Activator.CreateInstance(createdType);
            createdType.GetField("id").SetValue(shot, 43);
            createdType.GetField("type").SetValue(shot, shotPrefab);
            createdType.GetField("master").SetValue(shot, "LeftPlayer");
            createdType.GetField("position").SetValue(shot, position);
            update.GetType().GetField("id").SetValue(update, 42);
            update.GetType().GetField("master").SetValue(update, "LeftPlayer");

            Type objectsType = Runtime("GameScene.Dto.ObjectsInfo");
            object objects = Activator.CreateInstance(objectsType);
            Array created = Array.CreateInstance(createdType, 1);
            created.SetValue(shot, 0);
            Array updated = Array.CreateInstance(update.GetType(), 1);
            updated.SetValue(update, 0);
            objectsType.GetField("create").SetValue(objects, created);
            objectsType.GetField("update").SetValue(objects, updated);
            Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply")
                .Invoke(null, new[] { objects, null, world });
        }

        private static void AssertFrames(Sprite[] frames, int count, Sprite reference)
        {
            Assert.That(frames.Distinct().Count(), Is.EqualTo(count));
            foreach (Sprite frame in frames)
            {
                Assert.That(frame, Is.Not.Null);
                Assert.That(frame.rect.size, Is.EqualTo(reference.rect.size));
                Assert.That(frame.pivot, Is.EqualTo(reference.pivot));
                Assert.That(frame.pixelsPerUnit, Is.EqualTo(reference.pixelsPerUnit));
            }
        }

        private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
    }
}
