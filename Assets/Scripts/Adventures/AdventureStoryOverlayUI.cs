using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Data.Adventures.Domain;
using Data.Adventures.Local;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Adventures
{
    public class AdventureStoryOverlayUI : MonoBehaviour
    {
        private const string PrefabResourcePath = "UI/Adventures/AdventureStoryOverlayUI";
        private const float ActivePortraitAlpha = 1f;
        private const float InactivePortraitAlpha = 0.35f;
        private const float ActivePortraitScale = 1f;
        private const float InactivePortraitScale = 0.92f;
        private const float InputBufferDuration = 0.15f;

        private static readonly Regex MarkdownBold = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex MarkdownItalic = new Regex(@"\*(?!\s)(.+?)(?<!\s)\*", RegexOptions.Compiled);

        private static AdventureStoryOverlayUI instance;

        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button advanceButton;
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private Image leftPortraitImage;
        [SerializeField] private Image rightPortraitImage;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private string hintMessage = "Tap to continue";

        private readonly List<AdventureStoryLineData> activeLines = new List<AdventureStoryLineData>();

        private Scenario activeScenario;
        private Action completionCallback;
        private int currentLineIndex;
        private float inputReadyAt;
        private bool isResolvingLine;
        private int activeLineVersion;
        private Coroutine lineResolutionCoroutine;

        public static void Play(Scenario scenario, Action onCompleted)
        {
            if (scenario == null || scenario.Story == null || !scenario.Story.HasLines)
            {
                onCompleted?.Invoke();
                return;
            }

            AdventureStoryOverlayUI overlay = Instance;
            if (overlay == null)
            {
                onCompleted?.Invoke();
                return;
            }

            overlay.PlaySequence(scenario, onCompleted);
        }

        private static AdventureStoryOverlayUI Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
                if (prefab == null)
                {
                    WDebug.LogError($"Adventure story prefab not found at Resources/{PrefabResourcePath}.prefab");
                    return null;
                }

                GameObject root = Instantiate(prefab);
                instance = root.GetComponent<AdventureStoryOverlayUI>();
                if (instance == null)
                {
                    WDebug.LogError("Adventure story prefab is missing AdventureStoryOverlayUI component.");
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
            ConfigureCanvas();

            if (advanceButton != null)
            {
                advanceButton.onClick.AddListener(AdvanceStory);
                advanceButton.transition = Selectable.Transition.None;
            }

            if (leftPortraitImage != null)
            {
                leftPortraitImage.preserveAspect = true;
            }

            if (rightPortraitImage != null)
            {
                rightPortraitImage.preserveAspect = true;
            }

            if (hintText != null)
            {
                hintText.text = hintMessage;
            }

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (advanceButton != null)
            {
                advanceButton.onClick.RemoveListener(AdvanceStory);
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (overlayRoot == null || !overlayRoot.activeSelf || isResolvingLine || Time.unscaledTime < inputReadyAt)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                AdvanceStory();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                FinishStory();
            }
        }

        private void ConfigureCanvas()
        {
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 3500;
            }

            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
        }

        private void PlaySequence(Scenario scenario, Action onCompleted)
        {
            if (overlayRoot == null)
            {
                onCompleted?.Invoke();
                return;
            }

            if (overlayRoot.activeSelf)
            {
                return;
            }

            activeLines.Clear();
            if (scenario.Story.Lines != null)
            {
                foreach (var line in scenario.Story.Lines)
                {
                    if (line != null && line.HasDialogue)
                    {
                        activeLines.Add(line);
                    }
                }
            }

            if (activeLines.Count == 0)
            {
                onCompleted?.Invoke();
                return;
            }

            activeScenario = scenario;
            completionCallback = onCompleted;
            currentLineIndex = 0;
            inputReadyAt = Time.unscaledTime + InputBufferDuration;

            ApplyPortrait(leftPortraitImage, activeScenario.Story.LeftImage, false);
            ApplyPortrait(rightPortraitImage, activeScenario.Story.RightImage, true);

            overlayRoot.SetActive(true);
            DisplayCurrentLine();
        }

        private void DisplayCurrentLine()
        {
            if (currentLineIndex < 0 || currentLineIndex >= activeLines.Count)
            {
                FinishStory();
                return;
            }

            AdventureStoryLineData line = activeLines[currentLineIndex];
            bool isLeftSpeaker = line.SpeakerSide == AdventureDialogueSpeakerSide.Left;

            if (speakerNameText != null)
            {
                speakerNameText.text = string.Empty;
            }

            if (dialogueText != null)
            {
                dialogueText.text = string.Empty;
            }

            SetPortraitState(leftPortraitImage, isLeftSpeaker, false);
            SetPortraitState(rightPortraitImage, !isLeftSpeaker, true);

            if (lineResolutionCoroutine != null)
            {
                StopCoroutine(lineResolutionCoroutine);
            }

            activeLineVersion++;
            lineResolutionCoroutine = StartCoroutine(ResolveLocalizedLine(line, currentLineIndex, activeLineVersion));
        }

        private void AdvanceStory()
        {
            if (activeScenario == null || isResolvingLine || Time.unscaledTime < inputReadyAt)
            {
                return;
            }

            currentLineIndex++;
            if (currentLineIndex >= activeLines.Count)
            {
                FinishStory();
                return;
            }

            DisplayCurrentLine();
        }

        private void FinishStory()
        {
            if (lineResolutionCoroutine != null)
            {
                StopCoroutine(lineResolutionCoroutine);
                lineResolutionCoroutine = null;
            }

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            activeLines.Clear();
            activeScenario = null;
            currentLineIndex = 0;
            activeLineVersion = 0;
            isResolvingLine = false;

            Action callback = completionCallback;
            completionCallback = null;
            callback?.Invoke();
        }

        // Character art faces right. The speaker on the right is mirrored so both sides face
        // each other instead of the enemy looking away from the player.
        private static Vector3 PortraitScale(float scale, bool mirrored)
        {
            return new Vector3(mirrored ? -scale : scale, scale, 1f);
        }

        private static void ApplyPortrait(Image portraitImage, Sprite portrait, bool mirrored)
        {
            if (portraitImage == null)
            {
                return;
            }

            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
            portraitImage.color = new Color(1f, 1f, 1f, InactivePortraitAlpha);
            portraitImage.rectTransform.localScale = PortraitScale(InactivePortraitScale, mirrored);
        }

        private static void SetPortraitState(Image portraitImage, bool isActive, bool mirrored)
        {
            if (portraitImage == null || !portraitImage.enabled)
            {
                return;
            }

            float alpha = isActive ? ActivePortraitAlpha : InactivePortraitAlpha;
            float scale = isActive ? ActivePortraitScale : InactivePortraitScale;
            portraitImage.color = new Color(1f, 1f, 1f, alpha);
            portraitImage.rectTransform.localScale = PortraitScale(scale, mirrored);
        }

        private IEnumerator ResolveLocalizedLine(AdventureStoryLineData line, int lineIndex, int lineVersion)
        {
            isResolvingLine = true;

            string localizedSpeakerName = string.Empty;
            string localizedDialogue = string.Empty;

            AsyncOperationHandle<string>? speakerHandle = GetSpeakerNameHandle(line);
            AsyncOperationHandle<string>? dialogueHandle = GetLocalizedHandle(line.LocalizedDialogue);

            if (speakerHandle.HasValue)
            {
                bool speakerCallbackReceived = false;
                string speakerResult = string.Empty;
                AsyncOperationHandle<string> handle = speakerHandle.Value;
                handle.Completed += completedHandle =>
                {
                    speakerCallbackReceived = true;
                    if (completedHandle.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrWhiteSpace(completedHandle.Result))
                    {
                        speakerResult = completedHandle.Result;
                    }
                };

                yield return handle;

                while (!speakerCallbackReceived)
                {
                    yield return null;
                }

                localizedSpeakerName = speakerResult;
            }

            if (dialogueHandle.HasValue)
            {
                bool dialogueCallbackReceived = false;
                string dialogueResult = string.Empty;
                AsyncOperationHandle<string> handle = dialogueHandle.Value;
                handle.Completed += completedHandle =>
                {
                    dialogueCallbackReceived = true;
                    if (completedHandle.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrWhiteSpace(completedHandle.Result))
                    {
                        dialogueResult = completedHandle.Result;
                    }
                };

                yield return handle;

                while (!dialogueCallbackReceived)
                {
                    yield return null;
                }

                localizedDialogue = dialogueResult;
            }

            bool isCurrentLine = activeScenario != null
                                 && currentLineIndex == lineIndex
                                 && activeLineVersion == lineVersion
                                 && currentLineIndex >= 0
                                 && currentLineIndex < activeLines.Count
                                 && ReferenceEquals(activeLines[currentLineIndex], line);

            if (isCurrentLine)
            {
                if (speakerNameText != null)
                {
                    speakerNameText.text = ConvertMarkdownToRichText(localizedSpeakerName);
                }

                if (dialogueText != null)
                {
                    dialogueText.text = ConvertMarkdownToRichText(localizedDialogue);
                }
            }

            if (activeLineVersion == lineVersion)
            {
                isResolvingLine = false;
                inputReadyAt = Time.unscaledTime + InputBufferDuration;
            }

            lineResolutionCoroutine = null;
        }

        // Safety net for authors who keep writing markdown emphasis: **x** becomes <b>x</b> and
        // *x* becomes <i>x</i> so no asterisks reach the screen.
        private static string ConvertMarkdownToRichText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('*') < 0)
            {
                return text;
            }

            text = MarkdownBold.Replace(text, "<b>$1</b>");
            return MarkdownItalic.Replace(text, "<i>$1</i>");
        }

        private AsyncOperationHandle<string>? GetSpeakerNameHandle(AdventureStoryLineData line)
        {
            AdventureStoryCharacterData character = line.SpeakerSide == AdventureDialogueSpeakerSide.Left
                ? activeScenario?.Story?.LeftCharacter
                : activeScenario?.Story?.RightCharacter;

            return character == null ? null : GetLocalizedHandle(character.LocalizedName);
        }

        private static AsyncOperationHandle<string>? GetLocalizedHandle(LocalizedString localizedString)
        {
            if (localizedString == null || localizedString.IsEmpty)
            {
                return null;
            }

            return localizedString.GetLocalizedStringAsync();
        }
    }
}
