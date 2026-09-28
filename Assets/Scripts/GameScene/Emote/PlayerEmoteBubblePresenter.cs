using System.Collections.Generic;
using DG.Tweening;
using GameScene.ServedObjectComponent;
using Global;
using UnityEngine;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;

namespace GameScene.Emote
{
    /// <summary>
    /// caster 머리 위에 emote 말풍선을 띄운다. Player.prefab 에 붙어 있고,
    /// 자기 쪽으로 온 emote 만 받는다.
    /// </summary>
    /// <remarks>
    /// 말풍선은 PveSpeechBubbleUI 와 같은 방식이다. screen space overlay canvas 를 만들고
    /// <see cref="ServedObject.GetSpeechBubbleAnchorWorldPosition"/> 를 화면 좌표로 옮겨 따라간다.
    /// sprite 를 world 에 세우면 기울어진 카메라 때문에 찌그러진다.
    /// </remarks>
    public class PlayerEmoteBubblePresenter : MonoBehaviour
    {
        private const string BubbleSpriteResourcePath = "UI/SpeechBubble";

        // PveSpeechBubbleUI 가 2000 을 쓴다. 대사가 올라오면 그쪽이 위로 오게 둔다.
        private const int CanvasSortingOrder = 1900;

        // DOTweenAction.PopIn 과 같은 모양이다. 처음 크기를 미리 넣고 DOScale 로 1 까지 올린다.
        private const float PopStartScale = 0.6f;

        private static readonly List<PlayerEmoteBubblePresenter> Presenters =
            new List<PlayerEmoteBubblePresenter>();

        [SerializeField] private ServedObject servedObject;
        [SerializeField] private EmoteSpriteEntry[] emoteSprites;
        [SerializeField] private float displaySeconds = 3f;
        [SerializeField] private float popSeconds = 0.18f;
        [SerializeField] private float fadeSeconds = 0.35f;
        [SerializeField] private float bubbleWidth = 190f;
        [SerializeField] private float bubbleHeight = 150f;
        [SerializeField] private float emoteSize = 96f;
        [SerializeField] private float emoteOffsetY = 18f;
        [SerializeField] private float screenOffsetY = 24f;

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform bubbleRoot;
        private CanvasGroup bubbleGroup;
        private Image emoteImage;
        private Camera worldCamera;
        private Sequence showSequence;

        /// <summary>
        /// 그 side 의 caster 에게 말풍선을 띄운다. 같은 side 의 것이 이미 떠 있으면
        /// 그림만 바꾸고 3초를 처음부터 다시 센다.
        /// </summary>
        public static void ShowForSide(string side, EmoteType emote)
        {
            if (string.IsNullOrWhiteSpace(side))
            {
                return;
            }

            bool shown = false;
            for (int i = Presenters.Count - 1; i >= 0; i--)
            {
                PlayerEmoteBubblePresenter presenter = Presenters[i];
                if (presenter == null)
                {
                    Presenters.RemoveAt(i);
                    continue;
                }

                if (presenter.MatchesSide(side))
                {
                    presenter.Show(emote);
                    shown = true;
                }
            }

            if (!shown)
            {
                WDebug.LogWarning($"[Emote] 말풍선을 띄울 caster 가 없다. side: {side}");
            }
        }

        private void Awake()
        {
            if (servedObject == null)
            {
                servedObject = GetComponent<ServedObject>();
            }
        }

        private void OnEnable()
        {
            if (!Presenters.Contains(this))
            {
                Presenters.Add(this);
            }
        }

        private void OnDisable()
        {
            Presenters.Remove(this);
            showSequence?.Kill();
            Hide();
        }

        private void OnDestroy()
        {
            showSequence?.Kill();
            showSequence = null;

            if (canvas != null)
            {
                Destroy(canvas.gameObject);
                canvas = null;
            }
        }

        private void LateUpdate()
        {
            if (bubbleRoot == null || !bubbleRoot.gameObject.activeSelf)
            {
                return;
            }

            UpdateBubblePosition();
        }

        private bool MatchesSide(string side)
        {
            if (servedObject == null)
            {
                return false;
            }

            string master = servedObject.GetMaster();
            return !string.IsNullOrEmpty(master)
                   && string.Equals(master, side, System.StringComparison.Ordinal);
        }

