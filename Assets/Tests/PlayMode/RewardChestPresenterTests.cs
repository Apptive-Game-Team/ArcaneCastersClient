#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RewardChest;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class RewardChestPresenterTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [UnityTest]
        public IEnumerator DefaultChestOpensAfterBothSkipTimingsAndReplaysFromClosed()
        {
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 120;
            var driver = new GameObject("RewardChestTestDriver", typeof(Image)).GetComponent<Image>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/RewardChest/RewardChestPresenter.prefab");
            GameObject root = Object.Instantiate(prefab, driver.transform);
            try
            {
                var presenter = root.GetComponents<MonoBehaviour>().Single(value => value.GetType().Name == "RewardChestPresenter");
                var type = presenter.GetType();
                var body = (Image)type.GetField("chestBodyImage", PrivateInstance).GetValue(presenter);
                var button = (Button)type.GetField("dismissButton", PrivateInstance).GetValue(presenter);
                var continueHint = (GameObject)type.GetField("continueHint", PrivateInstance).GetValue(presenter);
                var play = type.GetMethod("Play");
                var rewards = Array.Empty<RewardView>();
                string[] skipOn = { "animation_closed", "opening_03", null };

                foreach (string skipPose in skipOn)
                {
                    bool dismissed = false;
                    bool skipped = false;
                    var poses = new List<string>();
                    driver.StartCoroutine((IEnumerator)play.Invoke(presenter,
                        new object[] { rewards, (Action)(() => dismissed = true), "default" }));
                    Assert.That(body.sprite.name, Is.EqualTo("animation_closed"));

                    for (int frame = 0; frame < 240 && !continueHint.activeSelf; frame++)
                    {
                        string pose = body.sprite.name;
                        if (poses.Count == 0 || poses[poses.Count - 1] != pose)
                        {
                            poses.Add(pose);
                        }
                        if (!skipped && pose == skipPose)
                        {
                            button.onClick.Invoke();
                            skipped = true;
                        }
                        yield return null;
                    }

                    Assert.That(continueHint.activeSelf, Is.True);
                    Assert.That(body.sprite.name, Is.EqualTo("open"));
                    Assert.That(skipped, Is.EqualTo(skipPose != null));
                    if (skipPose == null)
                    {
                        Assert.That(poses, Is.EqualTo(new[]
                        {
                            "animation_closed", "opening_01", "opening_02", "opening_03", "opening_04", "opening_05", "open"
                        }));
                    }

                    button.onClick.Invoke();
                    for (int frame = 0; frame < 60 && !dismissed; frame++)
                    {
                        yield return null;
                    }
                    Assert.That(dismissed, Is.True);
                    Assert.That(root.activeSelf, Is.False);
                }
            }
            finally
            {
                Time.captureFramerate = previousRate;
                Object.Destroy(driver.gameObject);
            }
        }
    }
}
#endif
