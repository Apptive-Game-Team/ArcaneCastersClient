using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class FireworkShellFlightPrefabTests
    {
        [Test]
        public void FlightFramesKeepTheShellAnchorOnTheArcRenderer()
        {
            var projectile = Resources.Load<GameObject>("Projectiles/FireworkShell");
            var arc = new SerializedObject(projectile.GetComponents<MonoBehaviour>()
                .Single(component => component.GetType().Name == "ArcProjectile"));
            var renderer = (SpriteRenderer)arc.FindProperty("flightSpriteRenderer").objectReferenceValue;
            var original = Resources.Load<Sprite>("Game/sprites/FireworkShell");
            Assert.AreSame(projectile.transform.Find("actualObject").GetComponent<SpriteRenderer>(), renderer);
            Assert.AreSame(renderer.transform, arc.FindProperty("actualObject").objectReferenceValue);
            Assert.AreSame(original, renderer.sprite);
            Assert.AreEqual(2.5f, arc.FindProperty("arcHeight").floatValue);

            var frames = arc.FindProperty("flightFrames");
            Assert.AreEqual(4, frames.arraySize);
            for (int index = 0; index < frames.arraySize; index++)
            {
                var sprite = (Sprite)frames.GetArrayElementAtIndex(index).objectReferenceValue;
                Assert.AreEqual($"FireworkShellFlight{index + 1:00}", sprite.name);
                Assert.AreEqual(new Vector2(160, 109), sprite.rect.size);
                Assert.That(Vector2.Distance(original.pivot + new Vector2(32, 0), sprite.pivot), Is.LessThan(.01f));
                Assert.AreEqual(original.pixelsPerUnit, sprite.pixelsPerUnit);
            }

            var impact = Resources.Load<GameObject>("Prefabs/FireworkShell");
            Assert.IsTrue(impact.GetComponentsInChildren<SpriteRenderer>(true)
                .Any(body => body.sprite != null && body.sprite.name == "FireworkExplosion"));
        }
    }
}
