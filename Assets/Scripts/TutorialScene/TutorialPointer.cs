using System;
using UnityEngine;
using UnityEngine.UI;

namespace TutorialScene
{
    /// <summary>
    /// 튜토리얼에서 눌러야 할 곳을 손가락 그림으로 가리킨다. 손끝이 대상 중심에 오도록 두고,
    /// 0.6초마다 손을 대상 쪽으로 눌렀다 뗀다. 누르는 순간 손끝 자리에 고리가 한 번 퍼진다.
    /// 카드를 골라 전장에 놓는 단계는 카드를 누르고 놓을 자리로 옮겨 가 누르는 동작을 반복한다.
    /// </summary>
    /// <remarks>
    /// 프리팹 루트가 자기 Screen Space Overlay Canvas 를 갖는다. 대상이 어느 Canvas 에 있든
    /// 화면 좌표로 바꿔 따라가므로, 씬마다 Canvas 구성이 달라도 씬 파일을 고치지 않고 띄울 수
    /// 있다. 손과 고리는 raycast 를 받지 않아 아래 버튼을 그대로 누를 수 있다.
    /// </remarks>
    public sealed class TutorialPointer : MonoBehaviour
    {
        private const string ResourcePath = "UI/Tutorial/TutorialPointer";

        [SerializeField] private RectTransform hand;
        [SerializeField] private Image handImage;
        [SerializeField] private RectTransform ring;
        [SerializeField] private Image ringImage;

        [Header("Press")]
        [SerializeField] private float pressPeriodSeconds = 0.6f;
        [SerializeField] private float pressDistance = 26f;
        [SerializeField] private float pressedScale = 0.88f;

        [Header("Drag")]
        [SerializeField] private float dragCycleSeconds = 1.8f;

        [Header("Ring")]
        [SerializeField] private float ringSeconds = 0.35f;
        [SerializeField] private float ringStartScale = 0.4f;
        [SerializeField] private float ringEndScale = 1.4f;
        [SerializeField] private float ringStartAlpha = 0.8f;

        private RectTransform canvasRect;
        private Func<Vector2?> pressPoint;
        private Func<Vector2?> dragTo;
        private float startTime;
        private float lastPhase;
        private float ringStartedAt = float.NegativeInfinity;

        /// <summary>UI 대상 하나를 누르라고 가리킨다. 대상이 꺼져 있는 동안은 손을 숨긴다.</summary>
        public static TutorialPointer PointAt(Transform target)
        {
            return PointAtScreen(() => ScreenCenterOf(target));
        }

        /// <summary>화면 좌표 한 점을 누르라고 가리킨다. null 을 주는 동안은 손을 숨긴다.</summary>
        public static TutorialPointer PointAtScreen(Func<Vector2?> screenPoint)
        {
            TutorialPointer pointer = Spawn();
            if (pointer != null)
            {
                pointer.pressPoint = screenPoint;
                pointer.dragTo = null;
            }

            return pointer;
        }

        /// <summary>UI 대상을 누른 뒤 화면 좌표 한 점을 누르라고, 대상에서 그 점까지 손을 옮기며 가리킨다.</summary>
        public static TutorialPointer Drag(Transform from, Func<Vector2?> toScreenPoint)
        {
            TutorialPointer pointer = Spawn();
            if (pointer != null)
            {
                pointer.pressPoint = () => ScreenCenterOf(from);
                pointer.dragTo = toScreenPoint;
            }

            return pointer;
        }

        /// <summary>월드 좌표 한 점을 Camera.main 으로 비춘 화면 좌표. 카메라 뒤면 null.</summary>
        public static Vector2? ScreenPointOfWorld(Vector3 worldPosition)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            Vector3 screen = camera.WorldToScreenPoint(worldPosition);
            return screen.z > 0f ? new Vector2(screen.x, screen.y) : (Vector2?)null;
        }

        /// <summary>UI 대상의 사각형 중심을 화면 좌표로 돌려준다. 대상이 없거나 꺼져 있으면 null.</summary>
        public static Vector2? ScreenCenterOf(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return null;
            }

