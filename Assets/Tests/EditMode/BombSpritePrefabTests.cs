using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace WordOnline.Tests
{
    public class BombSpritePrefabTests
    {
        [Test]
        public void BombSpriteUsesDrawnHoverAndReleasePosesOnItsBody()
        {
            const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
            var prefab = Resources.Load<GameObject>("Prefabs/BombSprite");
            var components = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.IsFalse(components.Any(component => component.GetType().Name == "HoverMotionController"));
            Assert.IsTrue(components.Any(component => component.GetType().Name == "OnDestroySpawner"));
            Assert.IsTrue(components.Any(component => component.GetType().Name == "DeathPresentationController"));
            var owner = components.Single(component => component.GetType().Name == "ServedObject");
            Assert.IsFalse((bool)owner.GetType().GetField("_swingOnAttack", Fields).GetValue(owner));
            Assert.IsTrue((bool)owner.GetType().GetField("_keepUprightOnSpawn", Fields).GetValue(owner));
            var controller = components.Single(component => component.GetType().Name == "AerialUnitPoseController");
            var body = (SpriteRenderer)controller.GetType().GetField("targetRenderer", Fields).GetValue(controller);
            Assert.AreSame(owner.GetType().GetField("_spriteRenderer", Fields).GetValue(owner), body);
            var master = Resources.Load<Sprite>("Game/sprites/BombSprite");

            void CheckFrames(string field, string pose, int count)
            {
                var frames = (Sprite[])controller.GetType().GetField(field, Fields).GetValue(controller);
                CollectionAssert.AreEqual(Enumerable.Range(1, count).Select(index => $"BombSprite{pose}{index:00}"),
                    frames.Select(frame => frame.name));
                foreach (Sprite frame in frames)
                {
                    Assert.AreEqual(master.rect, frame.rect);
                    Assert.AreEqual(master.pivot, frame.pivot);
                    Assert.AreEqual(master.pixelsPerUnit, frame.pixelsPerUnit);
                }
            }

            CheckFrames("hoverFrames", "Hover", 6);
            CheckFrames("releaseFrames", "Release", 4);
            Assert.AreEqual("BombSpriteHover01", body.sprite.name);
        }
    }
}
