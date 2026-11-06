using UnityEngine;
using Global.Sound;
using Sound;

namespace GameScene.Card
{
    public class CardUIZoom : MonoBehaviour
    {
        private const string AutoPauseKey = "PracticeCardHoverAutoPause";
        private const float HoverSoundCooldown = 0.12f;
        private const float DescriptionHoverDelay = 1f;
        public static CardUIZoom Instance { get; private set; }
    
        [SerializeField] private ZoomedCardUI zoomedCardUI;
        [SerializeField] private ZoomedCardUI detailPrefab;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private GameObject pauseOverlay;
        private bool hoverLeaseSent;
        public bool AutoPause { get; private set; } = true;
        private float lastHoverSoundTime = float.NegativeInfinity;
        private CardUI hoveredCard;
        private bool hasHover;
        private bool locallyPaused;
        private float previousTimeScale;
        private float nextHoverRefresh;
        private float hoverReadyTime;
    
        private void Awake()
        {
            Instance = this;
            if (Global.SceneContext.MatchInfo?.IsPracticeMatch == true)
                AutoPause = PlayerPrefs.GetInt(AutoPauseKey, 1) != 0;
            zoomedCardUI.Hide();
            if (detailPrefab != null) zoomedCardUI = Instantiate(detailPrefab, transform);
            zoomedCardUI.Hide();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.clip = SoundAssets.CardHover;
            SoundVolumeSetter.Attach(audioSource, SoundVolumeSetter.SoundType.UI);
        }

        public void Show(CardUI cardUI)
        {
            if (cardUI == null || cardUI.Magic == null || !Application.isFocused) return;
            if (hoveredCard == cardUI) return;
            Hide();
            hoveredCard = cardUI;
            hoverReadyTime = Time.unscaledTime + DescriptionHoverDelay;
            if (detailPrefab == null) ShowDetails();
        }

        private void ShowDetails()
        {
            hasHover = true;
            zoomedCardUI.Show(hoveredCard);
            RefreshHover();
            if (Time.unscaledTime - lastHoverSoundTime >= HoverSoundCooldown)
            {
                audioSource.PlayOneShot(SoundAssets.CardHover);
                lastHoverSoundTime = Time.unscaledTime;
            }
        }
    
        public void Hide()
        {
            ReleaseHover();
            hasHover = false;
            hoveredCard = null;
            SetLocalPause(false);
            if (zoomedCardUI != null) zoomedCardUI.Hide();
        }

        public void Hide(CardUI cardUI)
        {
            if (hoveredCard == cardUI) Hide();
        }

        public void ApplyServerPause(bool paused)
        {
            SetLocalPause(paused && AutoPause && hoverLeaseSent && hasHover && hoveredCard != null && Application.isFocused);
        }

        public void SetAutoPause(bool enabled)
        {
            if (AutoPause == enabled) return;
            AutoPause = enabled;
            PlayerPrefs.SetInt(AutoPauseKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!enabled)
            {
                ReleaseHover();
                SetLocalPause(false);
            }
            else if (hasHover)
            {
                RefreshHover();
            }
        }

        private void ReleaseHover()
        {
            if (!hoverLeaseSent) return;
            hoverLeaseSent = false;
            StompConnector.Instance?.TrySendCardHover(false);
        }

        private void SetLocalPause(bool paused)
        {
            if (locallyPaused == paused) return;
            if (paused) {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            } else {
                Time.timeScale = previousTimeScale;
            }
            locallyPaused = paused;
            if (pauseOverlay != null) pauseOverlay.SetActive(paused);
        }

        private void Update()
        {
            if (hoveredCard is null) return;
            if (hoveredCard == null || !hoveredCard.isActiveAndEnabled) {
                Hide();
                return;
            }
            if (!hasHover) {
                if (Time.unscaledTime >= hoverReadyTime) ShowDetails();
                return;
            }
            if (AutoPause && Time.unscaledTime >= nextHoverRefresh) RefreshHover();
        }

        private void RefreshHover()
        {
            if (!AutoPause) return;
            if (StompConnector.Instance?.TrySendCardHover(true) == true) hoverLeaseSent = true;
            nextHoverRefresh = Time.unscaledTime + 0.5f;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Hide();
            else {
                CardUI card = PointerInputUtility.FindUnderPointer<CardUI>(Input.mousePosition);
                if (card != null && card.isActiveAndEnabled) Show(card);
            }
        }

        private void OnDisable()
        {
            Hide();
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            Instance = this;
        }
    }
}
