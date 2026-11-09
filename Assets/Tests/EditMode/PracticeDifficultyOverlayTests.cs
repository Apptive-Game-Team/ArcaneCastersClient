using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WordOnline.Tests
{
    public class PracticeDifficultyOverlayTests
    {
        [Test]
        public void LobbyPracticePanelBlocksTheHudAndKeepsItsButtonReference()
        {
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
            const string scenePath = "Assets/Scenes/LobbyScene.unity";
            var scene = SceneManager.GetSceneByPath(scenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var components = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                    .Where(component => component != null).ToArray();
                var panel = components.Single(component => component.GetType().Name == "PracticeDifficultyPanel");
                var button = components.Single(component => component.GetType().Name == "PracticeButton");
                var host = panel.transform.parent;
                Assert.That(host.parent, Is.Null);
                var canvas = host.GetComponent<Canvas>();
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.sortingOrder, Is.EqualTo(22));
                Assert.That(host.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                var scaler = host.GetComponent<CanvasScaler>();
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
                Assert.That(panel.gameObject.activeSelf, Is.False);
                Assert.That(new SerializedObject(button).FindProperty("difficultyPanel").objectReferenceValue,
                    Is.EqualTo(panel));

                var backdrop = (GameObject)new SerializedObject(panel).FindProperty("backdrop").objectReferenceValue;
                Assert.That(backdrop, Is.Not.Null);
                Assert.That(backdrop.transform.parent, Is.EqualTo(host));
                Assert.That(backdrop.transform.GetSiblingIndex(), Is.LessThan(panel.transform.GetSiblingIndex()));
                Assert.That(backdrop.activeSelf, Is.False);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(backdrop), Is.Not.Null);
                var rect = (RectTransform)backdrop.transform;
                Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(rect.sizeDelta, Is.EqualTo(Vector2.zero));
                var image = backdrop.GetComponent<Image>();
                Assert.That(image.raycastTarget, Is.True);
                Assert.That(image.color.a, Is.EqualTo(158f / 255).Within(0.001f));
                Assert.That(image.sprite, Is.Null);
                foreach (string field in new[] { "quickButton", "backButton" })
                {
                    var control = (UnityEngine.UI.Button)new SerializedObject(panel).FindProperty(field).objectReferenceValue;
                    Assert.That(control.interactable, Is.True, field);
                }
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
