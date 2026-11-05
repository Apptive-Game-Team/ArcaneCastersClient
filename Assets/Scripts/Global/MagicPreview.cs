using System;
using System.Collections.Generic;
using Data.Magic;
using GameScene.Dto;
using GameScene.Dto.Event;
using GameScene.Object;
using Global.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Global
{
    /// <summary>Recorded DTO source for the same prefab/frame presentation used in live games.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class MagicPreview : MonoBehaviour
    {
        [SerializeField] private RawImage output;
        [SerializeField] private Material groundMaterial;
#if UNITY_EDITOR
        // Editor tests inject fixtures. No recordings or asset references are shipped in the player.
        private PreviewClip[] clips;
#endif
        [SerializeField] private TMP_Text scenarioCaption;
        private string magicId;
        private string recordingJson;
        private Recording recording;
        private Scenario[] scenarios;
        private int scenarioIndex;
        private Scenario scenario => scenarios[scenarioIndex];
        public string CurrentScenarioId => scenarios == null ? null : scenario.id;
        public int ScenarioCount => scenarios?.Length ?? 0;
        private GameObject stage;
        private PresentationWorld world;
        private Camera previewCamera;
        private RenderTexture texture;
        private float elapsed;
        private int nextFrame;
        private float viewZoom = 1f;
        private HashSet<int> fixtureTargetIds;
        private static readonly HashSet<int> stageSlots = new();
        private int stageSlot = -1;

        public void SetViewZoom(float zoom) => viewZoom = Mathf.Clamp(zoom, 1f, 2f);
        public bool Supports(CombinedMagicData magic) => TryGetRecording(magic?.serverName, out _);
        public bool Configure(CombinedMagicData magic)
        {
            ReleaseStage();
            magicId = magic?.serverName;
            TryGetRecording(magicId, out recordingJson);
            recording = null;
            scenarios = null;
            if (isActiveAndEnabled) OnEnable();
            return recordingJson != null;
        }

        private bool TryGetRecording(string name, out string json)
        {
            json = null;
            if (string.IsNullOrWhiteSpace(name)) return false;
#if UNITY_EDITOR
            if (clips != null)
            foreach (PreviewClip candidate in clips)
                if (candidate != null && candidate.recordingAsset != null && string.Equals(candidate.magicId, name, StringComparison.OrdinalIgnoreCase)) {
                    json = candidate.recordingAsset.text;
                    return true;
                }
#endif
            return MagicPreviewDataSource.TryGet(name, out json);
        }

        private void OnEnable()
        {
            if (output == null || recordingJson == null) return;
            try
            {
                recording ??= JsonCodec.Deserialize<Recording>(recordingJson);
                if (recording == null || (recording.version != 1 && recording.version != 2) ||
                    !string.Equals(recording.magic, magicId, StringComparison.OrdinalIgnoreCase) ||
                    recording.frameDuration <= 0f || float.IsNaN(recording.frameDuration) || float.IsInfinity(recording.frameDuration) || groundMaterial == null)
                    throw new InvalidOperationException("Unsupported recording.");
                scenarios = recording.version == 1
                    ? new[] { new Scenario { id = "default", labelKo = "마법 시전", labelEn = "Spell cast", duration = recording.duration, frames = recording.frames } }
                    : recording.scenarios;
                if (scenarios == null || scenarios.Length == 0) throw new InvalidOperationException("No preview scenarios.");
                Validate();
                scenarioIndex = 0;
                CreateStage();
                ResetPlayback();
            }
            catch (Exception exception) { Unavailable(exception); }
        }

        private void Validate()
        {
            var checkedObjects = new HashSet<string>();
            var checkedProjectiles = new HashSet<string>();
            foreach (Scenario item in scenarios)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id) || item.duration <= 0f || float.IsNaN(item.duration) ||
                    float.IsInfinity(item.duration) || item.frames == null || item.frames.Length == 0) throw new InvalidOperationException("Invalid scenario.");
                HashSet<int> targets = Targets(item);
                float previous = -1f;
                foreach (Frame frame in item.frames)
                {
                    if (frame == null || frame.time < previous || frame.time < 0f || frame.time >= item.duration || float.IsNaN(frame.time) || frame.objects == null)
                        throw new InvalidOperationException("Invalid frame order/payload.");
                    previous = frame.time;
                    foreach (CreatedObjectDto created in frame.objects.create ?? Array.Empty<CreatedObjectDto>())
                    {
                        string type = TargetType(created, targets);
                        if (checkedObjects.Add(type) && ObjectSpawner.GetPrefab(type) == null) throw new InvalidOperationException($"Missing runtime prefab: {type}");
                    }
                    foreach (var projectile in frame.objects.projectile ?? Array.Empty<GameScene.Dto.Projectile.ProjectileDto>())
                        if (checkedProjectiles.Add(projectile.type) && projectile.type != "SpiritBombBeam" && projectile.type != "ShockOverloadSecondary" &&
                            projectile.type != "BoulderStrikeImpact" && ProjectileSpawner.GetPrefab(projectile.type) == null)
                            throw new InvalidOperationException($"Missing runtime projectile: {projectile.type}");
                }
            }
        }

        private HashSet<int> Targets(Scenario item)
        {
            var ids = new HashSet<int>(item.fixtureTargetIds ?? Array.Empty<int>());
            // Only the original v1 example used this convention. Real initial
            // ElectricSlime absorbers must not be substituted in version-2 recordings.
            if (recording.version == 1)
                foreach (CreatedObjectDto created in item.frames[0].objects?.create ?? Array.Empty<CreatedObjectDto>())
                    if (created.type == "ElectricSlime") ids.Add(created.id);
            return ids;
        }

        private static string TargetType(CreatedObjectDto created, HashSet<int> ids) =>
            ids.Contains(created.id) ? (created.position.y > 0.5f ? "ThunderBird" : "MiniRock") : created.type;

        private void CreateStage()
        {
            stageSlot = 0;
            while (stageSlots.Contains(stageSlot)) stageSlot++;
            stageSlots.Add(stageSlot);
            stage = new GameObject("MagicPreviewStage") { hideFlags = HideFlags.DontSave };
            // Separate origins isolate concurrent previews, without global layers/cameras.
            stage.transform.position = new Vector3(10000f + 100f * stageSlot, 10000f, 10000f);
            world = stage.AddComponent<PresentationWorld>();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "ForestGround";
            ground.transform.SetParent(stage.transform, false);
            ground.transform.localPosition = new Vector3(6f, -0.1f, 5f);
            ground.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = new Vector3(20f, 20f, 1f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
            Destroy(ground.GetComponent<Collider>());
            var cameraObject = new GameObject("PreviewCamera");
            cameraObject.transform.SetParent(stage.transform, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.orthographic = false;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 45f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color32(0x22, 0x42, 0x5F, 0xFF);
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            previewCamera.transparencySortMode = UnityEngine.TransparencySortMode.CustomAxis;
            previewCamera.transparencySortAxis = new Vector3(0f, 1f, 2f);
            previewCamera.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            previewCamera.transform.localPosition = new Vector3(6f, 12f, -6f);
            world.Camera = previewCamera;
            texture = new RenderTexture(512, 420, 16) { name = "MagicPreview", hideFlags = HideFlags.DontSave };
            texture.Create();
            previewCamera.targetTexture = texture;
            output.texture = texture;
            output.uvRect = new Rect(0f, 0f, 1f, 1f);
            output.raycastTarget = false;
        }

        private void Update() => AdvancePlayback(Time.deltaTime);
        private void AdvancePlayback(float deltaTime)
        {
            if (stage == null) return;
            try
            {
                // Native tweens/coroutines and frames share scaled Unity time at 1x.
                // Restart after a hitch instead of firing all overdue attacks together.
                if (deltaTime > 0.25f) { ResetPlayback(); return; }
                elapsed += deltaTime;
                if (elapsed >= scenario.duration) { scenarioIndex = (scenarioIndex + 1) % scenarios.Length; ResetPlayback(); }
                while (nextFrame < scenario.frames.Length && scenario.frames[nextFrame].time <= elapsed) Apply(scenario.frames[nextFrame++]);
            }
            catch (Exception exception) { Unavailable(exception); }
        }

        private void Apply(Frame frame)
        {
            ObjectsInfo objects = frame.objects;
            if (objects.create != null)
            {
                var created = new CreatedObjectDto[objects.create.Length];
                for (int i = 0; i < created.Length; i++)
                {
                    CreatedObjectDto source = objects.create[i];
                    created[i] = new CreatedObjectDto { id = source.id, master = source.master, type = TargetType(source, fixtureTargetIds), position = source.position, gizmos = source.gizmos };
                }
                objects = new ObjectsInfo { create = created, update = objects.update, projectile = objects.projectile };
            }
            PresentationFramePlayer.Apply(objects, frame.events, world);
        }

        private void ResetPlayback()
        {
            world.Clear();
            world.SetParameters(scenario.parameters);
            world.DamageFlashInterval = magicId == "sand_storm" ? 1f : 0f;
            fixtureTargetIds = Targets(scenario);
            elapsed = 0f;
            nextFrame = 0;
            if (scenarioCaption != null)
            {
                bool korean = LocalizationSettings.SelectedLocale == null || LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", StringComparison.OrdinalIgnoreCase);
                scenarioCaption.text = $"{scenarioIndex + 1}/{scenarios.Length}  {(korean ? scenario.labelKo : scenario.labelEn)}";
            }
            while (nextFrame < scenario.frames.Length && scenario.frames[nextFrame].time <= 0f) Apply(scenario.frames[nextFrame++]);
        }

        private void LateUpdate()
        {
            if (previewCamera == null) return;
            Rect rect = output.rectTransform.rect;
            previewCamera.aspect = rect.height > 0f ? Mathf.Max(0.5f, rect.width / rect.height) : 16f / 9f;
            float halfHeight = Mathf.Max(3.5f, 6.5f / previewCamera.aspect) / viewZoom;
            previewCamera.fieldOfView = 2f * Mathf.Atan(halfHeight / (12f * Mathf.Sqrt(2f))) * Mathf.Rad2Deg;
            previewCamera.transform.localPosition = new Vector3(5.5f, 12f, -6f) + previewCamera.transform.up * (viewZoom > 1f ? 0.55f : 0f);
            previewCamera.Render();
        }

        private void Unavailable(Exception exception)
        {
            Debug.LogWarning($"Magic preview unavailable ({magicId}): {exception.Message}");
            ReleaseStage();
            gameObject.SetActive(false);
        }
        private void OnDisable() => ReleaseStage();
        private void OnDestroy() => ReleaseStage();
        private void ReleaseStage()
        {
            if (world != null) world.Clear();
            if (output != null) output.texture = null;
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (stage != null) { stage.SetActive(false); Destroy(stage); }
            if (stageSlot >= 0) stageSlots.Remove(stageSlot);
            stageSlot = -1;
            stage = null;
            world = null;
            texture = null;
            previewCamera = null;
        }

#if UNITY_EDITOR
        [Serializable] private sealed class PreviewClip { public string magicId; public TextAsset recordingAsset; }
#endif
        [Serializable, Preserve] private sealed class Recording { [Preserve] public Recording() { } public int version; public string magic; public float frameDuration, duration; public Frame[] frames; public Scenario[] scenarios; }
        [Serializable, Preserve] private sealed class Scenario { [Preserve] public Scenario() { } public string id, labelKo, labelEn; public float duration; public Frame[] frames; public int[] fixtureTargetIds; public Dictionary<string, float> parameters; }
        [Serializable, Preserve] private sealed class Frame { [Preserve] public Frame() { } public float time; public ObjectsInfo objects; public List<GameEvent> events; }
    }
}
