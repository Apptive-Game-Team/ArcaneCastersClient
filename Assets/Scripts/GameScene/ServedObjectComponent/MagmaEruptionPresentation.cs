using GameScene.Dto;
using GameScene.ServedObjectComponent.Sound;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public class MagmaEruptionPresentation : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer eruptionRenderer;
        [SerializeField] private Sprite[] eruptionFrames;
        [SerializeField] private float burstDuration = 0.65f;

        private GameObject warning;
        private Material material;
        private Mesh mound;
        private Vector3[] moundVertices;
        private LineRenderer rim;
        private readonly LineRenderer[] cracks = new LineRenderer[8];
        private readonly LineRenderer[] embers = new LineRenderer[12];
        private float progress;
        private float radius = 1f;
        private bool erupted;

        protected override void OnBound()
        {
            foreach (PopSelfController pop in GetComponentsInChildren<PopSelfController>(true))
            {
                pop.enabled = false;
                pop.transform.localScale = Vector3.one;
            }
            foreach (SpriteFrameAnimator animator in GetComponentsInChildren<SpriteFrameAnimator>(true))
                animator.enabled = false;
            eruptionRenderer = ResolveRenderer(eruptionRenderer);
            if (eruptionRenderer != null) eruptionRenderer.enabled = false;
            Owner.TryGetGizmoRadius("AreaOfEffect", out radius);
            if (radius <= 0f) radius = 1f;
            material = new Material(Shader.Find("Sprites/Default"));
            warning = new GameObject("MagmaWarning");
            warning.transform.SetParent(Owner.transform, false);
            CreateMound();
            rim = MakeLine("RaisedGround", 49, new Color(0.24f, 0.12f, 0.08f), 0.16f);
            for (int i = 0; i < cracks.Length; i++)
                cracks[i] = MakeLine("Crack", 5, new Color(1f, 0.3f, 0.04f), 0.025f);
            for (int i = 0; i < embers.Length; i++)
                embers[i] = MakeLine("RisingEmber", 2, new Color(1f, 0.65f, 0.12f), 0.025f);
            Owner.OnGaugeChanged += HandleGauge;
            Owner.OnDestroyed += Erupt;
            foreach (Gauge gauge in Owner.gauges) HandleGauge(gauge);
            DrawWarning();
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnGaugeChanged -= HandleGauge;
                Owner.OnDestroyed -= Erupt;
            }
            Release(warning);
            Release(material);
            Release(mound);
        }

        private static void Release(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        private void CreateMound()
        {
            GameObject ground = new GameObject("SwellingGround");
            ground.transform.SetParent(warning.transform, false);
            mound = new Mesh { name = "MagmaGround" };
            moundVertices = new Vector3[49];
            int[] triangles = new int[16 * 3 + 2 * 16 * 6];
            int next = 0;
            for (int i = 0; i < 16; i++)
            {
                triangles[next++] = 0;
                triangles[next++] = 1 + (i + 1) % 16;
                triangles[next++] = 1 + i;
            }
            for (int ring = 0; ring < 2; ring++)
            for (int i = 0; i < 16; i++)
            {
                int a = 1 + ring * 16 + i;
                int b = 1 + ring * 16 + (i + 1) % 16;
                triangles[next++] = a; triangles[next++] = b; triangles[next++] = a + 16;
                triangles[next++] = b; triangles[next++] = b + 16; triangles[next++] = a + 16;
            }
            mound.vertices = moundVertices;
            mound.triangles = triangles;
            ground.AddComponent<MeshFilter>().sharedMesh = mound;
            ground.AddComponent<MeshRenderer>().sharedMaterial = material;
            Color[] colors = new Color[moundVertices.Length];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.Lerp(new Color(0.16f, 0.11f, 0.08f), new Color(0.28f, 0.19f, 0.12f), (i % 3) * 0.5f);
            mound.colors = colors;
        }

        private void HandleGauge(Gauge gauge)
        {
            if (gauge.category != "TTL" || gauge.maxValue <= 0f) return;
            // 복구 스냅샷과 일시정지도 서버의 남은 시간을 그대로 따른다.
            progress = Mathf.Clamp01(1f - gauge.value / gauge.maxValue);
            DrawWarning();
        }

        private LineRenderer MakeLine(string lineName, int count, Color color, float width)
        {
            GameObject child = new GameObject(lineName);
            child.transform.SetParent(warning.transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.positionCount = count;
            line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = width * radius;
            line.numCapVertices = 3;
            line.sortingOrder = 2;
            return line;
        }

        private void LateUpdate()
        {
            if (warning != null && !erupted) DrawWarning();
        }

        private void DrawWarning()
        {
            if (warning == null) return;
            warning.transform.position = Owner.transform.position + Vector3.up * 0.025f;
            warning.transform.rotation = Quaternion.identity;
            Vector3 scale = Owner.transform.lossyScale;
            warning.transform.localScale = new Vector3(
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                1f / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            float lift = radius * Mathf.Lerp(0.03f, 0.32f, progress);
            moundVertices[0] = Vector3.up * lift;
            for (int ring = 0; ring < 3; ring++)
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16;
                float distance = radius * (ring + 1) / 3f;
                moundVertices[1 + ring * 16 + i] = new Vector3(Mathf.Cos(angle) * distance,
                    lift * (1f - distance / radius), Mathf.Sin(angle) * distance);
            }
            mound.vertices = moundVertices;
            mound.RecalculateBounds();
            for (int j = 0; j < rim.positionCount; j++)
            {
                float angle = j * Mathf.PI * 2f / (rim.positionCount - 1);
                rim.SetPosition(j, new Vector3(Mathf.Cos(angle) * radius, lift * 0.2f, Mathf.Sin(angle) * radius));
            }
            for (int i = 0; i < cracks.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / cracks.Length;
                cracks[i].startWidth = cracks[i].endWidth = radius * Mathf.Lerp(0.018f, 0.065f, progress);
                cracks[i].startColor = cracks[i].endColor = Color.Lerp(
                    new Color(0.7f, 0.13f, 0.02f), new Color(1f, 0.8f, 0.18f), progress);
                for (int j = 0; j < 5; j++)
                {
                    float distance = radius * (0.08f + j * 0.21f);
                    float bend = angle + (j % 2 == 0 ? 0.10f : -0.10f);
                    cracks[i].SetPosition(j, new Vector3(Mathf.Cos(bend) * distance,
                        lift * (1f - distance / radius) + 0.01f, Mathf.Sin(bend) * distance));
                }
            }
            for (int i = 0; i < embers.Length; i++)
            {
                float phase = Mathf.Repeat(Time.time * (0.5f + progress) + i * 0.23f, 1f);
                float angle = i * 2.399963f;
                float spread = radius * (0.15f + (i % 4) * 0.18f);
                Vector3 position = new Vector3(Mathf.Cos(angle) * spread,
                    lift + phase * radius * (0.15f + progress * 0.7f), Mathf.Sin(angle) * spread);
                Color color = new Color(1f, 0.45f + progress * 0.35f, 0.08f, (1f - phase) * (0.2f + progress * 0.8f));
                embers[i].startColor = embers[i].endColor = color;
                embers[i].SetPosition(0, position);
                embers[i].SetPosition(1, position + Vector3.up * radius * 0.06f);
            }
        }

        private void Erupt()
        {
            if (erupted || eruptionRenderer == null) return;
            erupted = true;
            warning.SetActive(false);
            GameObject burst = new GameObject("MagmaEruptionBurst");
            burst.transform.position = eruptionRenderer.transform.position;
            burst.transform.rotation = eruptionRenderer.transform.rotation;
            burst.transform.localScale = eruptionRenderer.transform.lossyScale;
            SpriteRenderer renderer = burst.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = eruptionRenderer.sharedMaterial;
            renderer.sortingLayerID = eruptionRenderer.sortingLayerID;
            renderer.sortingOrder = eruptionRenderer.sortingOrder;
            renderer.sprite = eruptionFrames != null && eruptionFrames.Length > 0 ? eruptionFrames[0] : eruptionRenderer.sprite;
            burst.AddComponent<MagmaEruptionBurst>().Initialize(renderer, eruptionFrames, burstDuration);
            if (Application.isPlaying && Owner.PresentationWorld == null)
                ServedObjectSfxController.PlaySpawnForType("MagmaExplosion");
        }
    }

    public class MagmaEruptionBurst : MonoBehaviour
    {
        private SpriteRenderer rendererComponent;
        private Sprite[] frames;
        private float duration;
        private float elapsed;
        private Vector3 scale;

        public void Initialize(SpriteRenderer target, Sprite[] sprites, float lifetime)
        {
            rendererComponent = target;
            frames = sprites;
            duration = Mathf.Max(0.1f, lifetime);
            scale = transform.localScale;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (frames != null && frames.Length > 0)
                rendererComponent.sprite = frames[Mathf.Min(frames.Length - 1, (int)(t * frames.Length))];
            transform.localScale = scale * Mathf.Lerp(0.55f, 1.1f, Mathf.Sin(t * Mathf.PI * 0.5f));
            rendererComponent.color = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0.65f, 1f, t));
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
