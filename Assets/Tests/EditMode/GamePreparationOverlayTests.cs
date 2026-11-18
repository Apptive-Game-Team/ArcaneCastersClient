using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace WordOnline.Tests
{
    public class GamePreparationOverlayTests
    {
        private const string PrefabPath = "Assets/Resources/UI/GamePreparationOverlay.prefab";

        [Test]
        public void OverlayCoversGameUiAndBindsBothPlayersAndFailureActions()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(prefab.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(3500));
            Assert.That(prefab.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);

            var backdrop = prefab.transform.Find("Backdrop");
            Assert.That(backdrop.GetComponent<RectTransform>().anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(backdrop.GetComponent<RectTransform>().anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(backdrop.GetComponent<Image>().raycastTarget, Is.True);

            var overlay = prefab.GetComponents<MonoBehaviour>()
                .Single(component => component != null && component.GetType().Name == "GamePreparationOverlay");
            var serialized = new SerializedObject(overlay);
            foreach (var prefix in new[] { "left", "right" })
            {
                var portrait = (Image)serialized.FindProperty(prefix + "Portrait").objectReferenceValue;
                Assert.That(portrait.sprite, Is.Not.Null);
                Assert.That(portrait.preserveAspect, Is.True);

                var name = (TMP_Text)serialized.FindProperty(prefix + "Name").objectReferenceValue;
                Assert.That(name.richText, Is.False);
                Assert.That(name.enableAutoSizing, Is.True);
                Assert.That(name.overflowMode, Is.EqualTo(TextOverflowModes.Ellipsis));

                var rating = (TMP_Text)serialized.FindProperty(prefix + "Rating").objectReferenceValue;
                float nameBottom = name.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition.y
                    + name.rectTransform.anchoredPosition.y - name.rectTransform.sizeDelta.y / 2;
                float ratingTop = rating.rectTransform.anchoredPosition.y + rating.rectTransform.sizeDelta.y / 2;
                Assert.That(ratingTop, Is.LessThan(nameBottom));
            }

            foreach (var field in new[] { "cancelButton", "retryButton" })
            {
                var button = (Button)serialized.FindProperty(field).objectReferenceValue;
                Assert.That(button.gameObject.activeSelf, Is.False);
                Assert.That(button.targetGraphic.raycastTarget, Is.True);
                Assert.That(button.onClick.GetPersistentEventCount(), Is.Zero);
            }

            var versus = (GameObject)serialized.FindProperty("versusContent").objectReferenceValue;
            var adventure = (GameObject)serialized.FindProperty("adventureContent").objectReferenceValue;
            var art = (Image)serialized.FindProperty("adventureArt").objectReferenceValue;
            Assert.That(versus.activeSelf, Is.True);
            Assert.That(adventure.activeSelf, Is.False);
            Assert.That(adventure.GetComponentsInChildren<Button>(true), Is.Empty);
            Assert.That(art.transform.IsChildOf(adventure.transform), Is.True);
            Assert.That(art.sprite, Is.Not.Null);
            Assert.That(serialized.FindProperty("fortressArt").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("gateArt").objectReferenceValue, Is.Not.Null);
            Assert.That(((TMP_Text)serialized.FindProperty("adventureCountdown").objectReferenceValue)
                .transform.parent.gameObject.activeSelf, Is.False);
        }

        [TestCase(1280, 720)]
        [TestCase(844, 390)]
        [TestCase(390, 844)]
        public void MatchupAndFailureActionsFitViewport(int width, int height)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var scaler = prefab.GetComponent<CanvasScaler>();
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            float scale = Mathf.Min(width / scaler.referenceResolution.x, height / scaler.referenceResolution.y);
            var backdrop = prefab.transform.Find("Backdrop");
            foreach (string name in new[] { "Title", "VersusContent/LeftPlayer", "VersusContent/RightPlayer",
                "VersusContent/VersusBadge", "AdventureContent/AdventureCard", "StatusPlate", "LeaveButton", "RetryButton" })
            {
                var child = (RectTransform)backdrop.Find(name);
                Assert.That(child.anchorMin, Is.EqualTo(new Vector2(.5f, .5f)), name);
                Assert.That(child.anchorMax, Is.EqualTo(new Vector2(.5f, .5f)), name);
                Assert.That(child.localScale, Is.EqualTo(Vector3.one), name);
                Assert.That((Mathf.Abs(child.anchoredPosition.x) + child.sizeDelta.x / 2) * scale, Is.LessThan(width / 2f), name);
                Assert.That((Mathf.Abs(child.anchoredPosition.y) + child.sizeDelta.y / 2) * scale, Is.LessThan(height / 2f), name);
            }
        }
    }
}
