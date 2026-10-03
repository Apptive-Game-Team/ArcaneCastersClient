using System;
using System.Collections.Generic;
using Data.Magic;
using Global.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Global
{
    /// <summary>
    /// Replays bundled server recordings with visual-only objects. It never simulates
    /// a spell or registers objects with GameScene.
    /// </summary>
    public sealed class MagicPreview : MonoBehaviour
    {
        [SerializeField] private RawImage output;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private PreviewClip[] clips;
        [SerializeField] private TMP_Text scenarioCaption;
        [SerializeField] private VisualStyle[] sharedVisuals;
        [SerializeField] private VisualStyle[] projectileVisuals;
        [SerializeField] private VisualStyle[] effectVisuals;

        private PreviewClip clip;
        private Recording recording;
        private Scenario[] scenarios;
        private int scenarioIndex;
        private Scenario scenario => scenarios[scenarioIndex];
        public string CurrentScenarioId => scenarios == null || scenarioIndex >= scenarios.Length ? null : scenario.id;
        public int ScenarioCount => scenarios?.Length ?? 0;
        private GameObject stage;
        private Camera previewCamera;
        private RenderTexture texture;
        private readonly Dictionary<int, Visual> visuals = new();
        private SpriteRenderer impact;
        private float impactStarted = -10f;
        private float elapsed;
        private int nextFrame;
        private readonly HashSet<string> missingVisuals = new();
        private readonly List<Transient> transients = new();
        private Sprite gaugeSprite;

        public bool Supports(CombinedMagicData magic) => FindClip(magic?.serverName) != null;

        public bool Configure(CombinedMagicData magic)
        {
            ReleaseStage();
            clip = FindClip(magic?.serverName);
            recording = null;
            scenarios = null;
            missingVisuals.Clear();
            if (isActiveAndEnabled) OnEnable();
            return clip != null;
        }

        private PreviewClip FindClip(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName) || clips == null) return null;
            foreach (PreviewClip candidate in clips)
            {
                if (candidate != null && candidate.recordingAsset != null &&
                    string.Equals(candidate.magicId, serverName, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
            return null;
        }

        private void OnEnable()
        {
            if (output == null || clip == null) return;
            try
            {
                recording ??= JsonCodec.Deserialize<Recording>(clip.recordingAsset.text);
                if (recording == null || (recording.version != 1 && recording.version != 2) ||
                    !string.Equals(recording.magic, clip.magicId, StringComparison.OrdinalIgnoreCase) ||
                    recording.frameDuration <= 0f || float.IsNaN(recording.frameDuration) ||
                    float.IsInfinity(recording.frameDuration) || groundMaterial == null)
                    throw new InvalidOperationException("Unsupported magic preview recording or visual mapping.");
                scenarios = recording.version == 1
                    ? new[] { new Scenario { id = "default", labelKo = "마법 시전", labelEn = "Spell cast", duration = recording.duration, frames = recording.frames } }
                    : recording.scenarios;
                if (scenarios == null || scenarios.Length == 0) throw new InvalidOperationException("No preview scenarios.");
                foreach (Scenario item in scenarios)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.id) || item.duration <= 0f ||
                        float.IsNaN(item.duration) || float.IsInfinity(item.duration) || item.frames == null || item.frames.Length == 0)
                        throw new InvalidOperationException("Invalid preview scenario.");
                    float previous = -1f;
                    foreach (Frame frame in item.frames)
                    {
                        if (frame == null || frame.time < previous || frame.time < 0f || frame.time >= item.duration || float.IsNaN(frame.time))
                            throw new InvalidOperationException("Invalid preview frame order.");
                        previous = frame.time;
                    }
                }
                scenarioIndex = 0;
                CreateStage();
                ResetPlayback();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Magic preview unavailable ({clip.magicId}): {exception.Message}");
                ReleaseStage();
                gameObject.SetActive(false);
            }
        }

        private void CreateStage()
        {
            // Far outside scene cameras. Dynamic sprites are enabled only during this
            // camera's manual render; the ground stays here for the whole replay.
            stage = new GameObject("MagicPreviewStage") { hideFlags = HideFlags.DontSave };
            stage.transform.position = new Vector3(10000f, 10000f, 10000f);
            var groundObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            groundObject.name = "ForestGround";
            groundObject.transform.SetParent(stage.transform, false);
            groundObject.transform.localPosition = new Vector3(6f, -0.1f, 5f);
            // Unity's built-in Quad faces local -Z, so +90 degrees faces it upward.
            groundObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            groundObject.transform.localScale = new Vector3(20f, 20f, 1f);
            groundObject.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;
            Destroy(groundObject.GetComponent<Collider>());
            var cameraObject = new GameObject("PreviewCamera");
            cameraObject.transform.SetParent(stage.transform, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 4f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 40f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color32(0x22, 0x42, 0x5F, 0xFF);
            previewCamera.allowHDR = false;
            previewCamera.allowMSAA = false;
            previewCamera.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            previewCamera.transform.localPosition = new Vector3(6f, 12f, -6f);
            texture = new RenderTexture(512, 288, 16) { name = "MagicPreview", hideFlags = HideFlags.DontSave };
            texture.Create();
            previewCamera.targetTexture = texture;
            output.texture = texture;
            // The prefab's RawImage UV rect can deserialize as zero in Unity, sampling only
            // one background pixel despite a correctly rendered texture.
            output.uvRect = new Rect(0f, 0f, 1f, 1f);
            output.raycastTarget = false;
            gaugeSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0f, 0.5f), 1f);
            if (clip.impactSprite != null)
            {
                impact = CreateSprite("Impact", clip.impactSprite, 2.4f);
                impact.sortingOrder = 20;
            }
        }

        private SpriteRenderer CreateSprite(string objectName, Sprite sprite, float height)
        {
            var obj = new GameObject(objectName);
            obj.transform.SetParent(stage.transform, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.enabled = false;
            if (sprite != null)
                obj.transform.localScale = Vector3.one * (height / Mathf.Max(0.01f, sprite.bounds.size.y));
            return renderer;
        }

        private void Update() => AdvancePlayback(Time.unscaledDeltaTime);

        private void AdvancePlayback(float deltaTime)
        {
            if (stage == null) return;
            elapsed += deltaTime;
            if (elapsed >= scenario.duration)
            {
                scenarioIndex = (scenarioIndex + 1) % scenarios.Length;
                ResetPlayback();
            }
            while (nextFrame < scenario.frames.Length && scenario.frames[nextFrame].time <= elapsed)
                Apply(scenario.frames[nextFrame++]);

            foreach (var visual in visuals.Values)
            {
                float t = Mathf.Clamp01((elapsed - visual.movedAt) / recording.frameDuration);
                visual.renderer.transform.localPosition = Vector3.Lerp(visual.from, visual.to, t);
                visual.renderer.sprite = visual.style.attackSprite != null && elapsed - visual.attackAt < 0.3f ? visual.style.attackSprite : visual.style.sprite;
                visual.renderer.color = elapsed - visual.hitAt < 0.2f ? new Color(1f, 0.35f, 0.2f) : elapsed - visual.healAt < 0.3f ? Color.green : Color.white;
                foreach (var effect in visual.effects.Values)
                    effect.transform.localPosition = visual.renderer.transform.localPosition + previewCamera.transform.up * (visual.style.height * 0.65f);
                if (visual.gauge != null)
                {
                    visual.gauge.transform.localPosition = visual.renderer.transform.localPosition + previewCamera.transform.up * (visual.style.height + 0.2f);
                    visual.gauge.transform.localRotation = previewCamera.transform.localRotation;
                }
                if (elapsed - visual.attackAt < 0.25f)
                    visual.renderer.transform.localPosition += Vector3.right * (visual.master == "RightPlayer" ? -1 : 1) * Mathf.Sin((elapsed - visual.attackAt) / 0.25f * Mathf.PI) * 0.18f;
            }
            for (int i = transients.Count - 1; i >= 0; i--)
            {
                Transient item = transients[i];
                float progress = (elapsed - item.started) / item.duration;
                if (progress >= 1f)
                {
                    item.renderer.enabled = false;
                    Destroy(item.renderer.gameObject);
                    transients.RemoveAt(i);
                    continue;
                }
                item.renderer.transform.localPosition = Vector3.Lerp(item.from, item.to, Mathf.Clamp01(progress));
            }
        }

        private void LateUpdate()
        {
            if (previewCamera == null) return;
            // Preserve framing in both the deck hover and book explanation layouts.
            Rect rect = output.rectTransform.rect;
            float aspect = rect.height > 0 ? rect.width / rect.height : 16f / 9f;
            previewCamera.aspect = Mathf.Max(0.5f, aspect);
            previewCamera.orthographicSize = Mathf.Max(3.5f, 5.5f / previewCamera.aspect);
            SetVisualsEnabled(true);
            float age = elapsed - impactStarted;
            if (impact != null)
            {
                impact.enabled = age >= 0f && age < 0.45f;
                impact.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - age / 0.45f));
            }
            try { previewCamera.Render(); }
            finally
            {
                SetVisualsEnabled(false);
                if (impact != null) impact.enabled = false;
            }
        }

        private void SetVisualsEnabled(bool value)
        {
            foreach (var visual in visuals.Values)
            {
                visual.renderer.enabled = value;
                foreach (var effect in visual.effects.Values) effect.enabled = value;
                if (visual.gauge != null) visual.gauge.enabled = value;
            }
            foreach (var item in transients) item.renderer.enabled = value;
        }

        private void Apply(Frame frame)
        {
            if (frame.objects == null) return;
            foreach (var item in frame.objects.create ?? Array.Empty<Created>())
            {
                if (visuals.ContainsKey(item.id)) continue;
                VisualStyle style = FindVisual(item.type);
                if (style == null)
                {
                    if (missingVisuals.Add(item.type ?? string.Empty))
                        Debug.LogWarning($"Magic preview {clip.magicId}: no visual for {item.type}.");
                    continue;
                }
                var renderer = CreateSprite(item.type, style.sprite, style.height);
                renderer.flipX = style.flipForRight && item.master == "RightPlayer";
                renderer.sortingOrder = style.sortingOrder;
                renderer.transform.localPosition = item.position;
                visuals.Add(item.id, new Visual { renderer = renderer, style = style, master = item.master, from = item.position, to = item.position });
            }
            foreach (var item in frame.objects.update ?? Array.Empty<Updated>())
            {
                if (!visuals.TryGetValue(item.id, out var visual)) continue;
                if (item.status == "Destroyed")
                {
                    if (visual.style.lingerOnDestroy > 0f)
                        AddTransient(visual.style.destroySprite != null ? visual.style.destroySprite : visual.style.sprite,
                            visual.style.height, item.position, item.position, frame.time, visual.style.lingerOnDestroy);
                    visual.renderer.enabled = false;
                    Destroy(visual.renderer.gameObject);
                    foreach (var effect in visual.effects.Values) { effect.enabled = false; Destroy(effect.gameObject); }
                    if (visual.gauge != null) { visual.gauge.enabled = false; Destroy(visual.gauge.gameObject); }
                    visuals.Remove(item.id);
                    continue;
                }
                visual.from = visual.to;
                visual.to = item.position;
                visual.movedAt = frame.time;
                if (item.status == "Damaged") visual.hitAt = frame.time;
                if (item.status == "Attack") visual.attackAt = frame.time;
                if (!string.IsNullOrEmpty(item.master)) visual.master = item.master;
                visual.renderer.flipX = visual.style.flipForRight && visual.master == "RightPlayer";
                if (item.effects != null) UpdateEffects(visual, item.effects);
                foreach (Gauge gauge in item.gauges ?? Array.Empty<Gauge>())
                {
                    if (gauge.category != "HP" || gauge.maxValue <= 0f) continue;
                    if (visual.hp >= 0f && gauge.value < visual.hp) visual.hitAt = frame.time;
                    if (visual.hp >= 0f && gauge.value > visual.hp) visual.healAt = frame.time;
                    visual.hp = gauge.value;
                    if (visual.gauge == null) visual.gauge = CreateSprite("HP", gaugeSprite, 0.1f);
                    float width = Mathf.Max(0.02f, Mathf.Clamp01(gauge.value / gauge.maxValue) * 1.3f);
                    visual.gauge.transform.localScale = new Vector3(width / gaugeSprite.bounds.size.x, 0.1f / gaugeSprite.bounds.size.y, 1f);
                    visual.gauge.sortingOrder = 40;
                    visual.gauge.color = visual.master == "None" ? new Color(0.8f, 0.3f, 1f) : visual.master == "RightPlayer" ? new Color(1f, 0.5f, 0.25f) : new Color(0.3f, 1f, 0.5f);
                }
            }
            foreach (Projection projection in frame.objects.projectile ?? Array.Empty<Projection>())
            {
                VisualStyle style = FindStyle(projectileVisuals, projection.type);
                if (style == null) { WarnMissing("projectile", projection.type); continue; }
                if (!TryEndpoint(projection.start, out Vector3 start) || !TryEndpoint(projection.end, out Vector3 end)) continue;
                AddTransient(style.sprite, style.height, start, end, frame.time, Mathf.Max(0.05f, projection.duration));
            }
            foreach (HitEvent hit in frame.events ?? Array.Empty<HitEvent>())
            {
                if (hit.type == "hit" && visuals.TryGetValue(hit.targetId, out Visual victim))
                {
                    victim.hitAt = frame.time;
                    if (clip.impactSprite != null)
                        AddTransient(clip.impactSprite, 1f, victim.to, victim.to, frame.time, 0.25f);
                }
            }
            // Explicit marker comes only from a damage-confirmed collision in the server capture.
            if (frame.impact != null)
            {
                if (impact != null)
                    impact.transform.localPosition = new Vector3(frame.impact.x, frame.impact.y, frame.impact.z);
                impactStarted = frame.time;
            }
        }

        private bool TryEndpoint(Endpoint point, out Vector3 position)
        {
            position = Vector3.zero;
            if (point == null) return false;
            if (point.targetType == "position") { position = point.position; return true; }
            if (point.targetType != "reference" || !visuals.TryGetValue(point.id, out Visual visual)) return false;
            position = visual.to + previewCamera.transform.up * (visual.style.height * 0.5f);
            return true;
        }

        private void AddTransient(Sprite sprite, float height, Vector3 from, Vector3 to, float started, float duration)
        {
            SpriteRenderer renderer = CreateSprite("PreviewEvent", sprite, height);
            renderer.sortingOrder = 30;
            renderer.transform.localPosition = from;
            transients.Add(new Transient { renderer = renderer, from = from, to = to, started = started, duration = duration });
        }

        private void UpdateEffects(Visual visual, string[] effects)
        {
            foreach (string key in new List<string>(visual.effects.Keys))
            {
                if (Array.IndexOf(effects, key) >= 0) continue;
                visual.effects[key].enabled = false;
                Destroy(visual.effects[key].gameObject);
                visual.effects.Remove(key);
            }
            foreach (string key in effects)
            {
                if (key == "None" || visual.effects.ContainsKey(key)) continue;
                VisualStyle style = FindStyle(effectVisuals, key);
                if (style == null) { WarnMissing("effect", key); continue; }
                SpriteRenderer renderer = CreateSprite(key, style.sprite, style.height);
                renderer.sortingOrder = 35;
                visual.effects.Add(key, renderer);
            }
        }

        private void WarnMissing(string category, string name)
        {
            if (missingVisuals.Add(category + ":" + name)) Debug.LogWarning($"Magic preview {clip.magicId}: no {category} visual for {name}.");
        }

        private VisualStyle FindVisual(string prefabType)
        {
            return FindStyle(clip.visuals, prefabType) ?? FindStyle(sharedVisuals, prefabType);
        }

        private static VisualStyle FindStyle(VisualStyle[] styles, string prefabType)
        {
            foreach (VisualStyle style in styles ?? Array.Empty<VisualStyle>())
            {
                if (style != null && style.sprite != null && style.height > 0f &&
                    string.Equals(style.prefabType, prefabType, StringComparison.Ordinal))
                    return style;
            }
            return null;
        }

        private void ResetPlayback()
        {
            foreach (var visual in visuals.Values)
            {
                visual.renderer.enabled = false;
                Destroy(visual.renderer.gameObject);
                foreach (var effect in visual.effects.Values) { effect.enabled = false; Destroy(effect.gameObject); }
                if (visual.gauge != null) { visual.gauge.enabled = false; Destroy(visual.gauge.gameObject); }
            }
            visuals.Clear();
            foreach (var item in transients) { item.renderer.enabled = false; Destroy(item.renderer.gameObject); }
            transients.Clear();
            elapsed = 0f;
            nextFrame = 0;
            impactStarted = -10f;
            if (impact != null) impact.enabled = false;
            if (scenarioCaption != null)
            {
                bool korean = LocalizationSettings.SelectedLocale == null || LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("ko", StringComparison.OrdinalIgnoreCase);
                scenarioCaption.text = $"{scenarioIndex + 1}/{scenarios.Length}  {(korean ? scenario.labelKo : scenario.labelEn)}";
            }
            while (nextFrame < scenario.frames.Length && scenario.frames[nextFrame].time <= 0f)
                Apply(scenario.frames[nextFrame++]);
        }

        private void OnDisable() => ReleaseStage();
        private void OnDestroy() => ReleaseStage();

        private void ReleaseStage()
        {
            if (output != null) output.texture = null;
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (stage != null) { stage.SetActive(false); Destroy(stage); }
            if (gaugeSprite != null) Destroy(gaugeSprite);
            visuals.Clear();
            transients.Clear();
            if (scenarioCaption != null) scenarioCaption.text = string.Empty;
            stage = null;
            previewCamera = null;
            texture = null;
            impact = null;
            gaugeSprite = null;
        }

        [Serializable] private sealed class PreviewClip
        {
            public string magicId;
            public TextAsset recordingAsset;
            public Sprite impactSprite;
            public VisualStyle[] visuals;
        }

        [Serializable] private sealed class VisualStyle
        {
            public string prefabType;
            public Sprite sprite;
            public Sprite attackSprite;
            public Sprite destroySprite;
            public float height;
            public int sortingOrder;
            public bool flipForRight;
            public float lingerOnDestroy;
        }

        private sealed class Visual
        {
            public SpriteRenderer renderer;
            public VisualStyle style;
            public string master;
            public SpriteRenderer gauge;
            public readonly Dictionary<string, SpriteRenderer> effects = new();
            public float hp = -1f;
            public Vector3 from, to;
            public float movedAt;
            public float hitAt = -10f;
            public float healAt = -10f, attackAt = -10f;
        }

        private sealed class Transient { public SpriteRenderer renderer; public Vector3 from, to; public float started, duration; }

        [Serializable, Preserve] private sealed class Recording
        {
            [Preserve] public Recording() { }
            public int version;
            public string magic;
            public float frameDuration, duration;
            public Frame[] frames;
            public Scenario[] scenarios;
        }
        [Serializable, Preserve] private sealed class Scenario { [Preserve] public Scenario() { } public string id, labelKo, labelEn; public float duration; public Frame[] frames; }
        [Serializable, Preserve] private sealed class Frame { [Preserve] public Frame() { } public float time; public Objects objects; public Point impact; public HitEvent[] events; }
        [Serializable, Preserve] private sealed class Objects { [Preserve] public Objects() { } public Created[] create; public Updated[] update; public Projection[] projectile; }
        [Serializable, Preserve] private sealed class Created { [Preserve] public Created() { } public int id; public string type, master; public Vector3 position; }
        [Serializable, Preserve] private sealed class Updated { [Preserve] public Updated() { } public int id; public string status, master; public Vector3 position; public string[] effects; public Gauge[] gauges; }
        [Serializable, Preserve] private sealed class Gauge { [Preserve] public Gauge() { } public float value, maxValue; public string category; }
        [Serializable, Preserve] private sealed class Projection { [Preserve] public Projection() { } public string type; public Endpoint start, end; public float duration; }
        [Serializable, Preserve] private sealed class Endpoint { [Preserve] public Endpoint() { } public string targetType; public int id; public Vector3 position; }
        [Serializable, Preserve] private sealed class HitEvent { [Preserve] public HitEvent() { } public string type; public int actorId, targetId; }
        [Serializable, Preserve] private sealed class Point { [Preserve] public Point() { } public float x, y, z; }
    }
}
