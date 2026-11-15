using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class InfantryPosePlayModeTests
    {
        [UnityTest]
        public IEnumerator InfantryWalkAndAttackHaveOneSpriteOwnerAndRestoreTheCurrentPose()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/ArcaneInfantry");
            MonoBehaviour[] components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(c => c != null &&
                (c.GetType().Name == "HoppingMotionController" || c.GetType().Name == "AttackSpriteSwapController")));
            MonoBehaviour authored = components.Single(c => c != null && c.GetType().Name == "UnitSpriteMotionController");
            Sprite idle = (Sprite)Get(authored, "idleSprite");
            Sprite[] movement = (Sprite[])Get(authored, "moveFrames");
            Sprite[] attacks = (Sprite[])Get(authored, "attackFrames");
            Sprite stop = (Sprite)Get(authored, "moveStopFrame");
            Assert.NotNull(idle);
            Assert.NotNull(stop);
            Assert.AreEqual(6, movement.Length);
            Assert.AreEqual(6, attacks.Length);
            Assert.IsTrue(movement.All(frame => frame != null));
            Assert.IsTrue(attacks.All(frame => frame != null));
            Type ownerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("GameScene.ServedObjectComponent.ServedObject")).First(t => t != null);
            GameObject instance = new GameObject("infantry-pose-test");
            try
            {
                Component owner = instance.AddComponent(ownerType);
                SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
                Component presenter = instance.AddComponent(authored.GetType());
                Set(presenter, "targetRenderer", renderer);
                Set(presenter, "idleSprite", idle);
                Set(presenter, "moveFrames", movement);
                float moveInterval = (float)Get(authored, "moveFrameInterval");
                Set(presenter, "moveFrameInterval", moveInterval);
                Assert.That(movement.Length * moveInterval, Is.EqualTo(8f / 12f).Within(0.001f));
                Set(presenter, "attackFrames", attacks);
                Set(presenter, "moveStopFrame", stop);
                presenter.GetType().GetMethod("Bind").Invoke(presenter, new object[] { owner });
                Action<string> raise = name => ((Action)ownerType.GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner))?.Invoke();
                Action refresh = () => presenter.GetType().GetMethod("ApplyPose",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(presenter, new object[] { Time.time });

                yield return null;
                Assert.AreSame(idle, renderer.sprite);
                renderer.flipX = true;
                raise("OnMoved");
                Assert.AreSame(movement[0], renderer.sprite);
                // 연속 이동 이벤트가 이전 보행 위상을 덮어쓰지 않는지 확인한다.
                float started = Time.time - 1.5f * moveInterval;
                Set(presenter, "movementStarted", started);
                for (int i = 0; i < 5; i++) raise("OnMoved");
                Assert.AreEqual(started, Get(presenter, "movementStarted"));
                Assert.AreSame(movement[1], renderer.sprite);
                raise("OnAttack");
                raise("OnMoved");
                Assert.AreSame(attacks[0], renderer.sprite);
                Set(presenter, "attackStarted", Time.time - 2.5f / 12f);
                refresh();
                Assert.AreSame(attacks[2], renderer.sprite);
                Set(presenter, "attackStarted", float.NegativeInfinity);
                refresh();
                Assert.AreSame(movement[0], renderer.sprite);
                Set(presenter, "movementUntil", Time.time - 0.5f * moveInterval);
                refresh();
                Assert.AreSame(stop, renderer.sprite);
                Set(presenter, "movementUntil", Time.time - 2f * moveInterval);
                refresh();
                Assert.AreSame(idle, renderer.sprite);
                raise("OnAttack");
                Set(presenter, "movementUntil", float.NegativeInfinity);
                Set(presenter, "attackStarted", float.NegativeInfinity);
                refresh();
                yield return null;
                Assert.AreSame(idle, renderer.sprite);
                raise("OnAttack");
                instance.SetActive(false);
                yield return null;
                Assert.AreSame(idle, renderer.sprite);
                raise("OnMoved");
                raise("OnAttack");
                instance.SetActive(true);
                refresh();
                yield return null;
                Assert.AreSame(idle, renderer.sprite);
                Assert.IsTrue(renderer.flipX);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [UnityTest]
        public IEnumerator DamageDoesNotReplaceMovementOrAttackOrReplayAfterwards()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/ArcaneInfantry");
            var authored = prefab.GetComponentsInChildren<MonoBehaviour>(true)
                .Single(c => c != null && c.GetType().Name == "UnitSpriteMotionController");
            var ownerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("GameScene.ServedObjectComponent.ServedObject")).First(t => t != null);
            var instance = new GameObject("infantry-hit-priority-test");
            try
            {
                var owner = instance.AddComponent(ownerType);
                var renderer = instance.AddComponent<SpriteRenderer>();
                var presenter = instance.AddComponent(authored.GetType());
                var idle = (Sprite)Get(authored, "idleSprite");
                var move = (Sprite[])Get(authored, "moveFrames");
                var attack = (Sprite[])Get(authored, "attackFrames");
                var hurt = (Sprite[])Get(authored, "hurtFrames");
                Set(presenter, "targetRenderer", renderer);
                Set(presenter, "idleSprite", idle);
                Set(presenter, "moveFrames", move);
                Set(presenter, "attackFrames", attack);
                Set(presenter, "hurtFrames", hurt);
                presenter.GetType().GetMethod("Bind").Invoke(presenter, new object[] { owner });
                void Raise(string name) => ((Action)ownerType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(owner))?.Invoke();
                void Refresh() => presenter.GetType().GetMethod("ApplyPose", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(presenter, new object[] { Time.time });

                Raise("OnHpDecreased");
                Assert.AreSame(hurt[0], renderer.sprite);
                // 체력 갱신이 공격 상태보다 먼저 들어오는 같은 패킷을 재현한다.
                Raise("OnAttack");
                Assert.AreSame(attack[0], renderer.sprite);
                Set(presenter, "attackStarted", float.NegativeInfinity);
                Refresh();
                Assert.AreSame(idle, renderer.sprite);

                Set(presenter, "hurtStarted", float.NegativeInfinity);
                Raise("OnMoved");
                Raise("OnHpDecreased");
                Assert.AreSame(move[0], renderer.sprite);
                Set(presenter, "movementUntil", float.NegativeInfinity);
                Refresh();
                Assert.AreSame(idle, renderer.sprite);
                yield return null;
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [TestCase("ArcaneInfantry")]
        [TestCase("RockGolem")]
        [TestCase("WallGolem")]
        [TestCase("Towerback")]
        public void WalkUsesAuthoredCadenceWithUncompressedColors(string unit)
        {
            var prefab = Resources.Load<GameObject>($"Prefabs/{unit}");
            var motion = prefab.GetComponentsInChildren<MonoBehaviour>(true)
                .Single(component => component != null && component.GetType().Name == "UnitSpriteMotionController");
            var frames = (Sprite[])Get(motion, "moveFrames");
            var interval = (float)Get(motion, "moveFrameInterval");
            Assert.IsTrue(frames.All(frame => frame != null));
            if (unit == "ArcaneInfantry")
            {
                Assert.AreEqual(6, frames.Length);
                Assert.That(frames.Length * interval, Is.EqualTo(8f / 12f).Within(0.001f));
                for (int i = 0; i < frames.Length; i++)
                    Assert.AreNotSame(frames[i], frames[(i + 1) % frames.Length]);
            }
            else if (unit == "RockGolem")
            {
                CollectionAssert.AreEqual(new[]
                {
                    ("RockGolemStepNear", 2), ("RockGolemLoadNear", 5),
                    ("RockGolemSupportNear", 1), ("RockGolemPassNear", 2),
                    ("RockGolemStepFar", 2), ("RockGolemLoadFar", 5),
                    ("RockGolemPassFar", 2), ("RockGolemSupportFar", 1)
                }.SelectMany(pose => Enumerable.Repeat(pose.Item1, pose.Item2)),
                    frames.Select(frame => frame.name).ToArray());
                Assert.That(frames.Length * interval, Is.EqualTo(20f / 24f).Within(0.001f));
            }
            else
            {
                Assert.AreEqual(10, frames.Length);
                Assert.That(frames.Length * interval, Is.EqualTo(10f / 12f).Within(0.001f));
                Assert.AreNotSame(frames[0], frames[5]);
                for (int i = 0; i < frames.Length; i += 5)
                {
                    Assert.AreSame(frames[i], frames[i + 1]);
                    Assert.AreNotSame(frames[i + 1], frames[i + 2]);
                    Assert.AreSame(frames[i + 2], frames[i + 3]);
                    Assert.AreNotSame(frames[i + 3], frames[i + 4]);
                }
            }
            foreach (var frame in frames.Append((Sprite)Get(motion, "idleSprite")).Distinct())
                CollectionAssert.Contains(new[] { TextureFormat.RGBA32, TextureFormat.ARGB32, TextureFormat.RGB24 }, frame.texture.format, frame.name);
        }

        private static object Get(object target, string field) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
