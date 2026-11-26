using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    public class VinePrefabTests
    {
        [Test]
        public void VineUnfurlsOnceOnItsExistingBodyAndCollider()
        {
            const BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Instance;
            var vine = Resources.Load<GameObject>("Prefabs/Vine");
            var components = vine.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "SpawnPopInEffect"));
            var owner = components.Single(component => component.GetType().Name == "ServedObject");
            var animation = components.Single(component => component.GetType().Name == "SpriteFrameAnimator");
            var renderer = (SpriteRenderer)owner.GetType().GetField("_spriteRenderer", fields).GetValue(owner);
            var type = animation.GetType();
            Assert.AreSame(renderer, type.GetField("spriteRenderer", fields).GetValue(animation));
            Assert.That((float)type.GetField("frameInterval", fields).GetValue(animation), Is.EqualTo(.05f).Within(.001f));
            Assert.IsFalse((bool)type.GetField("loop", fields).GetValue(animation));

            var frames = (Sprite[])type.GetField("frames", fields).GetValue(animation);
            var original = Resources.Load<Sprite>("Game/sprites/VineToss");
            Assert.That(frames, Has.Length.EqualTo(6));
            Assert.AreSame(frames[0], renderer.sprite);
            for (int index = 0; index < frames.Length; index++)
            {
                Sprite frame = frames[index];
                Assert.AreEqual($"VineGrowth{index + 1:00}", frame.name);
                Assert.AreEqual(original.rect.size, frame.rect.size);
                Assert.AreEqual(original.pixelsPerUnit, frame.pixelsPerUnit);
                Assert.AreEqual(original.pivot, frame.pivot);
            }

            var collider = renderer.GetComponent<BoxCollider2D>();
            var inherited = Resources.Load<GameObject>("Prefabs/Abstract/AbstractBuild").GetComponentInChildren<BoxCollider2D>();
            Assert.AreEqual(inherited.offset, collider.offset);
            Assert.AreEqual(inherited.size, collider.size);
            Assert.AreEqual(Vector3.one, renderer.transform.localScale);
        }
    }
}
