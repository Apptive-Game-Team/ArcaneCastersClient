using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Data;
using GameScene;
using Global;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class GamePreparationFlowTests
    {
        private GamePreparationOverlay overlay;
        private GameObject eventSystemObject;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!Application.isPlaying)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                yield return new EnterPlayMode();
            }
            SceneContext.ClearAdventureMatch();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (overlay != null) Object.Destroy(overlay.gameObject);
            if (eventSystemObject != null) Object.Destroy(eventSystemObject);
            SceneContext.ClearAdventureMatch();
            yield return null;
            yield return new ExitPlayMode();
        }

        private static T Field<T>(GamePreparationOverlay view, string name) =>
            (T)typeof(GamePreparationOverlay).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);

        [UnityTest]
        public IEnumerator VersusShowsActualMmrBlocksPointerAndWaitsForStartBeforeHiding()
        {
            overlay = GamePreparationOverlay.Show(new MatchedInfoDto {
                sessionId = "match", leftUser = new User(1,"Player","",0) { matchMmr = 0 },
                rightUser = new User(2,"Opponent","",0)
            });
            yield return null;
            Assert.That(Field<TMP_Text>(overlay,"leftRating").gameObject.activeSelf, Is.True);
            Assert.That(Field<TMP_Text>(overlay,"leftRating").text, Is.EqualTo("MMR 0"));
            Assert.That(Field<TMP_Text>(overlay,"rightRating").gameObject.activeSelf, Is.False);
            Assert.That(overlay.IsVisible, Is.True);
            eventSystemObject = new GameObject("PreparationTestEventSystem",typeof(EventSystem));
            Canvas.ForceUpdateCanvases();
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {
                position = new Vector2(Screen.width / 2f,Screen.height / 2f)
            },hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].gameObject.transform.IsChildOf(overlay.transform), Is.True);
            foreach (int count in new[] {3,2,1})
            {
                overlay.Render("countdown",count);
                Assert.That(Field<TMP_Text>(overlay,"countdownLabel").text, Is.EqualTo(count.ToString()));
                Assert.That(overlay.IsVisible, Is.True);
            }
            overlay.Render("running");
            overlay.HideAfterStarted();
            Assert.That(overlay.IsVisible, Is.True);
            yield return new WaitForSecondsRealtime(0.65f);
            Assert.That(overlay.IsVisible, Is.False);
            overlay.Render("reconnecting");
            Assert.That(overlay.IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator AdventureUsesSeparateContentAndKeepsVersusInformationHidden()
        {
            SceneContext.AdventureId = 1;
            SceneContext.AdventureName = "Test Adventure";
            SceneContext.AdventureStageNumber = 2;
            SceneContext.AdventureScenarioNumber = 1;
            overlay = GamePreparationOverlay.Show(new MatchedInfoDto {
                sessionId = "pve-1", mapType = "FOREST", leftUser = new User(1,"Player","",0),
                rightUser = new User(0,"dummy","",0)
            });
            yield return null;
            Assert.That(Field<GameObject>(overlay,"versusContent").activeSelf, Is.False);
            Assert.That(Field<GameObject>(overlay,"adventureContent").activeSelf, Is.True);
            Assert.That(Field<TMP_Text>(overlay,"adventureName").text, Is.EqualTo("Test Adventure"));
            Assert.That(Field<TMP_Text>(overlay,"adventureStage").gameObject.activeSelf, Is.True);
            overlay.Render("countdown",3);
            Assert.That(Field<TMP_Text>(overlay,"adventureCountdown").text, Is.EqualTo("3"));
        }
    }
}
