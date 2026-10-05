using System.Collections;
using System.Collections.Generic;
using GameScene.Dto;
using GameScene.Object;
using GameScene.ServedObjectComponent;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;

namespace GameScene.Handler
{
    public class PveScriptEventHandler : IFrameInfoHandler<PveScriptEventInfo>
    {
        // In-match speech is keyed by the event's message_key in the Adventure string table,
        // so it follows the player's language. The server's raw lines are only the fallback
        // for a key the table does not have yet.
        private const string DialogueTable = "Adventure";

        private readonly PveSyncState syncState;

        public PveScriptEventHandler(PveSyncState syncState)
        {
            this.syncState = syncState ?? new PveSyncState();
        }

        public void Handler(PveScriptEventInfo pveScriptEvent)
        {
            if (pveScriptEvent == null)
            {
                return;
            }

            // The server replays recent events after a pveSync request; one already shown is dropped.
            if (!syncState.ShouldShow(pveScriptEvent.seq))
            {
                return;
            }

            // The bubble shows one line at a time, so only the last server line would stay visible.
            string fallback = pveScriptEvent.lines != null && pveScriptEvent.lines.Count > 0
                ? pveScriptEvent.lines[pveScriptEvent.lines.Count - 1]
                : pveScriptEvent.key;
            int speakerObjectId = pveScriptEvent.speakerObjectId;
            string key = pveScriptEvent.key;

            if (string.IsNullOrWhiteSpace(key))
            {
                PveDialoguePresenter.ShowLine(speakerObjectId, fallback);
                return;
            }

            // GetLocalizedStringAsync reports a missing key as a successful "No translation
            // found" string, so look the entry up in the table to know whether it exists.
            LocalizationSettings.StringDatabase.GetTableAsync(DialogueTable).Completed += handle =>
            {
                StringTableEntry entry = handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null
                    ? handle.Result.GetEntry(key)
                    : null;
                string text = entry != null ? entry.GetLocalizedString() : fallback;
                PveDialoguePresenter.ShowLine(speakerObjectId, text);
            };
        }
    }

    internal static class PveDialoguePresenter
    {
        public static void ShowLine(int speakerObjectId, string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            PveSpeechBubbleUI.ShowMessage(speakerObjectId, line);
        }
    }

    internal class PveSpeechBubbleUI : MonoBehaviour
    {
        // The game's label font (Lilita One, with Jua as its Hangul fallback; see
        // .agents/docs/DESIGN.md). The bubble is built at runtime with no serialized
        // font, so it borrows the one GameScene's HUD already loaded; without it TMP's
        // default LiberationSans has no Hangul and Korean lines render as boxes.
        private const string GameFontName = "LilitaOne SDF";

        internal static TMP_FontAsset FindGameFont()
        {
            foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (font.name == GameFontName)
                {
                    return font;
                }
            }
            return null;
        }

        private static PveSpeechBubbleUI instance;

        [SerializeField] private float duration = 3.5f;
        [SerializeField] private float maxTextWidth = 320f;
        [SerializeField] private float horizontalPadding = 54f;
        [SerializeField] private float verticalPadding = 48f;
        [SerializeField] private float screenOffsetY = 42f;

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform bubbleRoot;
        private RectTransform backgroundRect;
        private RectTransform textRect;
        private TextMeshProUGUI bubbleText;
        private Coroutine messageRoutine;
        private ServedObject activeTarget;
        private Camera worldCamera;
        private Image backgroundImage;
        private Sprite bubbleSprite;
        private int activeSpeakerObjectId;
        private float hideAtTime;

        public static void ShowMessage(int speakerObjectId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // A line with no speaker has no one to anchor a bubble to, so it goes to the banner.
            if (speakerObjectId <= 0)
            {
                PveObjectiveHud.ShowBanner(message);
                return;
            }

            Instance.ShowOrReplace(speakerObjectId, message);
        }

        private static PveSpeechBubbleUI Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject root = new GameObject(nameof(PveSpeechBubbleUI));
                    instance = root.AddComponent<PveSpeechBubbleUI>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            CreateUi();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void LateUpdate()
        {
            if (activeTarget == null)
            {
                return;
            }

            UpdateBubblePosition();
        }

        private void ShowOrReplace(int speakerObjectId, string message)
        {
            activeSpeakerObjectId = speakerObjectId;
            TryResolveTarget(activeSpeakerObjectId, out activeTarget);
            SetBubbleMessage(message);
            hideAtTime = Time.unscaledTime + duration;
            bubbleRoot.gameObject.SetActive(true);

            if (messageRoutine == null)
            {
                messageRoutine = StartCoroutine(DisplayCurrentMessage());
            }
        }

