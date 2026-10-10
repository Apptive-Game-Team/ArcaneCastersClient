using GameScene.Dto;
using UnityEngine;
using UnityEngine.UI;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// A world-space bar that mirrors one server gauge of its <see cref="ServedObject"/>.
    /// <para>
    /// Which gauge it shows is prefab data, not a subclass: <see cref="gaugeCategory"/> picks the
    /// category the server sends, so the HP bar and a building's TTL bar are the same component
    /// with different settings. Everything else here — the 2.5D anchor, camera billboarding and
    /// the size fit to the sprite — is gauge-agnostic and shared by both.
    /// </para>
    /// <para>
    /// A prefab may carry a bar the server sends no gauge for, so a bar stays hidden until its
    /// category arrives. See <see cref="ApplyDataVisibility"/>.
    /// </para>
    /// </summary>
    public class ServedObjectGaugeBar : MonoBehaviour
    {
        private const string LeftPlayer = "LeftPlayer";
        private const string RightPlayer = "RightPlayer";
        private const string HpCategory = "HP";
        private const float FallbackCanvasWidth = 100f;
        private const float FallbackCanvasHeight = 20f;
        private static readonly Color LeftHpFillColor = new Color(0.92f, 0.24f, 0.24f, 1f);
        private static readonly Color RightHpFillColor = new Color(0.25f, 0.55f, 0.98f, 1f);
        private static readonly Color NeutralHpFillColor = new Color(0.46f, 0.92f, 0.47f, 1f);
        private static readonly Color DefaultBackgroundColor = new Color(0f, 0f, 0f, 0.45f);

        [SerializeField] private ServedObject servedObject;
        [SerializeField] private float verticalOffset = 0.15f;
        [SerializeField] private float widthMultiplier = 0.85f;
        [SerializeField] private Vector2 worldWidthRange = new Vector2(0.65f, 1.15f);

        /// <summary>Which server gauge this bar tracks — "HP" for health, "TTL" for remaining lifetime.</summary>
        [SerializeField] private string gaugeCategory = HpCategory;

        /// <summary>
        /// Off for anything but the HP bar. A TTL bar keeps the colours authored on its prefab and
        /// leaves the team indicator to the HP bar, so the two never fight over it.
        /// </summary>
        [SerializeField] private bool useTeamColors = true;
        private Slider slider;
        private RectTransform canvasRectTransform;
        private Image fillImage;
        private Image backgroundImage;
        private Image objectIndicatorImage;
        private bool colorsApplied;
        private string appliedMaster;
        private bool subscribedToGaugeChanges;
        private Canvas canvas;
        private bool gaugeReceived;
        private bool hasResolvedServedObject;

        private void Awake()
        {
            slider = GetComponentInChildren<Slider>();
            canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                canvasRectTransform = canvas.GetComponent<RectTransform>();
                canvas.worldCamera = GameScene.Object.PresentationWorld.CameraFor(this);
            }

            ApplyDataVisibility();

            if (slider == null)
            {
                return;
            }

            if (slider.fillRect != null)
            {
                fillImage = slider.fillRect.GetComponent<Image>();
            }

            backgroundImage = FindBackgroundImage();
            objectIndicatorImage = FindImageByName("ObjectIndicator");
        }

        private void OnEnable()
        {
            SubscribeToGaugeChanges();
        }

        private void OnDisable()
        {
            UnsubscribeFromGaugeChanges();
        }

        private void Start()
        {
            ResolveServedObject();
            SubscribeToGaugeChanges();
            ApplyExistingGauge();
            ApplyDataVisibility();
            NormalizeTransform();
        }
    
        private void Update()
        {
            if (!useTeamColors || slider == null || !ResolveServedObject())
            {
                return;
            }

            string currentMaster = servedObject.GetMaster();
            if (!colorsApplied || appliedMaster != currentMaster)
            {
                ApplyTeamColors();
            }
        }

        private void LateUpdate()
        {
            if (!ResolveServedObject())
            {
                return;
            }

            NormalizeTransform();
        }

        private void OnTransformParentChanged()
        {
            hasResolvedServedObject = false;
        }

        private bool ResolveServedObject()
        {
            if (servedObject != null)
            {
                return true;
            }

            if (hasResolvedServedObject)
            {
                return false;
            }

            servedObject = GetComponentInParent<ServedObject>();
            hasResolvedServedObject = true;
            return servedObject != null;
        }

        private void SubscribeToGaugeChanges()
        {
            if (subscribedToGaugeChanges || !ResolveServedObject())
            {
                return;
            }

            servedObject.OnGaugeChanged += OnGaugeUpdated;
            subscribedToGaugeChanges = true;
        }

        private void UnsubscribeFromGaugeChanges()
        {
            if (!subscribedToGaugeChanges || servedObject == null)
            {
                return;
            }

            servedObject.OnGaugeChanged -= OnGaugeUpdated;
            subscribedToGaugeChanges = false;
        }

        private void OnGaugeUpdated(Gauge gauge)
        {
            if (slider == null || !gauge.category.Equals(gaugeCategory))
            {
                return;
            }

            slider.maxValue = gauge.maxValue;
            slider.value = gauge.value;
            gaugeReceived = true;
            ApplyDataVisibility();
        }

        /// <summary>
        /// Keeps a bar hidden until the server has sent a gauge of its category at least once.
        /// A category the object has no component for never arrives — TTL on an object with no
        /// lifetime, for instance — and the slider would otherwise show the value the prefab was
        /// authored with, a bar that is filled to some fraction and never moves.
        /// <para>
        /// The bar that carries the team indicator is exempt. It is the HP bar, and hiding it
        /// would take the team colour with it, which costs more than a stale bar.
        /// </para>
        /// </summary>
        private void ApplyDataVisibility()
        {
            if (canvas == null || slider == null || useTeamColors)
            {
                return;
            }

            canvas.enabled = gaugeReceived;
        }

        private void ApplyExistingGauge()
        {
            if (servedObject == null)
            {
                return;
            }

            Gauge existingGauge = servedObject.gauges.Find(gauge => gauge.category.Equals(gaugeCategory));
            if (existingGauge != null)
            {
                OnGaugeUpdated(existingGauge);
            }
        }

        private void NormalizeTransform()
        {
            // Face the camera, not world forward: PopupBookVisualPresenter billboards world-space
            // canvases every LateUpdate, so forcing identity here fought it in undefined order.
            Quaternion facing = GetFacingRotation();
            transform.position = servedObject.GetSpeechBubbleAnchorWorldPosition(verticalOffset);
            transform.rotation = facing;

            if (canvasRectTransform == null)
            {
                return;
            }

            canvasRectTransform.position = transform.position;
            canvasRectTransform.rotation = facing;
            canvasRectTransform.localPosition = Vector3.zero;

            Vector2 canvasSize = canvasRectTransform.rect.size;
            float canvasWidth = canvasSize.x > 0f ? canvasSize.x : FallbackCanvasWidth;
            float canvasHeight = canvasSize.y > 0f ? canvasSize.y : FallbackCanvasHeight;
            float desiredWorldWidth = GetDesiredWorldWidth();
            float desiredWorldHeight = desiredWorldWidth * canvasHeight / canvasWidth;
            Vector3 parentScale = transform.lossyScale;

            canvasRectTransform.localScale = new Vector3(
                SafeDivide(desiredWorldWidth, canvasWidth * Mathf.Abs(parentScale.x)),
                SafeDivide(desiredWorldHeight, canvasHeight * Mathf.Abs(parentScale.y)),
                1f);
        }

        private float GetDesiredWorldWidth()
        {
            float objectSize = GetObjectWorldSize();
            float desiredWidth = objectSize > 0f ? objectSize * widthMultiplier : worldWidthRange.x;
            return Mathf.Clamp(desiredWidth, worldWidthRange.x, worldWidthRange.y);
        }

        private float GetObjectWorldSize()
        {
            Transform actualTransform = servedObject.GetActualTransform();
            if (actualTransform != null && actualTransform.TryGetComponent(out SpriteRenderer spriteRenderer))
            {
                Bounds bounds = spriteRenderer.bounds;
                return Mathf.Max(bounds.size.x, bounds.size.y);
            }

            SpriteRenderer fallbackRenderer = servedObject.GetComponentInChildren<SpriteRenderer>();
            if (fallbackRenderer == null)
            {
                return 0f;
            }

            Bounds fallbackBounds = fallbackRenderer.bounds;
            return Mathf.Max(fallbackBounds.size.x, fallbackBounds.size.y);
        }

        private Quaternion GetFacingRotation()
        {
            Camera camera = GameScene.Object.PresentationWorld.CameraFor(this);
            return camera != null ? camera.transform.rotation : Quaternion.identity;
        }

        private static float SafeDivide(float numerator, float denominator)
        {
            return denominator > Mathf.Epsilon ? numerator / denominator : 1f;
        }

        private void ApplyTeamColors()
        {
            if (servedObject == null)
            {
                return;
            }

            string currentMaster = servedObject.GetMaster();
            if (fillImage != null)
            {
                fillImage.color = GetHpFillColor(currentMaster);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = DefaultBackgroundColor;
            }

            ApplyObjectIndicator(currentMaster);
            appliedMaster = currentMaster;
            colorsApplied = true;
        }

        private Image FindBackgroundImage()
        {
            return FindImageByName("Background");
        }

        private Image FindImageByName(string imageName)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image != null && image.name == imageName)
                {
                    return image;
                }
            }

            return null;
        }

        private static Color GetHpFillColor(string master)
        {
            switch (master)
            {
                case LeftPlayer:
                    return LeftHpFillColor;
                case RightPlayer:
                    return RightHpFillColor;
                default:
                    return NeutralHpFillColor;
            }
        }

        /// <summary>True only for the bar that owns the team indicator, i.e. the HP bar.</summary>
        public bool UsesTeamColors => useTeamColors;

        public void SetObjectIndicatorMaster(string master)
        {
            if (!useTeamColors)
            {
                return;
            }

            if (objectIndicatorImage == null)
            {
                objectIndicatorImage = FindImageByName("ObjectIndicator");
            }

            ApplyObjectIndicator(master);
        }

        private void ApplyObjectIndicator(string master)
        {
            if (objectIndicatorImage == null)
            {
                return;
            }

            bool hasIndicatorColor = TryGetIndicatorColor(master, out Color indicatorColor);
            objectIndicatorImage.enabled = hasIndicatorColor;
            if (hasIndicatorColor)
            {
                objectIndicatorImage.color = indicatorColor;
            }
        }

        private static bool TryGetIndicatorColor(string master, out Color indicatorColor)
        {
            switch (master)
            {
                case LeftPlayer:
                    indicatorColor = LeftHpFillColor;
                    return true;
                case RightPlayer:
                    indicatorColor = RightHpFillColor;
                    return true;
                default:
                    indicatorColor = Color.clear;
                    return false;
            }
        }
    }
}
