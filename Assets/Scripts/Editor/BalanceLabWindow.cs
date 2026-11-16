using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevPlayground;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BalanceLabWindow : EditorWindow {
    public const string ScenePath = "Assets/DevPlayground/Generated/BalanceLab.unity";
    private const string Prefix = "ArcaneCasters.BalanceLab.";
    internal static int Port => EditorPrefs.GetInt(Prefix + "Port", 6211);
    internal static long Seed => EditorPrefs.GetInt(Prefix + "Seed", 0);
    internal static bool Automatic => EditorPrefs.GetBool(Prefix + "Automatic", false);
    internal static Dictionary<string, double> Overrides => JsonConvert.DeserializeObject<Dictionary<string, double>>(
        EditorPrefs.GetString(Prefix + "Overrides", "{}"));

    [MenuItem("Tools/ArcaneCasters/Balance Test")]
    public static void Open() => GetWindow<BalanceLabWindow>("Balance Test");

    private void OnGUI() {
        EditorGUILayout.HelpBox("로컬 Rust 실행기를 먼저 시작하세요. 실제 전장에서 왼쪽 진영을 조작합니다. 초기 배치 병력은 무료이며 덱·손패는 사용하지 않습니다.", MessageType.Info);
        EditorPrefs.SetInt(Prefix + "Port", Mathf.Clamp(EditorGUILayout.IntField("서버 포트", Port), 1024, 65535));
        EditorPrefs.SetInt(Prefix + "Seed", EditorGUILayout.IntField("난수 seed", (int)Seed));
        EditorPrefs.SetBool(Prefix + "Automatic", EditorGUILayout.Toggle("예약 시전 관전", Automatic));
        EditorGUILayout.LabelField("추가 수정값 JSON · 수정안 모드에서만 적용");
        string text = EditorGUILayout.TextArea(EditorPrefs.GetString(Prefix + "Overrides", "{}"), GUILayout.MinHeight(70));
        EditorPrefs.SetString(Prefix + "Overrides", text);
        bool valid;
        try { valid = Overrides != null; } catch { valid = false; }
        if (!valid) EditorGUILayout.HelpBox("예: {\"tree_golem.heal_amount\":20}", MessageType.Error);
        EditorGUILayout.HelpBox("설정 변경은 다음 재시작에 적용합니다. 시나리오와 기존값/수정안은 Game View의 버튼으로 선택합니다.", MessageType.None);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || !valid)) {
            if (GUILayout.Button("테스트 전장 열기")) {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Prepare();
                EditorSceneManager.OpenScene(ScenePath);
                EditorApplication.isPlaying = true;
            }
        }
        using (new EditorGUI.DisabledScope(BalanceController.Active == null)) {
            if (GUILayout.Button("현재 전투 기록 저장")) BalanceController.Active.Export();
            if (GUILayout.Button("저장한 전투 재생")) {
                string path = EditorUtility.OpenFilePanel("전투 기록 선택", "", "json");
                if (!string.IsNullOrEmpty(path)) {
                    try { BalanceController.Active.Replay(File.ReadAllText(path)); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }
        }
    }

    public static void Prepare() {
        PlaygroundSceneBuilder.Generate();
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene(PlaygroundSceneBuilder.ScenePath, OpenSceneMode.Additive);
        try {
            var host = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PlaygroundHost>(true)).Single();
            host.balanceMode = true;
            host.statusText.text = "로컬 밸런스 실행기에 연결합니다.";
            host.targetText.text = "마법 선택 후 전장을 클릭하세요.";
            host.timerText.text = "마나";
            host.sideDropdown.ClearOptions();
            host.sideDropdown.AddOptions(new System.Collections.Generic.List<string> { "시나리오 불러오기" });
            SetLabel(host.clearAllyButton, "기존값 / 수정안");
            SetLabel(host.clearEnemyButton, "다음 수정안");
            SetLabel(host.clearAllButton, "재시작 / 다시 연결");
            SetLabel(host.immuneAllyButton, "시작 / 일시정지");
            SetLabel(host.immuneEnemyButton, "배속: 1×");
            SetLabel(host.closeButton, "테스트 종료");
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Cannot save balance scene.");
            PlaygroundBuildGuard.Validate();
        } finally {
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    internal static void SetLabel(UnityEngine.UI.Button button, string text) {
        var label = button.GetComponentInChildren<TMP_Text>(true);
        label.text = text;
        if (!EditorApplication.isPlaying) PrefabUtility.RecordPrefabInstancePropertyModifications(label);
    }
}
