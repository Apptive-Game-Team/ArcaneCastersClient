using System;
using System.Collections;
using System.Collections.Generic;
using Data;
using GameScene.Dto;
using GameScene.Dto.debug;
using GameScene.Object;
using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public class ServedObject : MonoBehaviour
    {
        private const string LeftPlayer = "LeftPlayer";
        private const string RightPlayer = "RightPlayer";
        private const string TeamIndicatorResourcePath = "UI/ObjectIndicator";
        private static readonly Color LeftIndicatorColor = new Color(0.92f, 0.24f, 0.24f, 1f);
        private static readonly Color RightIndicatorColor = new Color(0.25f, 0.55f, 0.98f, 1f);
        private static Sprite _teamIndicatorSprite;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _actualTransform = null;
        /// <summary>
        /// Where aura and status effects are parented. Leave empty and they sit on the
        /// object itself, which is what every mob wants. The player points it at the staff
        /// tip anchor so the element auras gather there and follow the attack frame.
        /// </summary>
        [SerializeField] private Transform _effectAnchor = null;
        /// <summary>
        /// Names the effects that hang on <see cref="_effectAnchor"/>; every other effect stays on
        /// the object. This is why the player's <c>Burn</c>, <c>Panic</c>, <c>Snared</c> and other
        /// status effects do not fly up to the staff tip along with the element auras. An empty or
        /// null list means nothing is anchored, so an object that never set this behaves exactly as
        /// if <see cref="_effectAnchor"/> did not exist.
        /// </summary>
        [SerializeField] private string[] _effectAnchorEffects = null;

        /// <summary>
        /// Whether the attack event swings the object. Turn it off for objects whose sprite is a
        /// tall effect rather than a body: the lightning cloud's canvas reaches the ground, so a
        /// swing around the canvas centre throws the bolt sideways.
        /// </summary>
        [SerializeField] private bool _swingOnAttack = true;
        [SerializeField] private float _teamIndicatorVerticalOffset = 0.1f;
        [SerializeField] private float _teamIndicatorScale = 0.3f;
        [SerializeField] private float _effectScaleReferenceHeight = 1.2f;
        [SerializeField] private float _effectScaleMultiplier = 1f;
        [SerializeField] private float _effectScaleMin = 0.8f;
        [SerializeField] private float _effectScaleMax = 1.8f;
        public int id;
        public PresentationWorld PresentationWorld { get; set; }
        private SpriteRenderer damageRenderer;
        private float lastDamageFlash = float.NegativeInfinity;
        private float damageFlashUntil;
        private bool damageFlashing;
        
        public List<Gauge> gauges = new List<Gauge>();
        private readonly HashSet<string> activeEffects = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> pendingEffects = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> ActiveEffects => activeEffects;

        /// <summary>서버가 이번 frame에 보낸 effect 목록에 주어진 이름이 있는지 본다.</summary>
        public bool HasEffect(string effect)
        {
            return !string.IsNullOrEmpty(effect) && activeEffects.Contains(effect);
        }
        
        private string master;
        private Transform _teamIndicatorTransform;
        private SpriteRenderer _teamIndicatorRenderer;
        private ServedObjectEffectRenderer _effectRenderer;
        private ServedObjectGaugeBar _teamColorGaugeBar;
        private readonly List<Gizmo> _gizmos = new List<Gizmo>();
#if UNITY_EDITOR
        private ServedObjectGizmoRenderer _gizmoRenderer;
#endif

        private PositionUpdater _positionUpdater;

        public event Action OnAttack;
        public event Action OnDamaged;
        public event Action<string> OnOtherStatus;
        public event Action OnDestroyed;
        public event Action OnMoved;
        public event Action<Gauge> OnGaugeChanged;

        /// <summary>
        /// Raised after <see cref="UpdateActiveEffects"/> rebuilds <see cref="ActiveEffects"/> and
        /// the resulting set differs from the previous frame's. Not raised on every frame, since
        /// this update runs for every object every frame and most frames carry no effect change.
        /// </summary>
        public event Action OnEffectsChanged;
        
        public event Action OnHpIncreased;
        public event Action OnHpDecreased;

        /// <summary>
        /// Raised once, right after spawning, only when the spawn presentation should play. Objects
        /// that appear through a full-state sync rather than a real spawn never raise it.
        /// </summary>
        public event Action OnSpawned;

        private int lastHp = 0;
        private bool hasReceivedHp;
        
        public void SetMaster(string master)
        {
            this.master = master;

            EnsureSpriteRenderer();
            if (_spriteRenderer == null)
            {
                return;
            }

            _positionUpdater = new PositionUpdater(transform, _spriteRenderer, () => OnMoved?.Invoke(), () => PresentationWorld);
            UpdateTeamIndicator();

            if (master.Equals(RightPlayer))
            {
                if (transform.rotation.eulerAngles.y == 0)
                {
                    _spriteRenderer.flipX = true;
                    return;
                }
                gameObject.transform.Rotate(0, 180, 0);
            }
        }

        /// <summary>
        /// Hands this ServedObject to every <see cref="IServedObjectListener"/> in the hierarchy.
        /// Call once, after the object is fully configured, so prefab components can subscribe
        /// without depending on Awake/Start ordering.
        /// </summary>
        public void BindListeners()
        {
            IServedObjectListener[] listeners = GetComponentsInChildren<IServedObjectListener>(true);
            foreach (IServedObjectListener listener in listeners)
            {
                listener.Bind(this);
            }
        }

        /// <summary>Raises <see cref="OnSpawned"/>. Only the spawner should call this.</summary>
        public void NotifySpawned()
        {
            OnSpawned?.Invoke();
        }

        private void OnEnable()
        {
            OnGaugeChanged += HandleDamageEffect;
        }

        private void OnDisable()
        {
            OnGaugeChanged -= HandleDamageEffect;
        }

        private void LateUpdate()
        {
            UpdateTeamIndicatorPosition();
            if (damageFlashing && Time.time >= damageFlashUntil)
            {
                if (damageRenderer != null) damageRenderer.color = Color.white;
                damageFlashing = false;
            }
        }
        
        public string GetMaster()
        {
            return master;
        }
        
        public void UpdateObject(UpdatedObjectDto updatedObjectDto)
        {
            UpdateMasterIfNeeded(updatedObjectDto.master);
            _positionUpdater?.UpdatePosition(updatedObjectDto);

            UpdateActiveEffects(updatedObjectDto.effects);
            HandleGaugeUpdate(updatedObjectDto.gauges);

            HandleStatus(updatedObjectDto.status);
            EnsureEffectRenderer();
            _effectRenderer.SetEffects(updatedObjectDto.effects);
        }

        private void UpdateActiveEffects(List<string> effects)
        {
            pendingEffects.Clear();
            if (effects != null)
            {
                foreach (string effect in effects)
                {
                    if (string.IsNullOrWhiteSpace(effect))
                    {
                        continue;
                    }

                    string normalizedEffect = effect.Trim();
                    if (!string.Equals(normalizedEffect, "None", StringComparison.Ordinal))
                    {
                        pendingEffects.Add(normalizedEffect);
                    }
                }
            }

            // Compare before writing: this runs for every object every frame, and most frames
            // carry no effect change, so activeEffects must not be torn down and rebuilt (and
            // OnEffectsChanged must not fire) unless the set actually differs.
            if (activeEffects.SetEquals(pendingEffects))
            {
                return;
            }

            activeEffects.Clear();
            foreach (string effect in pendingEffects)
            {
                activeEffects.Add(effect);
            }

            OnEffectsChanged?.Invoke();
        }

        /// <summary>
        /// 서버가 생성 시점에 보낸 gizmo 목록을 보관한다. Editor에서는 debug 선까지 그리지만,
        /// 값 자체는 build에서도 쓴다 — <see cref="TryGetGizmoRadius"/>를 보라.
        /// </summary>
        public void SetGizmos(List<Gizmo> gizmos)
        {
            _gizmos.Clear();
            if (gizmos != null)
            {
                _gizmos.AddRange(gizmos);
            }

#if UNITY_EDITOR
            // Keep DTO geometry for real visual listeners, without editor-only debug lines.
            if (PresentationWorld != null) return;
            if (_gizmoRenderer == null)
            {
                _gizmoRenderer = GetComponent<ServedObjectGizmoRenderer>();
                if (_gizmoRenderer == null)
                {
                    _gizmoRenderer = gameObject.AddComponent<ServedObjectGizmoRenderer>();
                }
            }

            _gizmoRenderer.SetGizmos(gizmos);
#endif
        }

        /// <summary>
        /// 서버가 보낸 gizmo 중 주어진 category의 원 반경을 찾는다. 같은 category가 여러 개면 첫 번째를 쓴다.
        /// category 문자열은 서버 <c>GizmoCategory</c>의 이름 그대로다 (예: <c>DetectionRange</c>).
        /// </summary>
        public bool TryGetGizmoRadius(string category, out float radius)
        {
            radius = 0f;
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }

            foreach (Gizmo gizmo in _gizmos)
            {
                if (gizmo == null || gizmo.radius <= 0f)
                {
                    continue;
                }

                if (!string.Equals(gizmo.category, category, StringComparison.Ordinal))
                {
                    continue;
                }

                radius = gizmo.radius;
                return true;
            }

            return false;
        }

        private void HandleStatus(string status)
        {
            switch (status)
            {
                case "Destroyed":
                    DestroySelf(GameConfig.FRAME_DURATION);
                    break;

                case "Attack":
                    PlayAttackPresentation();
                    break;

                case "Damaged":
                    OnDamaged?.Invoke();
                    FlashDamage();
                    break;

                default:
                    OnOtherStatus?.Invoke(status);
                    break;
            }
        }

        public void PlayAttackPresentation()
        {
            OnAttack?.Invoke();
            if (_swingOnAttack)
            {
                DOTweenAction.SwingMobAttack(GetActualTransform());
            }
        }
        
        /// <summary>
        /// Parent for a spawned effect instance named <paramref name="effectName"/>. Only the
        /// names listed in <see cref="_effectAnchorEffects"/> go to <see cref="_effectAnchor"/>;
        /// every other effect, including player status effects such as <c>Burn</c>, stays on the
        /// object itself.
        /// </summary>
        private Transform GetEffectParent(string effectName)
        {
            if (_effectAnchor != null && IsEffectAnchorEffect(effectName))
            {
                return _effectAnchor;
            }

            return GetActualTransform();
        }

        private bool IsEffectAnchorEffect(string effectName)
        {
            if (_effectAnchorEffects == null)
            {
                return false;
            }

            for (int i = 0; i < _effectAnchorEffects.Length; i++)
            {
                if (string.Equals(_effectAnchorEffects[i], effectName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public Transform GetActualTransform()
        {
            if (_actualTransform != null)
            {
                return _actualTransform;
            }

            Transform namedActualTransform = transform.Find("actualObject");
            if (namedActualTransform != null)
            {
                _actualTransform = namedActualTransform;
                return _actualTransform;
            }

            EnsureSpriteRenderer();
            if (_spriteRenderer != null)
            {
                _actualTransform = _spriteRenderer.transform;
                return _actualTransform;
            }
            
            _actualTransform = transform;
            return _actualTransform;
        }

        /// <summary>
        /// Point on the sprite facing <paramref name="fromWorldPosition"/>, at
        /// <paramref name="edgeBias"/> of the way out to its edge.
        /// <para>
        /// Sprites are billboarded to the tilted 2.5D camera, so the direction is taken in screen space
        /// and applied along the renderer's own axes. A world-space offset would slide off the sprite.
        /// </para>
        /// </summary>
        public Vector3 GetEdgeWorldPositionTowards(Vector3 fromWorldPosition, float edgeBias)
        {
            EnsureSpriteRenderer();

            if (_spriteRenderer == null || _spriteRenderer.sprite == null)
            {
                return GetActualTransform().position;
            }

            Bounds localBounds = _spriteRenderer.sprite.bounds;
            Vector3 centerWorldPosition = _spriteRenderer.transform.TransformPoint(localBounds.center);

            Vector2 screenDirection = GetScreenDirection(centerWorldPosition, fromWorldPosition);
            if (screenDirection == Vector2.zero)
            {
                return centerWorldPosition;
            }

            Vector3 localOffset = new Vector3(
                localBounds.extents.x * screenDirection.x,
                localBounds.extents.y * screenDirection.y,
                0f) * edgeBias;

            return centerWorldPosition + _spriteRenderer.transform.TransformVector(localOffset);
        }

        private Vector2 GetScreenDirection(Vector3 fromWorldPosition, Vector3 toWorldPosition)
        {
            Camera camera = GameScene.Object.PresentationWorld.CameraFor(this);
            Vector3 delta = camera != null
                ? camera.WorldToScreenPoint(toWorldPosition) - camera.WorldToScreenPoint(fromWorldPosition)
                : toWorldPosition - fromWorldPosition;

            Vector2 direction = new Vector2(delta.x, delta.y);
            return direction.sqrMagnitude < Mathf.Epsilon ? Vector2.zero : direction.normalized;
        }

        public Vector3 GetSpeechBubbleAnchorWorldPosition(float verticalOffset = 0.15f)
        {
            EnsureSpriteRenderer();
            Vector3 anchorUp = GetAnchorUpDirection();

            if (_spriteRenderer != null && _spriteRenderer.sprite != null)
            {
                // Sprites are billboarded to the tilted camera in 2.5D, so read the top in the
                // renderer's own space. A world AABB loses the depth the tilt adds and drops the
                // anchor onto the sprite itself.
                Bounds localBounds = _spriteRenderer.sprite.bounds;
                Vector3 topWorldPosition = _spriteRenderer.transform.TransformPoint(
                    new Vector3(localBounds.center.x, localBounds.max.y, 0f));
                return topWorldPosition + anchorUp * verticalOffset;
            }

            return GetActualTransform().position + anchorUp * (1f + verticalOffset);
        }

        /// <summary>Screen-up in world space, so anchors sit above the sprite from the player's view.</summary>
        private Vector3 GetAnchorUpDirection()
        {
            Camera camera = GameScene.Object.PresentationWorld.CameraFor(this);
            return camera != null ? camera.transform.up : Vector3.up;
        }

        private void EnsureEffectRenderer()
        {
            if (_effectRenderer != null)
            {
                return;
            }

            _effectRenderer = new ServedObjectEffectRenderer(
                GetEffectParent,
                GetSpriteWorldHeight,
                _effectScaleReferenceHeight,
                _effectScaleMultiplier,
                _effectScaleMin,
                _effectScaleMax);
        }

        private void HandleGaugeUpdate(List<Gauge> gauges)
        {
            if (gauges == null) return;
            foreach (Gauge gauge in gauges)
            {
                Gauge temp = this.gauges.Find(existedGauge => existedGauge.category.Equals(gauge.category));
                if (temp == null)
                {
                    this.gauges.Add(gauge);
                }
                else
                {
                    temp.maxValue = gauge.maxValue;
                    temp.value = gauge.value;
                }
                OnGaugeChanged?.Invoke(gauge);
            }
        }
        
        private void HandleDamageEffect(Gauge gauge)
        {
            if (!gauge.category.Equals("HP")) return;

            if (!hasReceivedHp)
            {
                lastHp = (int) gauge.value;
                hasReceivedHp = true;
                return;
            }
            
            if (gauge.value < lastHp)
            {
                OnHpDecreased?.Invoke();
                FlashDamage();
            }
            if (gauge.value > lastHp && !Mathf.Approximately(gauge.value, gauge.maxValue))
            {
                OnHpIncreased?.Invoke();
            }
            lastHp = (int) gauge.value;
        }

        private void UpdateMasterIfNeeded(string updatedMaster)
        {
            if (string.IsNullOrEmpty(updatedMaster) || string.Equals(master, updatedMaster, StringComparison.Ordinal))
            {
                return;
            }

            master = updatedMaster;
            UpdateTeamIndicator();
        }

        private void EnsureSpriteRenderer()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        private float GetSpriteWorldHeight()
        {
            EnsureSpriteRenderer();
            if (_spriteRenderer == null)
            {
                return 0f;
            }

            return Mathf.Max(_spriteRenderer.bounds.size.y, _spriteRenderer.bounds.size.x);
        }

        private void UpdateTeamIndicator()
        {
            if (TryUpdateGaugeBarTeamIndicator())
            {
                DisableRuntimeTeamIndicator();
                return;
            }

            EnsureTeamIndicator();
            if (_teamIndicatorRenderer == null)
            {
                return;
            }

            if (!TryGetIndicatorColor(master, out Color indicatorColor))
            {
                _teamIndicatorRenderer.enabled = false;
                return;
            }

            _teamIndicatorRenderer.enabled = true;
            _teamIndicatorRenderer.color = indicatorColor;
            UpdateTeamIndicatorSorting();
            UpdateTeamIndicatorPosition();
        }

        private bool TryUpdateGaugeBarTeamIndicator()
        {
            if (_teamColorGaugeBar == null)
            {
                // Buildings carry a TTL bar built from the same component, so pick by role
                // rather than by hierarchy order — only the HP bar owns the team indicator.
                foreach (ServedObjectGaugeBar bar in GetComponentsInChildren<ServedObjectGaugeBar>(true))
                {
                    if (bar.UsesTeamColors)
                    {
                        _teamColorGaugeBar = bar;
                        break;
                    }
                }
            }

            if (_teamColorGaugeBar == null)
            {
                return false;
            }

            _teamColorGaugeBar.SetObjectIndicatorMaster(master);
            return true;
        }

        private void DisableRuntimeTeamIndicator()
        {
            if (_teamIndicatorRenderer == null)
            {
                return;
            }

            _teamIndicatorRenderer.enabled = false;
        }

        private void EnsureTeamIndicator()
        {
            if (_teamIndicatorRenderer != null)
            {
                return;
            }

            GameObject indicatorObject = new GameObject("TeamIndicator");
            _teamIndicatorTransform = indicatorObject.transform;
            _teamIndicatorTransform.SetParent(transform, false);
            _teamIndicatorTransform.localScale = Vector3.one * _teamIndicatorScale;

            _teamIndicatorRenderer = indicatorObject.AddComponent<SpriteRenderer>();
            _teamIndicatorRenderer.sprite = GetTeamIndicatorSprite();
            _teamIndicatorRenderer.enabled = false;
        }

        private void UpdateTeamIndicatorPosition()
        {
            if (_teamIndicatorRenderer == null || !_teamIndicatorRenderer.enabled)
            {
                return;
            }

            if (_teamIndicatorTransform == null)
            {
                _teamIndicatorTransform = _teamIndicatorRenderer.transform;
            }

            _teamIndicatorTransform.position = GetTeamIndicatorWorldPosition();
            UpdateTeamIndicatorSorting();
        }

        private Vector3 GetTeamIndicatorWorldPosition()
        {
            return GetSpeechBubbleAnchorWorldPosition(_teamIndicatorVerticalOffset);
        }

        private void UpdateTeamIndicatorSorting()
        {
            EnsureSpriteRenderer();
            if (_spriteRenderer == null || _teamIndicatorRenderer == null)
            {
                return;
            }

            _teamIndicatorRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
            _teamIndicatorRenderer.sortingOrder = _spriteRenderer.sortingOrder + 10;
        }

        private static bool TryGetIndicatorColor(string targetMaster, out Color indicatorColor)
        {
            switch (targetMaster)
            {
                case LeftPlayer:
                    indicatorColor = LeftIndicatorColor;
                    return true;
                case RightPlayer:
                    indicatorColor = RightIndicatorColor;
                    return true;
                default:
                    indicatorColor = Color.clear;
                    return false;
            }
        }

        private static Sprite GetTeamIndicatorSprite()
        {
            if (_teamIndicatorSprite != null)
            {
                return _teamIndicatorSprite;
            }

            _teamIndicatorSprite = Resources.Load<Sprite>(TeamIndicatorResourcePath);
            if (_teamIndicatorSprite == null)
            {
                WDebug.LogWarning($"Team indicator sprite not found at Resources/{TeamIndicatorResourcePath}.");
            }

            return _teamIndicatorSprite;
        }

        private void DestroySelf()
        {
            OnDestroyed?.Invoke();
            if (PresentationWorld != null) PresentationWorld.Unregister(id);
            else ObjectContainer.Instance?.UnregisterObject(id);
        }

        private void FlashDamage()
        {
            float interval = HasEffect("Burn") || HasEffect("SandStorm") ? 1f : 0.05f;
            if (PresentationWorld != null) interval = Mathf.Max(interval, PresentationWorld.DamageFlashInterval);
            if (Time.time - lastDamageFlash < interval) return;
            EnsureSpriteRenderer();
            damageRenderer = _spriteRenderer;
            if (damageRenderer == null) return;
            lastDamageFlash = Time.time;
            damageFlashUntil = Time.time + 0.12f;
            damageRenderer.color = new Color(1f, 0.35f, 0.2f);
            damageFlashing = true;
        }
        
        private void DestroySelf(float delay)
        {
            StartCoroutine(DelayedDestroySelfCoroutine(delay));
        }
        
        private IEnumerator DelayedDestroySelfCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            DestroySelf();
        }
    }
}
