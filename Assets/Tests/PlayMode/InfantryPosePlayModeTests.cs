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
            Assert.AreEqual(8, movement.Length);
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
                float started = Time.time - 1.5f / 12f;
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
                Assert.AreSame(movement[1], renderer.sprite);
                Set(presenter, "movementUntil", Time.time - 0.5f / 12f);
                refresh();
                Assert.AreSame(stop, renderer.sprite);
                Set(presenter, "movementUntil", Time.time - 2f / 12f);
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

        private static object Get(object target, string field) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
