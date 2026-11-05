using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WordOnline.Tests
{
    public class FactionSelectionPlayModeTests
    {
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
