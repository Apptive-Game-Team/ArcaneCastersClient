using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace WordOnline.Tests
{
    public class FactionSelectionViewTests
    {
        private GameObject root;
        private MonoBehaviour view;
        private Locale previousLocale;
        private const string Path = "Assets/Prefabs/UI/Tutorial/FactionSelectionView.prefab";
        private void Call(string method, params object[] arguments) =>
            view.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, arguments);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // EditMode에서는 언어가 자동 선택되지 않으므로 Show의 번역 조회 전에 준비한다.
            previousLocale = LocalizationSettings.SelectedLocale;
            var locale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/English (en).asset");
            Assert.IsNotNull(locale);
            LocalizationSettings.SelectedLocale = locale;
            var initialization = LocalizationSettings.InitializationOperation;
            double deadline = EditorApplication.timeSinceStartup + 30d;
            while (!initialization.IsDone)
            {
                Assert.Less(EditorApplication.timeSinceStartup, deadline, "언어 초기화가 완료되지 않았습니다.");
                yield return null;
            }
            var table = LocalizationSettings.StringDatabase.GetTableAsync("Onboarding");
            while (!table.IsDone)
            {
                Assert.Less(EditorApplication.timeSinceStartup, deadline, "Onboarding 번역 테이블을 불러오지 못했습니다.");
                yield return null;
            }
            Assert.IsNotNull(table.Result);
            root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Path));
            view = root.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "FactionSelectionView");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            LocalizationSettings.SelectedLocale = previousLocale;
        }

        [Test]
        public void WholeRegionsSubmitOnlyOneChoiceAndCanBeReopened()
        {
            int hell = 0, nature = 0;
            Call("Show", (Action)(() => hell++), (Action)(() => nature++));
            var buttons = root.GetComponentsInChildren<Button>();
            Assert.That(buttons.Length, Is.EqualTo(2));
            foreach (var button in buttons)
            {
                var rect = button.GetComponent<RectTransform>();
                Assert.That(rect.anchorMax.y - rect.anchorMin.y, Is.EqualTo(1));
                Assert.That(rect.anchorMax.x - rect.anchorMin.x, Is.EqualTo(.5f));
                Assert.That(button.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That(button.navigation.selectOnRight, Is.Not.Null);
            }
            buttons[0].onClick.Invoke();
            buttons[1].onClick.Invoke();
            Assert.That(hell, Is.EqualTo(1));
            Assert.That(nature, Is.Zero);
            Assert.That(buttons.All(button => !button.interactable), Is.True);
            Call("Hide");
            Call("Show", (Action)(() => hell++), (Action)(() => nature++));
            buttons[1].onClick.Invoke();
            Assert.That(nature, Is.EqualTo(1));
        }

        [Test]
        public void HoverAndKeyboardFocusChangeTheBackgroundAndPreserveOriginalSprites()
        {
            Call("Show", (Action)(() => { }), (Action)(() => { }));
            var data = new SerializedObject(view);
            var backgrounds = data.FindProperty("backgrounds");
            var regions = root.GetComponentsInChildren<MonoBehaviour>().Where(component => component.GetType().Name == "FactionSelectionRegion").ToArray();
            var sprites = root.GetComponentsInChildren<Image>().Where(image => image.sprite != null &&
                AssetDatabase.GetAssetPath(image.sprite).StartsWith("Assets/Resources/Game/sprites/")).ToArray();
            Assert.That(sprites.Length, Is.EqualTo(6));
            foreach (var sprite in sprites)
                Assert.That(sprite.sprite, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(sprite.sprite))));
            ExecuteEvents.Execute(regions[0].gameObject, new PointerEventData(null), ExecuteEvents.pointerEnterHandler);
            Call("Apply", 1f);
            Assert.That(((Image)backgrounds.GetArrayElementAtIndex(1).objectReferenceValue).color.a, Is.EqualTo(1));
            Call("Preview", 2);
            Call("Apply", .5f);
            Assert.That(((Image)backgrounds.GetArrayElementAtIndex(1).objectReferenceValue).color.a, Is.EqualTo(1).Within(.0001f));
            Assert.That(((Image)backgrounds.GetArrayElementAtIndex(2).objectReferenceValue).color.a, Is.EqualTo(.5f).Within(.0001f));
            ExecuteEvents.Execute(regions[1].gameObject, new BaseEventData(null), ExecuteEvents.selectHandler);
            ExecuteEvents.Execute(regions[0].gameObject, new PointerEventData(null), ExecuteEvents.pointerExitHandler);
            Call("Apply", 1f);
            Assert.That(((Image)backgrounds.GetArrayElementAtIndex(2).objectReferenceValue).color.a, Is.EqualTo(1));
            Assert.That(((Image)backgrounds.GetArrayElementAtIndex(1).objectReferenceValue).color.a, Is.Zero);
        }
    }
}