        private IEnumerator DisplayCurrentMessage()
        {
            while (true)
            {
                if (activeTarget == null)
                {
                    TryResolveTarget(activeSpeakerObjectId, out activeTarget);
                }

                if (activeTarget != null)
                {
                    UpdateBubblePosition();
                }

                if (Time.unscaledTime >= hideAtTime)
                {
                    break;
                }

                yield return null;
            }

            bubbleRoot.gameObject.SetActive(false);
            activeTarget = null;
            activeSpeakerObjectId = 0;
            messageRoutine = null;
        }

        private void CreateUi()
        {
            GameObject canvasObject = new GameObject("SpeechBubbleCanvas");
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            canvasObject.AddComponent<GraphicRaycaster>();
            canvasRect = canvas.GetComponent<RectTransform>();

            bubbleRoot = CreateRect("SpeechBubble", canvasRect);
            bubbleRoot.anchorMin = new Vector2(0.5f, 0.5f);
            bubbleRoot.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRoot.pivot = new Vector2(0.5f, 0f);

            backgroundRect = CreateRect("Background", bubbleRoot);
            backgroundRect.anchorMin = new Vector2(0.5f, 0f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0f);
            backgroundRect.pivot = new Vector2(0.5f, 0f);

            backgroundImage = backgroundRect.gameObject.AddComponent<Image>();
            bubbleSprite = Resources.Load<Sprite>("UI/SpeechBubble");
            backgroundImage.sprite = bubbleSprite != null
                ? bubbleSprite
                : Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            backgroundImage.type = bubbleSprite != null ? Image.Type.Simple : Image.Type.Sliced;
            backgroundImage.color = new Color(1f, 1f, 1f, 0.96f);
            backgroundImage.raycastTarget = false;

            textRect = CreateRect("Text", backgroundRect);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);

            bubbleText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset gameFont = FindGameFont();
            if (gameFont != null)
            {
                bubbleText.font = gameFont;
            }
            bubbleText.alignment = TextAlignmentOptions.Center;
            bubbleText.enableWordWrapping = true;
            bubbleText.fontSize = 26f;
            bubbleText.color = new Color(0.16f, 0.16f, 0.16f, 1f);
            bubbleText.raycastTarget = false;

            if (bubbleSprite == null)
            {
                Outline backgroundOutline = backgroundRect.gameObject.AddComponent<Outline>();
                backgroundOutline.effectColor = new Color(0f, 0f, 0f, 0.18f);
                backgroundOutline.effectDistance = new Vector2(2f, -2f);
            }

            bubbleRoot.gameObject.SetActive(false);
        }

        private void SetBubbleMessage(string message)
        {
            bubbleText.text = message;

            Vector2 preferredSize = bubbleText.GetPreferredValues(message, maxTextWidth, 0f);
            preferredSize.x = Mathf.Min(preferredSize.x, maxTextWidth);

            textRect.sizeDelta = preferredSize;

            Vector2 backgroundSize = new Vector2(
                preferredSize.x + horizontalPadding * 2f,
                preferredSize.y + verticalPadding * 2f
            );
            backgroundRect.sizeDelta = backgroundSize;
            backgroundRect.anchoredPosition = bubbleSprite != null ? Vector2.zero : new Vector2(0f, 10f);

            textRect.anchoredPosition = bubbleSprite != null ? new Vector2(0f, 18f) : Vector2.zero;

            if (backgroundImage != null && bubbleSprite != null)
            {
                backgroundImage.preserveAspect = false;
            }

            bubbleRoot.sizeDelta = backgroundSize;
        }

        private void UpdateBubblePosition()
        {
            EnsureWorldCamera();
            if (worldCamera == null || activeTarget == null)
            {
                bubbleRoot.gameObject.SetActive(false);
                return;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(activeTarget.GetSpeechBubbleAnchorWorldPosition());
            if (screenPoint.z <= 0f)
            {
                bubbleRoot.gameObject.SetActive(false);
                return;
            }

            bubbleRoot.gameObject.SetActive(true);
            screenPoint.y += screenOffsetY;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);

            float halfCanvasWidth = canvasRect.rect.width * 0.5f;
            float halfCanvasHeight = canvasRect.rect.height * 0.5f;
            float halfBubbleWidth = bubbleRoot.sizeDelta.x * 0.5f;
            float bubbleHeight = bubbleRoot.sizeDelta.y;

            localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvasWidth + halfBubbleWidth, halfCanvasWidth - halfBubbleWidth);
            localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvasHeight, halfCanvasHeight - bubbleHeight);

            bubbleRoot.anchoredPosition = localPoint;
        }

        private void EnsureWorldCamera()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private static bool TryResolveTarget(int speakerObjectId, out ServedObject target)
        {
            target = null;
            if (speakerObjectId <= 0 || ObjectContainer.Instance == null)
            {
                return false;
            }

            target = ObjectContainer.Instance.FindById(speakerObjectId);
            return target != null;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }
    }
}