            RectTransform rectTransform = target as RectTransform;
            if (rectTransform == null)
            {
                return ScreenPointOfWorld(target.position);
            }

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            Camera camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
            return RectTransformUtility.WorldToScreenPoint(camera, worldCenter);
        }

        /// <summary>
        /// 화면 좌표가 화면 안에 있는지. 마나 바가 내려가 있으면 손패 카드는 화면 아래로 빠져
        /// 있으므로, 카드를 끌라고 가리키기 전에 이것으로 카드가 보이는지 확인한다.
        /// </summary>
        public static bool IsFullyOnScreen(RectTransform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            Canvas canvas = target.GetComponentInParent<Canvas>();
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            Camera camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
                if (screen.x < 0f || screen.y < 0f || screen.x > Screen.width || screen.y > Screen.height)
                {
                    return false;
                }
            }

            return true;
        }

        public void Dismiss()
        {
            if (this != null)
            {
                Destroy(gameObject);
            }
        }

        private static TutorialPointer Spawn()
        {
            TutorialPointer prefab = Resources.Load<TutorialPointer>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"[TutorialPointer] Missing prefab at Resources/{ResourcePath}");
                return null;
            }

            return Instantiate(prefab);
        }

        private void Awake()
        {
            canvasRect = transform as RectTransform;
            startTime = Time.unscaledTime;
            SetHandVisible(false);
            SetRingAlpha(0f);
        }

        private void LateUpdate()
        {
            Vector2? press = pressPoint?.Invoke();
            if (!press.HasValue || canvasRect == null || hand == null)
            {
                SetHandVisible(false);
                SetRingAlpha(0f);
                return;
            }

            float elapsed = Time.unscaledTime - startTime;
            Vector2? to = dragTo?.Invoke();
            if (to.HasValue)
            {
                UpdateDrag(ToLocal(press.Value), ToLocal(to.Value), elapsed);
            }
            else
            {
                UpdatePress(ToLocal(press.Value), elapsed);
            }

            UpdateRing();
        }

        private void UpdatePress(Vector2 tip, float elapsed)
        {
            float phase = Mathf.Repeat(elapsed / pressPeriodSeconds, 1f);

            // 0 에서 떼고 0.5 에서 가장 깊이 누른다.
            float depth = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
            if (lastPhase < 0.5f && phase >= 0.5f)
            {
                StartRing(tip);
            }

            lastPhase = phase;
            PlaceHand(tip, 1f - depth, Mathf.Lerp(1f, pressedScale, depth), 1f);
        }

        /// <summary>
        /// 카드는 끌어서 놓는 것이 아니라 눌러 고른 뒤 놓을 자리를 누른다(CardUI.OnCardClicked,
        /// FieldSelector). 그래서 손은 출발점을 한 번 누르고(0~0.2), 손을 든 채 도착점으로
        /// 옮겨(0.2~0.6), 도착점을 한 번 누른 뒤(0.6~0.8), 잠깐 사라졌다가(0.8~1) 다시 한다.
        /// </summary>
        private void UpdateDrag(Vector2 from, Vector2 to, float elapsed)
        {
            float phase = Mathf.Repeat(elapsed / dragCycleSeconds, 1f);

            if (phase < 0.2f)
            {
                if ((lastPhase < 0.1f || lastPhase > phase) && phase >= 0.1f)
                {
                    StartRing(from);
                }

                float depth = Mathf.Sin(phase / 0.2f * Mathf.PI);
                PlaceHand(from, 1f - depth, Mathf.Lerp(1f, pressedScale, depth), 1f);
            }
            else if (phase < 0.6f)
            {
                float travel = Mathf.SmoothStep(0f, 1f, (phase - 0.2f) / 0.4f);
                PlaceHand(Vector2.Lerp(from, to, travel), 1f, 1f, 1f);
            }
            else if (phase < 0.8f)
            {
                if (lastPhase < 0.7f && phase >= 0.7f)
                {
                    StartRing(to);
                }

                float depth = Mathf.Sin((phase - 0.6f) / 0.2f * Mathf.PI);
                PlaceHand(to, 1f - depth, Mathf.Lerp(1f, pressedScale, depth), 1f);
            }
            else
            {
                SetHandVisible(false);
            }

            lastPhase = phase;
        }

        /// <summary>
        /// 손끝을 tip 에 맞춘다. 손은 기본으로 오른쪽 아래로 뻗어 있고, 그쪽이 화면 밖으로
        /// 나가면 좌우 또는 위아래를 뒤집는다. lift 는 0 이면 누른 자리, 1 이면 손을 뗀 자리다.
        /// </summary>
        private void PlaceHand(Vector2 tip, float lift, float scale, float alpha)
        {
            Rect bounds = canvasRect.rect;
            Vector2 size = hand.rect.size;
            float horizontal = tip.x + size.x > bounds.xMax ? -1f : 1f;
            float vertical = tip.y - size.y < bounds.yMin ? -1f : 1f;

            // 손이 뻗은 쪽(오른쪽 아래)으로 물러났다가 손끝 방향으로 누른다.
            Vector2 away = new Vector2(horizontal, -vertical).normalized * (pressDistance * lift);
            hand.anchoredPosition = tip + away;
            hand.localScale = new Vector3(horizontal * scale, vertical * scale, 1f);

            SetHandVisible(true);
            if (handImage != null)
            {
                Color color = handImage.color;
                color.a = alpha;
                handImage.color = color;
            }
        }

        private void StartRing(Vector2 tip)
        {
            if (ring == null)
            {
                return;
            }

            ring.anchoredPosition = tip;
            ringStartedAt = Time.unscaledTime;
        }

        private void UpdateRing()
        {
            if (ring == null)
            {
                return;
            }

            float progress = (Time.unscaledTime - ringStartedAt) / ringSeconds;
            if (progress < 0f || progress > 1f)
            {
                SetRingAlpha(0f);
                return;
            }

            float scale = Mathf.Lerp(ringStartScale, ringEndScale, progress);
            ring.localScale = new Vector3(scale, scale, 1f);
            SetRingAlpha(Mathf.Lerp(ringStartAlpha, 0f, progress));
        }

        private Vector2 ToLocal(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 local);
            return local;
        }

        private void SetHandVisible(bool visible)
        {
            if (hand != null && hand.gameObject.activeSelf != visible)
            {
                hand.gameObject.SetActive(visible);
            }
        }

        private void SetRingAlpha(float alpha)
        {
            if (ringImage == null)
            {
                return;
            }

            Color color = ringImage.color;
            color.a = alpha;
            ringImage.color = color;
        }
    }
}
