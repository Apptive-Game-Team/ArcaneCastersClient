using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WordOnline.Tests
{
    public class FactionSelectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator LobbyWithoutFactionOpensTheSharedSelectionScene()
        {
#if UNITY_EDITOR
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/UI/PlayerProgressionPanel.prefab"));
            var panel = root.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "PlayerProgressionPanel");
            var field = panel.GetType().GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(panel, Activator.CreateInstance(field.FieldType));
            yield return VerifyNavigation(panel, "CompleteInitialization", "FactionSelectionScene");
            if (root != null) Object.Destroy(root);
#else
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator SelectionOutsideOnboardingReturnsToLobby()
        {
#if UNITY_EDITOR
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("TutorialScene.FactionSelectionController")).First(value => value != null);
            var root = new GameObject("SelectionOnlyFixture");
            var controller = (MonoBehaviour)root.AddComponent(type);
            controller.enabled = false;
            yield return VerifyNavigation(controller, "ContinueAfterChoice", "LobbyScene");
            if (root != null) Object.Destroy(root);
#else
            yield return null;
#endif
        }

        private static IEnumerator VerifyNavigation(MonoBehaviour component, string method, string destination)
        {
            bool loaded = false;
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onLoaded = (scene, mode) => {
                if (scene.name != destination) return;
                loaded = true;
                // 실제 씬 이동을 검사하되 인증 및 보상 API 요청은 시작하지 않는다.
                foreach (var root in scene.GetRootGameObjects()) root.SetActive(false);
            };
            SceneManager.sceneLoaded += onLoaded;
            try
            {
                var routine = (IEnumerator)component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(component, null);
                Assert.That(routine.MoveNext(), Is.False);
                float deadline = Time.realtimeSinceStartup + 5;
                while (!loaded && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(loaded, Is.True, destination);
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(destination));
            }
            finally { SceneManager.sceneLoaded -= onLoaded; }
        }

        [UnityTest]
        public IEnumerator OpeningClearsPreviousSelectionAndPointerExitRestoresKeyboardFocus()
        {
#if UNITY_EDITOR
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Tutorial/FactionSelectionView.prefab"));
            var events = new GameObject("FocusTestEvents", typeof(EventSystem));
            var previous = new GameObject("PreviousRetry");
            try
            {
                yield return null;
                var view = root.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "FactionSelectionView");
                var type = view.GetType();
                EventSystem.current.SetSelectedGameObject(previous);
                type.GetMethod("Show").Invoke(view, new object[] { (Action)(() => { }), (Action)(() => { }) });
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
                var buttons = root.GetComponentsInChildren<UnityEngine.UI.Button>();
                EventSystem.current.SetSelectedGameObject(buttons[1].gameObject);
                type.GetMethod("Hover").Invoke(view, new object[] { 1 });
                type.GetMethod("ClearHover").Invoke(view, new object[] { 1 });
                type.GetMethod("Apply", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(view, new object[] { 1f });
                var data = new SerializedObject(view);
                var background = (UnityEngine.UI.Image)data.FindProperty("backgrounds").GetArrayElementAtIndex(2).objectReferenceValue;
                Assert.That(background.color.a, Is.EqualTo(1));
                type.GetMethod("Hide").Invoke(view, null);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
            }
            finally { Object.Destroy(root); Object.Destroy(events); Object.Destroy(previous); }
#else
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator BothHalvesRaycastToTheirRegionAndSubmitOnlyOnce()
        {
#if UNITY_EDITOR
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/Tutorial/FactionSelectionView.prefab"));
            var events = new GameObject("SelectionTestEvents", typeof(EventSystem));
            try
            {
                var view = root.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "FactionSelectionView");
                int calls = 0;
                view.GetType().GetMethod("Show").Invoke(view, new object[] { (Action)(() => calls++), (Action)(() => calls++) });
                yield return null;
                Canvas.ForceUpdateCanvases();
                foreach (float x in new[] { .15f, .4f, .6f, .85f })
                {
                    var pointer = new PointerEventData(events.GetComponent<EventSystem>()) {
                        position = new Vector2(Screen.width * x, Screen.height * .5f), button = PointerEventData.InputButton.Left };
                    var hits = new List<RaycastResult>();
                    events.GetComponent<EventSystem>().RaycastAll(pointer, hits);
                    Assert.That(hits.Count, Is.GreaterThan(0));
                    string expected = x < .5f ? "HellRegion" : "WorldTreeRegion";
                    Assert.That(hits[0].gameObject.name, Is.EqualTo(expected));
                    if (x == .4f || x == .6f)
                        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
                }
                Assert.That(calls, Is.EqualTo(1));
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(events);
            }
#else
            yield return null;
#endif
        }
    }
}
