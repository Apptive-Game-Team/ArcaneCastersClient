using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class LobbyModalLayerTests
    {
        private const string MatchingPath = "Assets/Prefabs/UI/Lobby/MatchingPage.prefab";

        private static Canvas CanvasAt(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            var canvas = prefab.GetComponent<Canvas>();
            Assert.That(canvas, Is.Not.Null, path);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay), path);
            return canvas;
        }

        [Test]
        public void LobbyModalsCoverHudWhileCriticalMessagesKeepTheirPriority()
        {
            var hud = CanvasAt("Assets/Resources/UI/Progression/PlayerProgressionView.prefab");
            var matching = CanvasAt(MatchingPath);
            foreach (string path in new[] {
                "Assets/Prefabs/UI/Lobby/SettingPage.prefab",
                "Assets/Resources/CustomBotBattlePage.prefab",
                "Assets/Prefabs/UI/RewardUI.prefab"
            })
            {
                var modal = CanvasAt(path);
                Assert.That(modal.sortingOrder, Is.GreaterThan(hud.sortingOrder), path);
                Assert.That(modal.sortingOrder, Is.LessThan(matching.sortingOrder), path);
            }

            var chest = CanvasAt("Assets/Prefabs/UI/RewardChest/RewardChestPresenter.prefab");
            var confirm = CanvasAt("Assets/Prefabs/UI/Global/ConfirmPage.prefab");
            var loading = CanvasAt("Assets/Prefabs/LoadingPage.prefab");
            var message = CanvasAt("Assets/Prefabs/SystemMessage.prefab");
            var debug = CanvasAt("Assets/Prefabs/UI/Debug/BotThoughtLog.prefab");
            var update = CanvasAt("Assets/Prefabs/UI/Global/DesktopUpdateDialog.prefab");
            Assert.That(matching.sortingOrder, Is.GreaterThan(hud.sortingOrder));
            Assert.That(chest.sortingOrder, Is.GreaterThan(matching.sortingOrder));
            Assert.That(confirm.sortingOrder, Is.GreaterThan(chest.sortingOrder));
            Assert.That(loading.sortingOrder, Is.GreaterThan(confirm.sortingOrder));
            Assert.That(message.sortingOrder, Is.GreaterThan(loading.sortingOrder));
            Assert.That(debug.sortingOrder, Is.GreaterThan(message.sortingOrder));
            Assert.That(update.sortingOrder, Is.GreaterThan(debug.sortingOrder));
        }

        [Test]
        public void MatchingBackdropCoversTheCanvasAndHidesWithTheModal()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MatchingPath));
            var modelRoot = new GameObject("MatchingLayerStateFixture");
            modelRoot.SetActive(false);
            try
            {
                foreach (var localized in root.GetComponentsInChildren<LocalizeStringEvent>(true)) localized.enabled = false;
                var page = root.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "MatchingPage");
                var data = new SerializedObject(page);
                var content = (GameObject)data.FindProperty("content").objectReferenceValue;
                var backdrop = content.GetComponent<Image>();
                var rect = (RectTransform)content.transform;
                Assert.That(content.activeSelf, Is.False);
                Assert.That(backdrop.raycastTarget, Is.True);
                Assert.That(backdrop.sprite, Is.Null);
                Assert.That(backdrop.color.r, Is.EqualTo(10f / 255f).Within(0.0001f));
                Assert.That(backdrop.color.g, Is.EqualTo(18f / 255f).Within(0.0001f));
                Assert.That(backdrop.color.b, Is.EqualTo(30f / 255f).Within(0.0001f));
                Assert.That(backdrop.color.a, Is.EqualTo(158f / 255f).Within(0.0001f));
                Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));

                var modelType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("LobbyScene.LobbySceneViewModel")).First(type => type != null);
                var model = modelRoot.AddComponent(modelType);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                page.GetType().GetMethod("Awake", flags).Invoke(page, null);
                page.GetType().GetField("viewModel", flags).SetValue(page, model);
                var currentState = modelType.GetField("CurrentState").GetValue(model);
                var update = currentState.GetType().GetMethod("UpdateData");
                var stateType = update.GetParameters()[0].ParameterType;
                var stateChanged = currentState.GetType().GetEvent("OnStateChange");
                var applyLobbyState = page.GetType().GetMethod("ApplyLobbyState", flags);
                stateChanged.AddEventHandler(currentState,
                    Delegate.CreateDelegate(stateChanged.EventHandlerType, page, applyLobbyState));

                foreach (string state in new[] { "Matching", "Idle", "PreparingPractice", "Idle" })
                {
                    update.Invoke(currentState, new[] { Enum.Parse(stateType, state) });
                    Assert.That(content.activeSelf, Is.EqualTo(state != "Idle"), state);
                    Assert.That(backdrop.gameObject.activeInHierarchy, Is.EqualTo(state != "Idle"), state);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(modelRoot);
            }
        }

    }
}
