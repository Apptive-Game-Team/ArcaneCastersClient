using Data.Magic;
using GameScene.Card;
using Global;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameScene.ServedObjectComponent
{
    public class Selectable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const string HitboxObjectName = "SelectableHitbox";

        [SerializeField] private Camera worldCamera;
        [SerializeField] private Behaviour outlineComponent;
        [SerializeField, Min(0.1f)] private float hitboxDepth = 0.5f;

        private SpriteRenderer _spriteRenderer;
        private BoxCollider _selectionCollider;
        private Bounds _lastRendererBounds;
        private bool _hasRendererBounds;
        private bool _isPointerOver;

        void Awake()
        {
            if (GameScene.Object.PresentationWorld.For(this) != null) { enabled = false; return; }
            if (!worldCamera) worldCamera = Camera.main;

            _spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            outlineComponent ??= FindOutlineComponent();
            if (outlineComponent != null)
                outlineComponent.enabled = false;

            if (_spriteRenderer == null)
            {
                WDebug.LogWarning($"{nameof(Selectable)} could not find a SpriteRenderer for {name}.");
                return;
            }

            _selectionCollider = GetOrCreateSelectionCollider(_spriteRenderer);
            SyncSelectionCollider(true);

            if (worldCamera && !worldCamera.GetComponent<PhysicsRaycaster>())
                worldCamera.gameObject.AddComponent<PhysicsRaycaster>();
        }

        private void LateUpdate()
        {
            SyncSelectionCollider(false);

            if (_isPointerOver)
            {
                SelectionGroundIndicator.Show(transform, GetSelectionPosition());
            }
        }

        private void OnDisable()
        {
            _isPointerOver = false;
            SelectionGroundIndicator.Hide(transform);
            if (outlineComponent != null)
            {
                outlineComponent.enabled = false;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;

            _isPointerOver = true;
            SelectionGroundIndicator.Show(transform, GetSelectionPosition());
            if (outlineComponent != null)
            {
                outlineComponent.enabled = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;
            SelectionGroundIndicator.Hide(transform);
            if (outlineComponent != null)
            {
                outlineComponent.enabled = false;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _isPointerOver = false;
            SelectionGroundIndicator.Hide(transform);
            if (outlineComponent != null)
            {
                outlineComponent.enabled = false;
            }

            CardInputSender cardInputSender = CardInputSender.Instance;
            if (cardInputSender == null)
            {
                return;
            }

            // 유닛과 건물은 누른 유닛 자리로 끌어당기지 않는다. FieldSelector 가 마우스 아래 바닥에 놓는다.
            if (cardInputSender.TryGetCurrentMagicData(out CombinedMagicData magic) &&
                MagicCastKinds.LeavesBody(magic.castKind))
            {
                return;
            }

            // SendInput을 직접 부르면 isFieldSelectMode 게이트를 건너뛰어,
            // 조합을 확정하지 않은 상태에서 유닛을 클릭하는 것만으로 useMagic이 전송된다.
            // TrySendInput은 필드 선택 모드일 때만 실제로 보낸다.
            Vector3 pos = GetSelectionPosition();
            if (!cardInputSender.TrySendInput(pos))
            {
                return;
            }

            // 전송되면 선택 카드 목록이 비워지므로 예상 마법 UI도 같이 갱신한다.
            cardInputSender.SetExpectedMagicUI();
        }

        private Vector3 GetSelectionPosition()
        {
            Transform target = transform.parent != null ? transform.parent : transform;
            return target.position;
        }

        private BoxCollider GetOrCreateSelectionCollider(SpriteRenderer spriteRenderer)
        {
            Transform hitboxTransform = spriteRenderer.transform.Find(HitboxObjectName);
            if (hitboxTransform == null)
            {
                GameObject hitboxObject = new GameObject(HitboxObjectName);
                hitboxTransform = hitboxObject.transform;
                hitboxTransform.SetParent(spriteRenderer.transform, false);
            }

            hitboxTransform.gameObject.layer = spriteRenderer.gameObject.layer;
            BoxCollider boxCollider = hitboxTransform.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = hitboxTransform.gameObject.AddComponent<BoxCollider>();
            }

            boxCollider.isTrigger = true;
            return boxCollider;
        }

        private void SyncSelectionCollider(bool force)
        {
            if (_spriteRenderer == null || _selectionCollider == null)
            {
                return;
            }

            Bounds rendererBounds = _spriteRenderer.localBounds;
            _selectionCollider.enabled = _spriteRenderer.enabled && _spriteRenderer.sprite != null;
            if (!force && _hasRendererBounds && BoundsApproximatelyEqual(rendererBounds, _lastRendererBounds))
            {
                return;
            }

            float safeDepth = Mathf.Max(hitboxDepth, 0.1f);
            _selectionCollider.center = rendererBounds.center;
            _selectionCollider.size = new Vector3(rendererBounds.size.x, rendererBounds.size.y, safeDepth);
            _lastRendererBounds = rendererBounds;
            _hasRendererBounds = true;
        }

        private static bool BoundsApproximatelyEqual(Bounds left, Bounds right)
        {
            return (left.center - right.center).sqrMagnitude <= Mathf.Epsilon &&
                   (left.size - right.size).sqrMagnitude <= Mathf.Epsilon;
        }

        private Behaviour FindOutlineComponent()
        {
            Behaviour[] candidates = GetComponentsInChildren<Behaviour>(true);
            foreach (Behaviour candidate in candidates)
            {
                if (candidate == null || candidate == this) continue;
                if (candidate.GetType().Name.Contains("Outlin"))
                    return candidate;
            }

            return null;
        }
    }
}