        private void Show(EmoteType emote)
        {
            Sprite sprite = EmoteSpriteEntries.Find(emoteSprites, emote);
            if (sprite == null)
            {
                WDebug.LogWarning($"[Emote] 아이콘이 없어 말풍선을 띄우지 않는다. emote: {emote}");
                return;
            }

            EnsureUi();
            emoteImage.sprite = sprite;

            showSequence?.Kill();

            bubbleGroup.alpha = 1f;
            bubbleRoot.localScale = Vector3.one * PopStartScale;
            bubbleRoot.gameObject.SetActive(true);
            UpdateBubblePosition();

            // 3초는 서버 cooldown 과 같은 값이다. 뜨는 연출과 사라지는 연출을 그 안에 넣어
            // 다음 emote 가 올 수 있는 시점에는 말풍선이 이미 지워져 있게 한다.
            float holdSeconds = Mathf.Max(0f, displaySeconds - popSeconds - fadeSeconds);

            Sequence sequence = DOTween.Sequence();
            showSequence = sequence;
            sequence.SetLink(gameObject);
            sequence.Append(bubbleRoot
                .DOScale(Vector3.one, popSeconds)
                .SetEase(Ease.OutBack));
            sequence.AppendInterval(holdSeconds);
            sequence.Append(bubbleGroup.DOFade(0f, fadeSeconds).SetEase(Ease.InQuad));
            sequence.OnComplete(Hide);
        }

        private void Hide()
        {
            showSequence = null;

            if (bubbleRoot != null)
            {
                bubbleRoot.gameObject.SetActive(false);
            }
        }

        private void UpdateBubblePosition()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null || servedObject == null)
            {
                bubbleRoot.gameObject.SetActive(false);
                return;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(
                servedObject.GetSpeechBubbleAnchorWorldPosition());
            if (screenPoint.z <= 0f)
            {
                bubbleRoot.gameObject.SetActive(false);
                return;
            }

            screenPoint.y += screenOffsetY;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, null, out Vector2 localPoint);

            float halfCanvasWidth = canvasRect.rect.width * 0.5f;
            float halfCanvasHeight = canvasRect.rect.height * 0.5f;
            float halfBubbleWidth = bubbleRoot.sizeDelta.x * 0.5f;

            localPoint.x = Mathf.Clamp(
                localPoint.x, -halfCanvasWidth + halfBubbleWidth, halfCanvasWidth - halfBubbleWidth);
            localPoint.y = Mathf.Clamp(
                localPoint.y, -halfCanvasHeight, halfCanvasHeight - bubbleRoot.sizeDelta.y);

            bubbleRoot.anchoredPosition = localPoint;
        }

        private void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            // Player 는 world 에 놓인 object 라서 그 아래에 overlay canvas 를 두면
            // 부모의 scale 을 따라간다. canvas 는 scene 최상단에 만들고 OnDestroy 에서 지운다.
            GameObject canvasObject = new GameObject($"{nameof(PlayerEmoteBubblePresenter)}Canvas");

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            canvasRect = canvas.GetComponent<RectTransform>();

            bubbleRoot = CreateRect("Bubble", canvasRect);
            bubbleRoot.anchorMin = new Vector2(0.5f, 0.5f);
            bubbleRoot.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRoot.pivot = new Vector2(0.5f, 0f);
            bubbleRoot.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);

            bubbleGroup = bubbleRoot.gameObject.AddComponent<CanvasGroup>();
            bubbleGroup.interactable = false;
            bubbleGroup.blocksRaycasts = false;

            Image backgroundImage = bubbleRoot.gameObject.AddComponent<Image>();
            backgroundImage.sprite = Resources.Load<Sprite>(BubbleSpriteResourcePath);
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = true;
            backgroundImage.raycastTarget = false;

            RectTransform emoteRect = CreateRect("Emote", bubbleRoot);
            emoteRect.anchorMin = new Vector2(0.5f, 0.5f);
            emoteRect.anchorMax = new Vector2(0.5f, 0.5f);
            emoteRect.pivot = new Vector2(0.5f, 0.5f);
            emoteRect.sizeDelta = new Vector2(emoteSize, emoteSize);
            emoteRect.anchoredPosition = new Vector2(0f, emoteOffsetY);

            emoteImage = emoteRect.gameObject.AddComponent<Image>();
            emoteImage.preserveAspect = true;
            emoteImage.raycastTarget = false;

            bubbleRoot.gameObject.SetActive(false);
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }
    }
}
