using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TutorialScene
{
    public class ComicIntroPlayer : MonoBehaviour
    {
        [Serializable]
        private class Beat
        {
            public ComicPanelView panel;
            public int captionIndex;
            public string localizationKey;
        }

        private const string TutorialSceneName = "InteractiveTutorialScene";
        private const float InputBufferSeconds = 0.16f;
        private const float LoadingTimeoutSeconds = 12f;

        [SerializeField] private GameObject[] pages;
        [SerializeField] private ComicPanelView[] panels;
        [SerializeField] private Beat[] beats;
        [SerializeField] private Button advanceButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text skipLabel;
        [SerializeField] private TMP_Text nextLabel;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private CanvasGroup fade;

        private StringTable strings;
        private TMP_Text currentText;
        private int beatIndex = -1;
        private int currentPage = -1;
        private float beatStartedAt;
        private float inputReadyAt;
        private int lastInputFrame = -1;
        private bool ready;
        private bool finishing;
        private bool textComplete;

        private void Awake()
        {
            advanceButton.onClick.AddListener(Advance);
            skipButton.onClick.AddListener(Skip);
            foreach (ComicPanelView panel in panels)
            {
                panel.ResetView();
            }
            foreach (GameObject page in pages)
            {
                page.SetActive(false);
            }
            nextLabel.text = string.Empty;
            pageLabel.text = string.Empty;
        }

        private IEnumerator Start()
        {
            if (finishing)
            {
                yield break;
            }
            float deadline = Time.unscaledTime + LoadingTimeoutSeconds;
            var initialization = LocalizationSettings.InitializationOperation;
            while (!initialization.IsDone && Time.unscaledTime < deadline)
            {
                yield return null;
            }
            if (!initialization.IsDone || initialization.Status != AsyncOperationStatus.Succeeded)
            {
                Skip();
                yield break;
            }

            var table = LocalizationSettings.StringDatabase.GetTableAsync("Onboarding");
            while (!table.IsDone && Time.unscaledTime < deadline)
            {
                yield return null;
            }
            if (!table.IsDone || table.Status != AsyncOperationStatus.Succeeded || table.Result == null)
            {
                Skip();
                yield break;
            }
            strings = table.Result;
            foreach (Beat beat in beats)
            {
                if (string.IsNullOrWhiteSpace(GetText(beat.localizationKey)))
                {
                    Skip();
                    yield break;
                }
            }
            skipLabel.text = GetText("onboarding.comic.skip");
            ready = true;
            ShowBeat(0);
        }

        private string GetText(string key)
        {
            return strings?.GetEntry(key)?.LocalizedValue;
        }

        private void Update()
        {
            if (finishing)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Skip();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Advance();
            }
            if (!ready || finishing || textComplete)
            {
                return;
            }

            float elapsed = Time.unscaledTime - beatStartedAt;
            float delay = beats[beatIndex].captionIndex == 0 ? 0.4f : 0.1f;
            beats[beatIndex].panel.SetCaptionAlpha(beats[beatIndex].captionIndex,
                Mathf.Clamp01((elapsed - delay) / 0.2f));
            currentText.maxVisibleCharacters = Mathf.Max(0, Mathf.FloorToInt((elapsed - delay) * 35f));
            if (elapsed >= delay && currentText.maxVisibleCharacters >= currentText.textInfo.characterCount)
            {
                CompleteText();
            }
        }

        public void Advance()
        {
            if (!ready || finishing || Time.unscaledTime < inputReadyAt || lastInputFrame == Time.frameCount)
            {
                return;
            }
            lastInputFrame = Time.frameCount;
            inputReadyAt = Time.unscaledTime + InputBufferSeconds;
            if (!textComplete)
            {
                CompleteText();
            }
            else if (beatIndex + 1 < beats.Length)
            {
                ShowBeat(beatIndex + 1);
            }
            else
            {
                Skip();
            }
        }

        private void ShowBeat(int index)
        {
            beatIndex = index;
            Beat beat = beats[index];
            if (currentPage != beat.panel.Page)
            {
                foreach (GameObject page in pages)
                {
                    page.SetActive(false);
                }
                currentPage = beat.panel.Page;
                pages[currentPage].SetActive(true);
            }
            if (beat.captionIndex == 0)
            {
                beat.panel.Reveal();
            }
            currentText = beat.panel.PrepareCaption(beat.captionIndex, GetText(beat.localizationKey));
            beatStartedAt = Time.unscaledTime;
            inputReadyAt = beatStartedAt + InputBufferSeconds;
            textComplete = false;
            nextLabel.text = string.Empty;
            pageLabel.text = $"{Array.IndexOf(panels, beat.panel) + 1} / {panels.Length}";
        }

        private void CompleteText()
        {
            Beat beat = beats[beatIndex];
            beat.panel.CompleteReveal();
            beat.panel.SetCaptionAlpha(beat.captionIndex, 1f);
            currentText.maxVisibleCharacters = int.MaxValue;
            textComplete = true;
            nextLabel.text = GetText(beatIndex == beats.Length - 1
                ? "onboarding.comic.begin" : "onboarding.comic.next");
        }

        public void Skip()
        {
            if (finishing)
            {
                return;
            }
            finishing = true;
            ready = false;
            advanceButton.interactable = false;
            skipButton.interactable = false;
            StopAllCoroutines();
            StartCoroutine(EnterTutorial());
        }

        private IEnumerator EnterTutorial()
        {
            float startedAt = Time.unscaledTime;
            while (Time.unscaledTime - startedAt < 0.25f)
            {
                fade.alpha = Mathf.Clamp01((Time.unscaledTime - startedAt) / 0.25f);
                yield return null;
            }
            fade.alpha = 1f;
            // 마지막 클릭이나 길게 누른 키가 전투 튜토리얼의 첫 설명을 넘기지 않게 한다.
            while (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)
                || Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter) || Input.touchCount > 0)
            {
                yield return null;
            }
            yield return null;
            SceneManager.LoadScene(TutorialSceneName);
        }

        private void OnDestroy()
        {
            advanceButton.onClick.RemoveListener(Advance);
            skipButton.onClick.RemoveListener(Skip);
        }
    }
}
