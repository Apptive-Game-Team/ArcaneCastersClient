using UnityEngine;
using Global.Sound;
using Sound;

namespace GameScene.Card
{
    public class CardUIZoom : MonoBehaviour
    {
        private const float HoverSoundCooldown = 0.12f;
        public static CardUIZoom Instance { get; private set; }
    
        [SerializeField] private ZoomedCardUI zoomedCardUI;
        [SerializeField] private ZoomedCardUI detailPrefab;
        [SerializeField] private AudioSource audioSource;
        private float lastHoverSoundTime = float.NegativeInfinity;
        private CardUI hoveredCard;
        private bool hasHover;
        private bool locallyPaused;
        private float previousTimeScale;
        private float nextHoverRefresh;
    
        private void Awake()
        {
            Instance = this;
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
            hoveredCard = cardUI;
            hasHover = true;
            zoomedCardUI.Show(cardUI);
            RefreshHover();
            if (Time.unscaledTime - lastHoverSoundTime >= HoverSoundCooldown)
            {
                audioSource.PlayOneShot(SoundAssets.CardHover);
                lastHoverSoundTime = Time.unscaledTime;
            }
        }
    
        public void Hide()
        {
            if (hasHover && StompConnector.Instance != null) StompConnector.Instance.TrySendCardHover(false);
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
            SetLocalPause(paused && hasHover && hoveredCard != null && Application.isFocused);
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
        }

        private void Update()
        {
            if (!hasHover) return;
            if (hoveredCard == null || !hoveredCard.isActiveAndEnabled) {
                Hide();
                return;
            }
            if (Time.unscaledTime >= nextHoverRefresh) RefreshHover();
        }

        private void RefreshHover()
        {
            if (StompConnector.Instance != null) StompConnector.Instance.TrySendCardHover(true);
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
