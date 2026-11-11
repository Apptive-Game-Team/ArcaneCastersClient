using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class MobRecoilScaleTests
    {
        [TestCase(0.6f, 0.6f, 1f)]
        [TestCase(1f, 1f, 1f)]
        [TestCase(1.4f, 0.7f, 0.8f)]
        public void RepeatedRecoilPreservesAuthoredScale(float x, float y, float z)
        {
            var body = new GameObject("recoil-scale-test");
            Vector3 baseline = new Vector3(x, y, z);
            body.transform.localScale = baseline;
            Type actions = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("GameScene.ServedObjectComponent.DOTweenAction"))
                .First(type => type != null);
            object sequence = null;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    sequence = actions.GetMethod("BounceMob").Invoke(null, new object[] { body.transform });
                    Seek(sequence, 0.1f);
                    AssertScale(new Vector3(x * 0.8f, y * 1.2f, z), body.transform.localScale);
                    Seek(sequence, 0.2f);
                    AssertScale(baseline, body.transform.localScale);
                }
            }
            finally
            {
                if (sequence != null)
                {
                    TweenExtension(sequence, "Kill").Invoke(null, new[] { sequence, (object)false });
                }
                Object.DestroyImmediate(body);
            }
        }

        [Test]
        public void RockGolemKeepsItsAuthoredBodyScaleAndAttackFrameHeight()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RockGolem");
            Assert.IsNotNull(prefab);
            Sprite idle = Resources.Load<Sprite>("Game/sprites/RockGolem");
            Sprite attack = Resources.Load<Sprite>("Game/sprites/RockGolem2");
            SpriteRenderer body = prefab.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(renderer => renderer.sprite == idle);
            AssertScale(new Vector3(0.6f, 0.6f, 1f), body.transform.localScale);
            Component motion = prefab.GetComponentsInChildren<Component>(true)
                .Single(component => component != null && component.GetType().Name == "UnitSpriteMotionController");
            var serialized = new SerializedObject(motion);
            Assert.AreEqual(attack, serialized.FindProperty("attackFrames")
                .GetArrayElementAtIndex(0).objectReferenceValue);
            foreach (string field in new[] { "moveFrames", "attackFrames" })
            {
                SerializedProperty frames = serialized.FindProperty(field);
                Assert.AreEqual(8, frames.arraySize);
                for (int i = 0; i < frames.arraySize; i++)
                {
                    var frame = (Sprite)frames.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.IsNotNull(frame, $"{field}[{i}]");
                    Assert.AreEqual(idle.rect.size, frame.rect.size);
                    Assert.AreEqual(idle.pixelsPerUnit, frame.pixelsPerUnit);
                    Assert.AreEqual(idle.pivot, frame.pivot);
                }
            }
        }

        private static void Seek(object sequence, float seconds)
        {
            TweenExtension(sequence, "Goto").Invoke(null, new[] { sequence, (object)seconds, false });
        }

        private static MethodInfo TweenExtension(object sequence, string method)
        {
            return sequence.GetType().Assembly.GetType("DG.Tweening.TweenExtensions")
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(candidate => candidate.Name == method && !candidate.IsGenericMethod);
        }

        private static void AssertScale(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 0.0001f);
        }
    }
}
