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
            var root = new GameObject("Infantry pose lifecycle test");
            Type config = Runtime("GameScene.GameConfig");
            MethodInfo setTickRate = config.GetMethod("SetTickRate");
            int previousTickRate = (int)config.GetProperty("TickRate").GetValue(null);
            int previousRenderRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            setTickRate.Invoke(null, new object[] { 20 });
            try
            {
                Component world = root.AddComponent(Runtime("GameScene.Object.PresentationWorld"));
                Component troop = Spawn(world, 37, "ArcaneInfantry", "LeftPlayer", 4f);
                Spawn(world, 38, "ElectricSlime", "RightPlayer", 2f);
                MonoBehaviour[] components = troop.GetComponentsInChildren<MonoBehaviour>(true);
                Assert.IsFalse(components.Any(c => c != null &&
                    (c.GetType().Name is "InfantryPoseController" or "HoppingMotionController" or "AttackSpriteSwapController")));
                MonoBehaviour presenter = components.Single(c => c.GetType().Name == "GroundedUnitPoseController");
                var renderer = (SpriteRenderer)Get(presenter, "targetRenderer");
                var attack = (Sprite[])Get(presenter, "attackFrames");
                Sprite idle = renderer.sprite;
                Assert.AreEqual("ArcaneInfantry", idle.name);
                MethodInfo update = troop.GetType().GetMethod("UpdateObject");
                var updateType = Runtime("GameScene.Dto.UpdatedObjectDto");
                object updateDto = Activator.CreateInstance(updateType);
                FieldInfo position = updateType.GetField("position");
                FieldInfo status = updateType.GetField("status");
                for (int i = 0; i < 16; i++)
                {
                    position.SetValue(updateDto, new Vector3(4f + i * .055f, 0f, 5f));
                    update.Invoke(troop, new[] { updateDto });
                    for (int render = 0; render < 6; render++) yield return null;
                }
                Assert.IsFalse(renderer.flipX);
                Assert.AreEqual(1, Get(presenter, "confirmedServerDirection"));

                Type hitType = Runtime("GameScene.Dto.Event.HitEvent");
                object hit = Activator.CreateInstance(hitType);
                hitType.GetField("actorId").SetValue(hit, 37);
                hitType.GetField("targetId").SetValue(hit, 38);
                Type eventType = typeof(System.Collections.Generic.List<>)
                    .MakeGenericType(Runtime("GameScene.Dto.Event.GameEvent"));
                var events = (IList)Activator.CreateInstance(eventType);
                events.Add(hit);
                MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
                apply.Invoke(null, new object[] { null, events, world });
                Assert.IsFalse(attack.Contains(renderer.sprite));

                int notices = 0;
                troop.GetType().GetEvent("OnAttack").AddEventHandler(troop, new Action(() => notices++));
                Type objects = Runtime("GameScene.Dto.ObjectsInfo");
                object envelope = Activator.CreateInstance(objects);
                Array updates = Array.CreateInstance(updateType, 1);
                updateType.GetField("id").SetValue(updateDto, 37);
                status.SetValue(updateDto, "Attack");
                updates.SetValue(updateDto, 0);
                objects.GetField("update").SetValue(envelope, updates);
                apply.Invoke(null, new[] { envelope, events, world });
                Assert.AreEqual(1, notices);
                Assert.AreSame(attack[0], renderer.sprite);
                var seen = new System.Collections.Generic.List<Sprite>();
                for (int frame = 0; frame < 42; frame++)
                {
                    Assert.IsTrue(renderer.flipX);
                    if (attack.Contains(renderer.sprite) && renderer.sprite != seen.LastOrDefault())
                        seen.Add(renderer.sprite);
                    yield return null;
                }
                CollectionAssert.AreEqual(attack, seen);
                Assert.AreSame(idle, renderer.sprite);

                apply.Invoke(null, new[] { envelope, events, world });
                Assert.AreSame(attack[0], renderer.sprite);
                troop.gameObject.SetActive(false);
                Assert.AreSame(idle, renderer.sprite);
                update.Invoke(troop, new[] { updateDto });
                troop.gameObject.SetActive(true);
                yield return null;
                Assert.AreSame(idle, renderer.sprite);
                Assert.IsTrue(renderer.flipX);
            }
            finally
            {
                Time.captureFramerate = previousRenderRate;
                setTickRate.Invoke(null, new object[] { previousTickRate });
                Object.Destroy(root);
            }
            yield return null;
        }

        private static Component Spawn(Component world, int id, string name, string master, float x)
        {
            Type data = Runtime("GameScene.Dto.CreatedObjectDto");
            object dto = Activator.CreateInstance(data);
            data.GetField("id").SetValue(dto, id);
            data.GetField("type").SetValue(dto, name);
            data.GetField("master").SetValue(dto, master);
            data.GetField("position").SetValue(dto, new Vector3(x, 0f, 5f));
            return (Component)Runtime("GameScene.Object.ObjectSpawner").GetMethod("SpawnShared")
                .Invoke(null, new[] { dto, world, false });
        }

        private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);

        private static object Get(object target, string field) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
