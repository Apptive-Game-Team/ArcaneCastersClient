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

        private static IEnumerator CheckBothTickRates(string prefabName, int walkPoseCount)
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
                    yield return CheckAtTickRate(prefabName, walkPoseCount, tickRate, tickRate == 60 ? -1 : 1);
                }
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                setTickRate.Invoke(null, new object[] { previousTickRate });
            }
        }

        private static IEnumerator CheckAtTickRate(string prefabName, int walkPoseCount, int tickRate, int direction)
        {
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
                var golem = (Component)spawner.GetMethod("SpawnShared").Invoke(null, new[] { created, world, false });
                var presenter = golem.GetComponents<MonoBehaviour>()
                    .Single(component => component.GetType().Name == "GroundedUnitPoseController");
                Assert.That(golem.GetComponents<MonoBehaviour>()
                    .Any(component => component.GetType().Name == "OnAttackSpriteSwapper"), Is.False);
                var inheritedSwap = golem.GetComponents<MonoBehaviour>()
                    .SingleOrDefault(component => component.GetType().Name == "AttackSpriteSwapController");
                if (inheritedSwap != null)
                {
                    Assert.That(inheritedSwap.GetType().BaseType.GetField("swapSprite", Private).GetValue(inheritedSwap), Is.Null);
                }
                var renderer = (SpriteRenderer)presenter.GetType().GetField("targetRenderer", Private).GetValue(presenter);
                var walk = (Sprite[])presenter.GetType().GetField("walkFrames", Private).GetValue(presenter);
                var attack = (Sprite[])presenter.GetType().GetField("attackFrames", Private).GetValue(presenter);
                Sprite idle = renderer.sprite;
                Sprite hit = Resources.Load<Sprite>($"Game/sprites/{prefabName}2");
                Assert.That(idle.name, Is.EqualTo(prefabName));
                Assert.That(attack[0], Is.SameAs(hit));
                AssertFrames(walk, walkPoseCount, idle);
                AssertFrames(attack, 4, hit);

                Type updatedType = Runtime("GameScene.Dto.UpdatedObjectDto");
                object frame = Activator.CreateInstance(updatedType);
                FieldInfo position = updatedType.GetField("position");
                FieldInfo status = updatedType.GetField("status");
                MethodInfo update = golem.GetType().GetMethod("UpdateObject");
                var target = Vector3.zero;
                var seenWalk = new HashSet<Sprite>();
                yield return null;
                // 기존 방향 전환 대기 0.5초 뒤에도 보행 주기가 온전히 지나야 한다.
                for (int tick = 0; tick < tickRate * 3; tick++)
                {
                    target.x += direction * WalkSpeed / tickRate;
                    position.SetValue(frame, target);
                    update.Invoke(golem, new[] { frame });
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
                update.Invoke(golem, new[] { frame });
                Assert.That(renderer.sprite, Is.SameAs(attack[0]));
                for (int render = 0; render < 8; render++)
                {
                    yield return null;
                }
                Assert.That(renderer.sprite, Is.SameAs(attack[1]));
                update.Invoke(golem, new[] { frame });
                Assert.That(renderer.sprite, Is.SameAs(attack[0]));

                var seenAttack = new HashSet<Sprite>();
                status.SetValue(frame, null);
                for (int render = 0; render < RenderRate / 4; render++)
                {
                    if (render % (RenderRate / tickRate) == 0)
                    {
                        target.x += direction * WalkSpeed / tickRate;
                        position.SetValue(frame, target);
                        update.Invoke(golem, new[] { frame });
                    }
                    seenAttack.Add(renderer.sprite);
                    yield return null;
                }
                Assert.That(attack.All(seenAttack.Contains), Is.True);
                Assert.That(walk.Contains(renderer.sprite), Is.True);

                yield return new WaitForSeconds(0.14f);
                Assert.That(renderer.sprite, Is.SameAs(idle));
                golem.transform.position += 3f * Vector3.right + Vector3.up;
                yield return null;
                yield return null;
                Assert.That(renderer.sprite, Is.SameAs(idle));
            }
            finally
            {
                Object.Destroy(root);
            }
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
