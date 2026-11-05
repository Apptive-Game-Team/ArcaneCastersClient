using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class TutorialOverlayLayerTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(name)).First(type => type != null);

        [TestCase("LobbyScene", "LobbyTutorialController")]
        [TestCase("ManageDeckScene", "DeckTutorialController")]
        [TestCase("MagicBookScene", "MagicBookTutorialController")]
        public void TutorialUiSharesOverlayRenderingAndCoversOrdinaryModals(string sceneName, string controllerName)
        {
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
            string path = "Assets/Scenes/" + sceneName + ".unity";
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var controller = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                    .Single(component => component != null && component.GetType().Name == controllerName);
                var data = new SerializedObject(controller);
                var mask = (GameObject)data.FindProperty("mask").objectReferenceValue;
                var panel = (MonoBehaviour)data.FindProperty("panel").objectReferenceValue;
                var panelRoot = (GameObject)new SerializedObject(panel).FindProperty("root").objectReferenceValue;
                Assert.That(mask.GetComponentInParent<Canvas>().rootCanvas.renderMode,
                    Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                var canvas = panelRoot.GetComponentInParent<Canvas>();
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.sortingOrder, Is.GreaterThan(32));
                Assert.That(canvas.sortingOrder, Is.LessThan(40));
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void MaskTargetsAndNextPanelKeepTheirOrderAndRestoreTheirRaycasters()
        {
            var root = new GameObject("TutorialLayerFixture", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var panelCanvas = new GameObject("TutorialPanelFixture", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            object lift = null;
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panelCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                panelCanvas.GetComponent<Canvas>().sortingOrder = 39;
                var mask = Child("Mask", root.transform);
                var blocker = Child("Blocker", root.transform);
                var target = Child("Target", root.transform);
                var panelRoot = Child("Panel", panelCanvas.transform);
                var panel = panelRoot.AddComponent(RuntimeType("TutorialScene.TutorialPanel"));
                var panelData = new SerializedObject(panel);
                panelData.FindProperty("root").objectReferenceValue = panelRoot;
                panelData.ApplyModifiedPropertiesWithoutUndo();
                var controller = root.AddComponent(RuntimeType("TutorialScene.LobbyTutorialController"));
                var data = new SerializedObject(controller);
                data.FindProperty("mask").objectReferenceValue = mask;
                data.FindProperty("targetClickBlocker").objectReferenceValue = blocker;
                data.FindProperty("panel").objectReferenceValue = panel;
                data.ApplyModifiedPropertiesWithoutUndo();
                var type = controller.GetType().BaseType;
                lift = type.GetField("lift", Private).GetValue(controller);

                type.GetMethod("LiftMaskAboveModals", Private).Invoke(controller, null);
                type.GetMethod("LiftAboveMask", Private).Invoke(controller, new object[] { new[] { target.transform }, true, true });

                Assert.That(mask.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(24));
                Assert.That(target.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(mask.GetComponent<Canvas>().sortingOrder));
                Assert.That(blocker.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(target.GetComponent<Canvas>().sortingOrder));
                Assert.That(panelCanvas.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(blocker.GetComponent<Canvas>().sortingOrder));
                Assert.That(mask.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                Assert.That(target.GetComponent<GraphicRaycaster>(), Is.Null);
                lift.GetType().GetMethod("RestoreAll").Invoke(lift, null);
                Assert.That(mask.GetComponent<Canvas>(), Is.Null);
                Assert.That(mask.GetComponent<GraphicRaycaster>(), Is.Null);
                Assert.That(target.GetComponent<Canvas>(), Is.Null);
                Assert.That(blocker.GetComponent<Canvas>(), Is.Null);
            }
            finally
            {
                if (lift != null) lift.GetType().GetMethod("RestoreAll").Invoke(lift, null);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(panelCanvas);
            }
        }

        private static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent, false);
            return child;
        }
    }
}
