using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WordOnline.Tests {
    public class AdminRoomSceneTests {
        private Scene scene;

        [TearDown]
        public void CloseScene() {
            if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        private Component[] Open(string name) {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity", OpenSceneMode.Additive);
            return scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Component>(true)).ToArray();
        }

        [Test]
        public void AdminHasBattleAndRoomNavigationWithoutRoomList() {
            var components = Open("AdminScene");
            Assert.IsTrue(components.Any(c => c != null && c.GetType().Name == "LocalLearnedBotButton"));
            Assert.IsFalse(components.Any(c => c != null && c.GetType().Name == "RoomUIFactory"));
            Assert.IsFalse(components.OfType<InputField>().Any(i => i.name == "RoomFilterInput"));
            var link = components.Single(c => c != null && c.GetType().Name == "AdminSceneLink");
            Assert.AreEqual("Scenes/AdminRoomsScene", new SerializedObject(link).FindProperty("sceneName").stringValue);
            Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(link.gameObject));
        }

        [Test]
        public void RoomSceneHasSearchListAndReturnNavigation() {
            var components = Open("AdminRoomsScene");
            Assert.IsTrue(components.Any(c => c != null && c.GetType().Name == "RoomUIFactory"));
            Assert.IsFalse(components.Any(c => c != null && c.GetType().Name == "LocalLearnedBotButton"));
            var link = components.Single(c => c != null && c.GetType().Name == "AdminSceneLink");
            Assert.AreEqual("Scenes/AdminScene", new SerializedObject(link).FindProperty("sceneName").stringValue);
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == "Assets/Scenes/AdminRoomsScene.unity"));
            var input = components.OfType<InputField>().Single(i => i.name == "RoomFilterInput");
            Assert.IsNotNull(input.textComponent.font);
            Canvas.ForceUpdateCanvases();
            input.text = "room-123 사용자";
            input.ForceLabelUpdate();
            Assert.AreEqual(input.text, input.textComponent.text);
            var rect = input.textComponent.rectTransform.rect;
            Assert.GreaterOrEqual(rect.height, 30);
            var settings = input.textComponent.GetGenerationSettings(rect.size);
            using (var generator = new TextGenerator()) {
                generator.Populate(input.text, settings);
                Assert.Greater(generator.vertexCount, 4, "검색 문자열이 실제 글자 정점을 생성해야 합니다.");
            }
            var factory = components.Single(c => c != null && c.GetType().Name == "RoomUIFactory");
            Assert.AreEqual(input, new SerializedObject(factory).FindProperty("filterInput").objectReferenceValue);
        }

        [TestCase("AdminScene")]
        [TestCase("AdminRoomsScene")]
        public void PagesUseTheSharedBackground(string name) {
            var components = Open(name);
            var background = components.OfType<Image>().Single(i => i.name == "Background"
                && i.transform.parent != null && i.transform.parent.GetComponent<Canvas>() != null
                && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(i.sprite)) == "4fe2d02ca2c9b49fc938a577493218e1");
            Assert.IsTrue(background.preserveAspect);
            Assert.AreEqual("4fe2d02ca2c9b49fc938a577493218e1", AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(background.sprite)));
        }
    }
}
