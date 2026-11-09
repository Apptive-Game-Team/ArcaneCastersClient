using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class ChestPresentationAssetsTests
    {
        [Test]
        public void ProgressionOpeningUsesAlignedFramesWhileListKeepsTheTightIcon()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/RewardChest/RewardChestPresenter.prefab"));
            try
            {
                var presenter = root.GetComponents<MonoBehaviour>().Single(value => value.GetType().Name == "RewardChestPresenter");
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                presenter.GetType().GetMethod("ChooseChestSprites", flags).Invoke(presenter, new object[] { "progression_chest" });
                var closed = (Sprite)presenter.GetType().GetField("activeClosedSprite", flags).GetValue(presenter);
                var opened = (Sprite)presenter.GetType().GetField("activeOpenSprite", flags).GetValue(presenter);
                Assert.That(closed, Is.SameAs(Resources.Load<Sprite>("RewardChest/Chests/default/animation_closed")));
                Assert.That(opened, Is.SameAs(Resources.Load<Sprite>("RewardChest/Chests/default/open")));
                Assert.That(closed, Is.Not.SameAs(Resources.Load<Sprite>("RewardChest/Chests/default/closed")));
                Assert.That(closed.rect.size, Is.EqualTo(opened.rect.size));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void OpeningFramePairHasTransparentCornersAndTheSameGroundBaseline()
        {
            var closed = Load("animation_closed");
            var opened = Load("open");
            try
            {
                Assert.That(closed.width, Is.EqualTo(opened.width));
                Assert.That(closed.height, Is.EqualTo(opened.height));
                Assert.That(LowestOpaquePixel(closed), Is.EqualTo(LowestOpaquePixel(opened)));
                foreach (var frame in new[] { closed, opened })
                {
                    Assert.That(frame.GetPixel(0, 0).a, Is.Zero);
                    Assert.That(frame.GetPixel(frame.width - 1, 0).a, Is.Zero);
                    Assert.That(frame.GetPixel(0, frame.height - 1).a, Is.Zero);
                    Assert.That(frame.GetPixel(frame.width - 1, frame.height - 1).a, Is.Zero);
                }
            }
            finally { Object.DestroyImmediate(closed); Object.DestroyImmediate(opened); }
        }

        private static Texture2D Load(string state)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes("Assets/Resources/RewardChest/Chests/default/" + state + ".png")), Is.True);
            return texture;
        }

        private static int LowestOpaquePixel(Texture2D texture)
        {
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (texture.GetPixel(x, y).a > 0.1f) return y;
            Assert.Fail("상자 프레임은 불투명한 몸체를 포함해야 한다.");
            return -1;
        }
    }
}
