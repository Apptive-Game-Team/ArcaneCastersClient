using System;
using System.Linq;
using LobbyScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PracticeDifficultyOverlaySetup
{
    private const string ScenePath = "Assets/Scenes/LobbyScene.unity";
    private const string HostName = "PracticeDifficultyOverlay";

    [MenuItem("Tools/Lobby/Upgrade Practice Difficulty Overlay")]
    public static void Upgrade()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode에서는 연습경기 화면을 저장할 수 없습니다.");

        var scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty)
                throw new InvalidOperationException("LobbyScene의 기존 변경을 먼저 저장한 뒤 실행하세요.");

            var panel = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PracticeDifficultyPanel>(true)).Single();
            var rect = (RectTransform)panel.transform;
            var originalCanvas = panel.GetComponentInParent<Canvas>();
            var sourceScaler = originalCanvas.GetComponent<CanvasScaler>();
            var host = scene.GetRootGameObjects().SingleOrDefault(root => root.name == HostName);
            if (host == null)
            {
                host = new GameObject(HostName, typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(host, scene);
            }
            host.layer = panel.gameObject.layer;
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 22;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = sourceScaler.uiScaleMode;
            scaler.referenceResolution = sourceScaler.referenceResolution;
            scaler.screenMatchMode = sourceScaler.screenMatchMode;
            scaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;

            var backdropTransform = host.transform.Find("Backdrop");
            GameObject backdrop;
            if (backdropTransform == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/UI-Base.prefab");
                backdrop = (GameObject)PrefabUtility.InstantiatePrefab(source, host.transform);
                backdrop.name = "Backdrop";
            }
            else backdrop = backdropTransform.gameObject;
            var backdropRect = (RectTransform)backdrop.transform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.pivot = Vector2.one * 0.5f;
            backdropRect.anchoredPosition = Vector2.zero;
            backdropRect.sizeDelta = Vector2.zero;
            backdropRect.localScale = Vector3.one;
            backdropRect.SetAsFirstSibling();
            var image = backdrop.GetComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color32(10, 18, 30, 158);
            image.raycastTarget = true;
            foreach (var effect in backdrop.GetComponents<BaseMeshEffect>()) effect.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(backdropRect);
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            foreach (var effect in backdrop.GetComponents<BaseMeshEffect>())
                PrefabUtility.RecordPrefabInstancePropertyModifications(effect);

            // 기존 패널과 버튼 참조를 유지한 채 동일한 화면 비율의 Canvas로 옮긴다.
            var anchorMin = rect.anchorMin;
            var anchorMax = rect.anchorMax;
            var pivot = rect.pivot;
            var position = rect.anchoredPosition;
            var size = rect.sizeDelta;
            var scale = rect.localScale;
            rect.SetParent(host.transform, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = scale;
            rect.SetAsLastSibling();
            var serializedPanel = new SerializedObject(panel);
            serializedPanel.FindProperty("backdrop").objectReferenceValue = backdrop;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            backdrop.SetActive(panel.gameObject.activeSelf);
            PrefabUtility.RecordPrefabInstancePropertyModifications(backdrop);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("LobbyScene 저장에 실패했습니다.");
        }
        finally
        {
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
