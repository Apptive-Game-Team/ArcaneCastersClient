using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameScene.Handler
{
    /// <summary>
    /// Top-centre HUD for adventure matches: the objective line, and a short banner under it for script
    /// lines that have no speaker. Built at runtime like <see cref="PveSpeechBubbleUI"/>, and created only
    /// when the first PVE message arrives, so PVP and tutorial matches never get one.
    /// </summary>
    internal class PveObjectiveHud : MonoBehaviour
    {
        private static PveObjectiveHud instance;

        private const float BannerDuration = 3.5f;
        private const int MaxQueuedBanners = 3;

        private readonly Queue<string> bannerQueue = new Queue<string>();
        private RectTransform objectiveRoot;
        private TextMeshProUGUI objectiveText;
        private RectTransform bannerRoot;
        private TextMeshProUGUI bannerText;
        private Coroutine bannerRoutine;

        public static void ShowObjective(string text)
        {
            Instance.SetObjective(text);
        }

        /// <summary>
        /// Shows a line that has no speaker. Lines queue and play one after another; past three waiting,
        /// the oldest is dropped so a burst cannot delay the newest line for long.
        /// </summary>
        public static void ShowBanner(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Instance.EnqueueBanner(text);
        }

        private static PveObjectiveHud Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject root = new GameObject(nameof(PveObjectiveHud));
                    instance = root.AddComponent<PveObjectiveHud>();
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

        private void SetObjective(string text)
        {
            objectiveText.text = text;
            objectiveRoot.gameObject.SetActive(true);
        }

        private void EnqueueBanner(string text)
        {
            bannerQueue.Enqueue(text);
            while (bannerQueue.Count > MaxQueuedBanners)
            {
                bannerQueue.Dequeue();
            }

            if (bannerRoutine == null)
            {
                bannerRoutine = StartCoroutine(PlayBanners());
            }
        }

        private IEnumerator PlayBanners()
        {
            while (bannerQueue.Count > 0)
            {
                bannerText.text = bannerQueue.Dequeue();
                bannerRoot.gameObject.SetActive(true);

                float hideAtTime = Time.unscaledTime + BannerDuration;
                while (Time.unscaledTime < hideAtTime)
                {
                    yield return null;
                }
            }

            bannerRoot.gameObject.SetActive(false);
            bannerRoutine = null;
        }

        private void CreateUi()
        {
            GameObject canvasObject = new GameObject("PveObjectiveCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Just under the speech bubble canvas (2000) so a bubble is never covered by the HUD.
            canvas.sortingOrder = 1999;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            TMP_FontAsset gameFont = PveSpeechBubbleUI.FindGameFont();

            objectiveRoot = CreateLabel("Objective", canvasRect, gameFont, -24f, 520f, 64f, 34f,
                new Color(0.08f, 0.08f, 0.1f, 0.78f), Color.white, out objectiveText);
            bannerRoot = CreateLabel("Banner", canvasRect, gameFont, -104f, 760f, 60f, 28f,
                new Color(1f, 1f, 1f, 0.96f), new Color(0.16f, 0.16f, 0.16f, 1f), out bannerText);

            objectiveRoot.gameObject.SetActive(false);
            bannerRoot.gameObject.SetActive(false);
        }

        // A top-centre panel with one centred text. The panel keeps a fixed size; the text shrinks to fit
        // so a long line stays on the panel at phone widths.
        private static RectTransform CreateLabel(string objectName, RectTransform parent, TMP_FontAsset font,
            float top, float width, float height, float fontSize, Color background, Color textColor,
            out TextMeshProUGUI text)
        {
            RectTransform panel = CreateRect(objectName, parent);
            panel.anchorMin = new Vector2(0.5f, 1f);
            panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, top);
            panel.sizeDelta = new Vector2(width, height);

            Image image = panel.gameObject.AddComponent<Image>();
            image.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = background;
            image.raycastTarget = false;

            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.18f);
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform textRect = CreateRect("Text", panel);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 6f);
            textRect.offsetMax = new Vector2(-20f, -6f);

            text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.6f;
            text.fontSizeMax = fontSize;
            text.fontSize = fontSize;
            text.color = textColor;
            text.raycastTarget = false;
            return panel;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }
    }
}
