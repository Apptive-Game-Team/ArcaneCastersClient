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
    public class CorruptedWindFlightPlayModeTests
    {
        private const string CorruptedWind = "PveCorruptedWindSpirit";
        private const string WindSpirit = "WindSpirit";
        private const int RenderRate = 120;

        [UnityTest]
        public IEnumerator ServerFramesSelectAllFlightPosesAt20Hz() => CheckFlight(CorruptedWind, 20, 1);

        [UnityTest]
        public IEnumerator ServerFramesSelectAllFlightPosesAt60Hz() => CheckFlight(CorruptedWind, 60, -1);

        [UnityTest]
        public IEnumerator WindSpiritUsesShallowDescentAndKeepsItsLastPoseAt20Hz() => CheckFlight(WindSpirit, 20, 1);

        [UnityTest]
        public IEnumerator WindSpiritKeepsItsLastPoseWhenTheFinalPacketsShareARenderFrame() => CheckFlight(WindSpirit, 60, -1);

        [UnityTest]
        public IEnumerator WindSpiritKeepsItsGlideDuringASameHeightRemoval() => CheckFlight(WindSpirit, 60, 1, true);

        private static IEnumerator CheckFlight(string prefabName, int tickRate, int direction, bool levelRemoval = false)
        {
            Type config = Runtime("GameScene.GameConfig");
            MethodInfo setRate = config.GetMethod("SetTickRate");
            int previousTickRate = (int)config.GetProperty("TickRate").GetValue(null);
            int previousRenderRate = Time.captureFramerate;
            var root = new GameObject(prefabName + " flight test");
            root.transform.position = new Vector3(100f, 100f, 100f);
            root.transform.rotation = Quaternion.Euler(0f, 15f, 0f);
            root.transform.localScale = Vector3.one * 0.4f;
            Component world = root.AddComponent(Runtime("GameScene.Object.PresentationWorld"));

            try
            {
                Time.captureFramerate = RenderRate;
                setRate.Invoke(null, new object[] { tickRate });
                Type infoType = Runtime("GameScene.Dto.ObjectsInfo");
                MethodInfo decode = Runtime("Global.Serialization.JsonCodec").GetMethod("Deserialize").MakeGenericMethod(infoType);
                MethodInfo apply = Runtime("GameScene.Object.PresentationFramePlayer").GetMethod("Apply");
                string master = direction > 0 ? "LeftPlayer" : "RightPlayer";
                object creation = decode.Invoke(null, new object[]
                {
                    "{\"create\":[{\"id\":42,\"type\":\"" + prefabName + "\",\"master\":\"" + master +
                    "\",\"position\":{\"x\":4,\"y\":3,\"z\":5}}]}"
                });
                apply.Invoke(null, new[] { creation, null, world });
                MethodInfo find = world.GetType().GetMethod("FindById");
                var unit = (Component)find.Invoke(world, new object[] { 42 });
                Type presenterType = Runtime("GameScene.ServedObjectComponent.Motion.WindSpiritFlightPresenter");
                var presenter = unit.GetComponent(presenterType);
                var renderer = (SpriteRenderer)presenterType.GetField("targetRenderer",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(presenter);
                Sprite reference = Resources.Load<Sprite>("Game/sprites/" + prefabName);
                bool playerSpirit = prefabName == WindSpirit;
                if (playerSpirit)
                    Assert.That(unit.GetComponent("HoverMotionController"), Is.Null);
                Assert.That(renderer.flipX, Is.EqualTo(direction < 0));

                object frame = decode.Invoke(null, new object[] { "{\"update\":[{\"id\":42,\"master\":\"" + master + "\"}]}" });
                object delta = ((Array)infoType.GetField("update").GetValue(frame)).GetValue(0);
                FieldInfo positionField = delta.GetType().GetField("position");
                FieldInfo statusField = delta.GetType().GetField("status");
                float x = 4f;
                int rendersPerTick = RenderRate / tickRate;
                void Send(float y, string status = null)
                {
                    positionField.SetValue(delta, new Vector3(x, y, 5f));
                    statusField.SetValue(delta, status);
                    apply.Invoke(null, new[] { frame, null, world });
                }

                var observed = new List<Sprite>();
                for (int render = 0; render < 158; render++)
                {
                    // 정지 중 간헐적으로 도착하는 위치가 대기 루프를 다시 시작시키지 않아야 한다.
                    if (render % 45 == 0) Send(3f);
                    yield return null;
                    observed.Add(renderer.sprite);
                }
                AssertPoses(observed, prefabName + "Hover", reference);
                Assert.That(observed.All(sprite => sprite.name.StartsWith(prefabName + "Hover", StringComparison.Ordinal)), Is.True);

                observed.Clear();
                for (int tick = 0; tick < Mathf.CeilToInt(tickRate * 1.18f); tick++)
                {
                    x += direction * 1.2f / tickRate;
                    Send(3f);
                    for (int i = 0; i < rendersPerTick; i++)
                    {
                        yield return null;
                        observed.Add(renderer.sprite);
                    }
                }
                AssertPoses(observed, prefabName + "Glide", reference);
                Assert.That(renderer.flipX, Is.EqualTo(direction < 0));

                if (levelRemoval)
                {
                    Sprite glide = renderer.sprite;
                    Send(3f, "Destroyed");
                    yield return null;
                    Assert.That(renderer.sprite, Is.SameAs(glide));
                    yield return new WaitForSeconds(0.12f);
                    Assert.That((Component)find.Invoke(world, new object[] { 42 }) == null, Is.True);
                    yield break;
                }

                x += direction * 4f;
                Send(3f);
                yield return null;
                Assert.That(renderer.sprite.name, Does.StartWith(prefabName + "Hover"));

                // 한 렌더 프레임에 위치가 두 번 갱신되는 녹화도 하강을 인식한다.
                Send(2.97f);
                Send(2.94f);
                yield return null;
                Assert.That(renderer.sprite.name, Is.EqualTo(prefabName + "Descent01"));
                Send(3f);
                yield return null;
                Assert.That(renderer.sprite.name, Does.StartWith(prefabName + "Hover"));

                observed.Clear();
                for (int tick = 1; tick < tickRate; tick++)
                {
                    float t = (float)tick / tickRate;
                    x += direction * 1.2f / tickRate;
                    Send(3f - (playerSpirit ? 0.235f * t : 2f * t * t));
                    for (int i = 0; i < rendersPerTick; i++)
                    {
                        yield return null;
                        observed.Add(renderer.sprite);
                    }
                }
                AssertPoses(observed, prefabName + "Descent", reference, playerSpirit ? 3 : 4);
                if (playerSpirit)
                {
                    Assert.That(renderer.sprite.name, Is.EqualTo(prefabName + "Descent03"));
                    Send(2.745f);
                }
                else
                {
                    Assert.That(renderer.sprite.name, Is.EqualTo(prefabName + "Descent04"));
                }

                Send(1f, "Destroyed");
                yield return null;
                Assert.That(renderer.sprite.name, Is.EqualTo(prefabName + "Descent04"));
                yield return new WaitForSeconds(0.12f);
                Assert.That((Component)find.Invoke(world, new object[] { 42 }) == null, Is.True);
            }
            finally
            {
                Object.Destroy(root);
                Time.captureFramerate = previousRenderRate;
                setRate.Invoke(null, new object[] { previousTickRate });
            }
        }

        private static void AssertPoses(List<Sprite> sprites, string prefix, Sprite reference, int count = 4)
        {
            Sprite[] frames = sprites.Where(sprite => sprite.name.StartsWith(prefix, StringComparison.Ordinal))
                .Distinct().ToArray();
            CollectionAssert.AreEqual(Enumerable.Range(1, count).Select(index => $"{prefix}{index:00}"),
                frames.Select(sprite => sprite.name));
            foreach (Sprite frame in frames)
            {
                Assert.That(frame.rect.size, Is.EqualTo(reference.rect.size));
                Assert.That(frame.pixelsPerUnit, Is.EqualTo(reference.pixelsPerUnit));
                Assert.That(frame.pivot, Is.EqualTo(reference.pivot));
            }
        }

        private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);
    }
}
