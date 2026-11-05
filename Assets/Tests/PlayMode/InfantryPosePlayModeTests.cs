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
            MonoBehaviour authored = components.Single(c => c != null && c.GetType().Name == "InfantryPoseController");
            Type ownerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("GameScene.ServedObjectComponent.ServedObject")).First(t => t != null);
            GameObject instance = new GameObject("infantry-pose-test");
            try
            {
                Component owner = instance.AddComponent(ownerType);
                SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
                Component presenter = instance.AddComponent(authored.GetType());
                Set(presenter, "targetRenderer", renderer);
                foreach (string name in new[] { "idleSprite", "walkLeftSprite", "walkRightSprite", "attackSprite" })
                {
                    Assert.NotNull(Get(authored, name), name);
                    Set(presenter, name, Get(authored, name));
                }
                presenter.GetType().GetMethod("Bind").Invoke(presenter, new object[] { owner });
                Action<string> raise = name => ((Action)ownerType.GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner))?.Invoke();
                Action refresh = () => presenter.GetType().GetMethod("ApplyPose",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(presenter, new object[] { Time.time });

                yield return null;
                Assert.AreSame(Get(authored, "idleSprite"), renderer.sprite);
                renderer.flipX = true;
                raise("OnMoved");
                Assert.AreSame(Get(authored, "walkLeftSprite"), renderer.sprite);
                bool sawSecondStep = false;
                for (int i = 0; i < 5; i++)
                {
                    yield return new WaitForSeconds(0.05f);
                    raise("OnMoved");
                    sawSecondStep |= renderer.sprite == (Sprite)Get(authored, "walkRightSprite");
                }
                Assert.IsTrue(sawSecondStep, "연속 이동 중 두 번째 발걸음이 표시되어야 한다.");
                // 연속 이동 이벤트가 이전 보행 위상을 덮어쓰지 않는지 확인한다.
                float started = Time.time - 0.17f;
                Set(presenter, "movementStarted", started);
                for (int i = 0; i < 5; i++) raise("OnMoved");
                Assert.AreEqual(started, Get(presenter, "movementStarted"));
                Assert.AreSame(Get(authored, "walkRightSprite"), renderer.sprite);
                raise("OnAttack");
                raise("OnMoved");
                Assert.AreSame(Get(authored, "attackSprite"), renderer.sprite);
                Set(presenter, "attackUntil", float.NegativeInfinity);
                refresh();
                Assert.AreSame(Get(authored, "walkRightSprite"), renderer.sprite);
                raise("OnAttack");
                Set(presenter, "movementUntil", float.NegativeInfinity);
                Set(presenter, "attackUntil", float.NegativeInfinity);
                refresh();
                yield return null;
                Assert.AreSame(Get(authored, "idleSprite"), renderer.sprite);
                raise("OnAttack");
                instance.SetActive(false);
                yield return null;
                Assert.AreSame(Get(authored, "idleSprite"), renderer.sprite);
                raise("OnMoved");
                raise("OnAttack");
                instance.SetActive(true);
                refresh();
                yield return null;
                Assert.AreSame(Get(authored, "idleSprite"), renderer.sprite);
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
