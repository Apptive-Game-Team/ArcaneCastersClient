using System;
using System.Collections.Generic;
using Data.Magic;
using Global.Serialization;
using UnityEngine;
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

        private PreviewClip clip;
        private Recording recording;
        private GameObject stage;
        private Camera previewCamera;
        private RenderTexture texture;
        private readonly Dictionary<int, Visual> visuals = new();
        private SpriteRenderer impact;
        private float impactStarted = -10f;
        private float elapsed;
        private int nextFrame;
        private readonly HashSet<string> missingVisuals = new();

        public bool Supports(CombinedMagicData magic) => FindClip(magic?.serverName) != null;

        public bool Configure(CombinedMagicData magic)
        {
            clip = FindClip(magic?.serverName);
            recording = null;
            missingVisuals.Clear();
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
                if (recording == null || recording.version != 1 ||
                    !string.Equals(recording.magic, clip.magicId, StringComparison.OrdinalIgnoreCase) ||
                    recording.frames == null || recording.frames.Length == 0 || recording.duration <= 0f ||
                    recording.frameDuration <= 0f || groundMaterial == null || clip.visuals == null)
                    throw new InvalidOperationException("Unsupported magic preview recording or visual mapping.");
                CreateStage();
                ResetPlayback();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Magic preview unavailable ({clip.magicId}): {exception.Message}");
                ReleaseStage();
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

        private void Update()
        {
            if (stage == null) return;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= recording.duration) ResetPlayback();
            while (nextFrame < recording.frames.Length && recording.frames[nextFrame].time <= elapsed)
                Apply(recording.frames[nextFrame++]);

            foreach (var visual in visuals.Values)
            {
                float t = Mathf.Clamp01((elapsed - visual.movedAt) / recording.frameDuration);
                visual.renderer.transform.localPosition = Vector3.Lerp(visual.from, visual.to, t);
                visual.renderer.color = elapsed - visual.hitAt < 0.2f ? new Color(1f, 0.35f, 0.2f) : Color.white;
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
            foreach (var visual in visuals.Values) visual.renderer.enabled = true;
            float age = elapsed - impactStarted;
            if (impact != null)
            {
                impact.enabled = age >= 0f && age < 0.45f;
                impact.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - age / 0.45f));
            }
            try { previewCamera.Render(); }
            finally
            {
                foreach (var visual in visuals.Values) visual.renderer.enabled = false;
                if (impact != null) impact.enabled = false;
            }
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
                visuals.Add(item.id, new Visual { renderer = renderer, from = item.position, to = item.position });
            }
            foreach (var item in frame.objects.update ?? Array.Empty<Updated>())
            {
                if (!visuals.TryGetValue(item.id, out var visual)) continue;
                if (item.status == "Destroyed")
                {
                    visual.renderer.enabled = false;
                    Destroy(visual.renderer.gameObject);
                    visuals.Remove(item.id);
                    continue;
                }
                visual.from = visual.to;
                visual.to = item.position;
                visual.movedAt = frame.time;
                if (item.status == "Damaged") visual.hitAt = frame.time;
            }
            // Explicit marker comes only from a damage-confirmed collision in the server capture.
            if (frame.impact != null)
            {
                if (impact != null)
                    impact.transform.localPosition = new Vector3(frame.impact.x, frame.impact.y, frame.impact.z);
                impactStarted = frame.time;
            }
        }

        private VisualStyle FindVisual(string prefabType)
        {
            foreach (VisualStyle style in clip.visuals)
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
            }
            visuals.Clear();
            elapsed = 0f;
            nextFrame = 0;
            impactStarted = -10f;
            if (impact != null) impact.enabled = false;
            while (nextFrame < recording.frames.Length && recording.frames[nextFrame].time <= 0f)
                Apply(recording.frames[nextFrame++]);
        }

        private void OnDisable() => ReleaseStage();
        private void OnDestroy() => ReleaseStage();

        private void ReleaseStage()
        {
            if (output != null) output.texture = null;
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (stage != null) { stage.SetActive(false); Destroy(stage); }
            visuals.Clear();
            stage = null;
            previewCamera = null;
            texture = null;
            impact = null;
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
            public float height;
            public int sortingOrder;
            public bool flipForRight;
        }

        private sealed class Visual
        {
            public SpriteRenderer renderer;
            public Vector3 from, to;
            public float movedAt;
            public float hitAt = -10f;
        }

        [Serializable, Preserve] private sealed class Recording
        {
            [Preserve] public Recording() { }
            public int version;
            public string magic;
            public float frameDuration, duration;
            public Frame[] frames;
        }
        [Serializable, Preserve] private sealed class Frame { [Preserve] public Frame() { } public float time; public Objects objects; public Point impact; }
        [Serializable, Preserve] private sealed class Objects { [Preserve] public Objects() { } public Created[] create; public Updated[] update; }
        [Serializable, Preserve] private sealed class Created { [Preserve] public Created() { } public int id; public string type, master; public Vector3 position; }
        [Serializable, Preserve] private sealed class Updated { [Preserve] public Updated() { } public int id; public string status; public Vector3 position; }
        [Serializable, Preserve] private sealed class Point { [Preserve] public Point() { } public float x, y, z; }
    }
}
