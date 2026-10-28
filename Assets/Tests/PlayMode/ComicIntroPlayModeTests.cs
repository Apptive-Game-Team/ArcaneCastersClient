#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class ComicIntroPlayModeTests
    {
        private GameObject root;
        private MonoBehaviour player;
        private int tutorialLoads;
        private float previousTimeScale;

        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void CreatePlayer()
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Tutorial/ComicIntroPlayer.prefab"));
            player = root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "ComicIntroPlayer");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "InteractiveTutorialScene") return;
            tutorialLoads++;
            // 진입까지만 검사한다. 인증 없는 테스트에서 전투 데이터 HTTP 요청을 시작하지 않는다.
            foreach (GameObject sceneRoot in scene.GetRootGameObjects()) sceneRoot.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Time.timeScale = previousTimeScale;
            if (root != null) Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ProgressivePanelsKeepPreviousCaptionsAndFinishInTutorialOnce()
        {
            CreatePlayer();
            float deadline = Time.realtimeSinceStartup + 15;
            while (!(bool)Get("ready") && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Get("ready"), Is.True, "번역 초기화 후 첫 컷이 나타나야 한다.");
            Time.timeScale = 0;
            for (int i = 0; i < 8; i++)
            {
                yield return new WaitForSecondsRealtime(0.18f);
                Invoke("Advance");
                Assert.That(Get("beatIndex"), Is.EqualTo(i), "타이핑 중 입력은 현재 대사만 완성해야 한다.");
                Assert.That(Get("textComplete"), Is.True);
                Invoke("Advance");
                Assert.That(Get("beatIndex"), Is.EqualTo(i), "같은 프레임의 중복 입력으로 컷을 넘기면 안 된다.");
                if (i == 1)
                {
                    var panels = (Array)Get("panels");
                    foreach (int index in new[] { 0, 1 })
                    {
                        var view = (Component)panels.GetValue(index);
                        Assert.That(((CanvasGroup)Field(view, "panelGroup")).alpha, Is.EqualTo(1));
                        Assert.That(((CanvasGroup[])Field(view, "captionGroups"))[0].alpha, Is.EqualTo(1));
                    }
                }
                if (i == 5)
                {
                    var view = (Component)((Array)Get("panels")).GetValue(4);
                    foreach (CanvasGroup caption in (CanvasGroup[])Field(view, "captionGroups"))
                        Assert.That(caption.alpha, Is.EqualTo(1), "전투 컷의 양쪽 대사가 함께 남아야 한다.");
                }
                if (i == 7)
                {
                    foreach (Component view in (Array)Get("panels"))
                    {
                        Assert.That(view.gameObject.activeInHierarchy, Is.True, "마지막 컷에서도 이전 7개 컷이 한 화면에 남아야 한다.");
                        Assert.That(((CanvasGroup)Field(view, "panelGroup")).alpha, Is.EqualTo(1));
                    }
                }
                yield return new WaitForSecondsRealtime(0.18f);
                Invoke("Advance");
            }
            Invoke("Skip");
            deadline = Time.realtimeSinceStartup + 8;
            while (tutorialLoads == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(tutorialLoads, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("InteractiveTutorialScene"));
        }

        [UnityTest]
        public IEnumerator SkipBeforeLocalizationStartsStillEntersTutorialOnce()
        {
            CreatePlayer();
            Assert.That(Get("ready"), Is.False);
            Invoke("Skip");
            Invoke("Skip");
            float deadline = Time.realtimeSinceStartup + 8;
            while (tutorialLoads == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(tutorialLoads, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("InteractiveTutorialScene"));
        }

        private object Get(string name) => Field(player, name);

        private static object Field(Component component, string name)
        {
            return component.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component);
        }

        private void Invoke(string name)
        {
            player.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public).Invoke(player, null);
        }
    }
}
#endif
