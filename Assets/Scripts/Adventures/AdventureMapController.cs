using Data.Adventures;
using Data.Adventures.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Adventures
{
    /// <summary>
    /// Builds the chapter map for the adventure currently selected on
    /// <see cref="Data.Adventures.CurrentAdventure"/>: one round node per stage,
    /// placed along a fixed road inside the map card, plus the bottom stage-info
    /// card and its Play button. <see cref="AdventureChapterSelector"/> calls
    /// <see cref="Show"/> again when the player switches chapters from the
    /// segmented control without leaving this scene.
    /// </summary>
    public class AdventureMapController : MonoBehaviour
    {
        // Normalized (x, yFromTop) position of each stage node along the road,
        // read off the approved mockup. A stage beyond the last waypoint keeps
        // extending in the direction of the last segment.
        private static readonly Vector2[] NodeWaypoints =
        {
            new Vector2(0.24f, 0.70f),
            new Vector2(0.42f, 0.70f),
            new Vector2(0.58f, 0.70f),
            new Vector2(0.71f, 0.58f),
        };

        private static readonly Color PlayableColor = new Color(1f, 0.6039216f, 0.12156863f); // #FF9A1F
        private static readonly Color LockedColor = new Color(0.49411765f, 0.5294118f, 0.6f); // #7E8799

        // The caption line ("Forest · Stage 1") normally sits small and grey above the
        // bigger orange stage name. When a stage has no name yet there is nothing to
        // put below it, so the caption itself is promoted to the name line's size,
        // color and outline material and recentred instead of leaving an empty gap.
        private const float CaptionFontSizeWithName = 28f;
        private const float CaptionFontSizePromoted = 56f;
        private const float CaptionYWithName = 39.06f;
        private const float CaptionYPromoted = 0f;
        private static readonly Color CaptionColorWithName = new Color(0.35686275f, 0.38431373f, 0.45882353f); // #5B6275
        private static readonly Color CaptionColorPromoted = new Color(1f, 0.41568628f, 0.101960786f); // #FF6A1A

        [SerializeField] private Transform nodesContainer;
        [SerializeField] private GameObject stageNodePrefab;
        [SerializeField] private Image mapImage;
        [SerializeField] private GameObject noContentLabel;
        [SerializeField] private GameObject infoCard;
        [SerializeField] private Image stageIconImage;
        [SerializeField] private TMP_Text stageCaptionText;
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private Material captionOutlineMaterial;
        [SerializeField] private Button playButton;
        [SerializeField] private RectTransform currentMarker;

        private Adventure currentAdventure;
        private Stage selectedStage;
        private Material defaultCaptionMaterial;

        private void Awake()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            defaultCaptionMaterial = stageCaptionText.fontSharedMaterial;
        }

        private void Start()
        {
            AdventureViewModel.Instance.CurrentState.OnStateChange += OnAdventureStateChanged;
            Show(CurrentAdventure.Instance != null ? CurrentAdventure.Instance.Adventure : null);
        }

        private void OnDestroy()
        {
            if (AdventureViewModel.Instance != null)
            {
                AdventureViewModel.Instance.CurrentState.OnStateChange -= OnAdventureStateChanged;
            }
        }

        /// <summary>Rebuilds the whole map for <paramref name="adventure"/>. Safe to call again on chapter switch.</summary>
        public void Show(Adventure adventure)
        {
            currentAdventure = adventure;
            selectedStage = null;

            foreach (Transform child in nodesContainer)
            {
                // The "you are here" marker lives under the same container so it shares
                // the node road's coordinate space, but it is repositioned in place
                // rather than rebuilt every time, so it must survive this sweep.
                if (currentMarker != null && child == currentMarker)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }

            if (adventure == null || adventure.Stages.Count == 0)
            {
                mapImage.enabled = false;
                noContentLabel.SetActive(true);
                infoCard.SetActive(false);
                return;
            }

            noContentLabel.SetActive(false);
            Sprite background = adventure.Stages[0].BackgroundImage;
            mapImage.sprite = background;
            mapImage.enabled = background != null;

            Stage stageToSelect = null;
            int currentIndex = -1;
            for (int i = 0; i < adventure.Stages.Count; i++)
            {
                Stage stage = adventure.Stages[i];
                bool isPlayable = stage.State != State.INACTIVE;
                if (isPlayable && (stageToSelect == null || stage.State == State.ACTIVE))
                {
                    stageToSelect = stage;
                    currentIndex = i;
                }

                CreateNode(stage, i);
            }

            PlaceCurrentMarker(currentIndex);
            SelectStage(stageToSelect ?? adventure.Stages[0]);
        }

        /// <summary>Puts the "you are here" head marker above the current playable stage's node.</summary>
        private void PlaceCurrentMarker(int stageIndex)
        {
            if (currentMarker == null)
            {
                return;
            }

            if (stageIndex < 0)
            {
                currentMarker.gameObject.SetActive(false);
                return;
            }

            Vector2 waypoint = GetNodeWaypoint(stageIndex);
            Vector2 anchor = new Vector2(waypoint.x, 1f - waypoint.y);
            currentMarker.anchorMin = anchor;
            currentMarker.anchorMax = anchor;
            currentMarker.gameObject.SetActive(true);
        }

        private void CreateNode(Stage stage, int index)
        {
            GameObject nodeObject = Instantiate(stageNodePrefab, nodesContainer);
            var rect = (RectTransform)nodeObject.transform;
            Vector2 waypoint = GetNodeWaypoint(index);
            Vector2 anchor = new Vector2(waypoint.x, 1f - waypoint.y);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;

            bool isPlayable = stage.State != State.INACTIVE;
            Image background = nodeObject.GetComponentInChildren<Image>();
            TMP_Text label = nodeObject.GetComponentInChildren<TMP_Text>();
            background.color = isPlayable ? PlayableColor : LockedColor;
            label.text = (index + 1).ToString();

            Button button = nodeObject.GetComponent<Button>();
            button.interactable = isPlayable;
            if (isPlayable)
            {
                button.onClick.AddListener(() => SelectStage(stage));
            }
        }

        private static Vector2 GetNodeWaypoint(int index)
        {
            if (index < NodeWaypoints.Length)
            {
                return NodeWaypoints[index];
            }

            Vector2 last = NodeWaypoints[NodeWaypoints.Length - 1];
            Vector2 previous = NodeWaypoints[NodeWaypoints.Length - 2];
            Vector2 step = last - previous;
            int extraSteps = index - (NodeWaypoints.Length - 1);
            Vector2 extrapolated = last + step * extraSteps;
            extrapolated.x = Mathf.Clamp01(extrapolated.x);
            extrapolated.y = Mathf.Clamp01(extrapolated.y);
            return extrapolated;
        }

        private void SelectStage(Stage stage)
        {
            selectedStage = stage;
            infoCard.SetActive(true);
            stageIconImage.sprite = currentAdventure.IconImage;

            int stageNumber = currentAdventure.Stages.IndexOf(stage) + 1;
            stageCaptionText.text = $"{currentAdventure.Name} · Stage {stageNumber}";

            bool hasName = !string.IsNullOrEmpty(stage.Name);
            stageNameText.gameObject.SetActive(hasName);
            stageNameText.text = hasName ? stage.Name : string.Empty;

            RectTransform captionRect = stageCaptionText.rectTransform;
            if (hasName)
            {
                stageCaptionText.color = CaptionColorWithName;
                stageCaptionText.fontSize = CaptionFontSizeWithName;
                stageCaptionText.fontSharedMaterial = defaultCaptionMaterial;
                captionRect.anchoredPosition = new Vector2(captionRect.anchoredPosition.x, CaptionYWithName);
            }
            else
            {
                // No stage name yet: promote the caption to the name line's look
                // (bigger, orange, ink-outlined) and centre it instead of leaving
                // the space below it empty.
                stageCaptionText.color = CaptionColorPromoted;
                stageCaptionText.fontSize = CaptionFontSizePromoted;
                stageCaptionText.fontSharedMaterial = captionOutlineMaterial;
                captionRect.anchoredPosition = new Vector2(captionRect.anchoredPosition.x, CaptionYPromoted);
            }

            RefreshPlayButton();
        }

        private void RefreshPlayButton()
        {
            bool isRequesting = AdventureViewModel.Instance.CurrentState.Data == AdventureViewModel.AdventureState.Requesting;
            bool isPlayable = selectedStage != null && selectedStage.State != State.INACTIVE;
            playButton.interactable = isPlayable && !isRequesting;
        }

        private void OnPlayClicked()
        {
            if (selectedStage == null || selectedStage.Scenarios.Count == 0)
            {
                return;
            }

            Scenario scenario = selectedStage.Scenarios[0];
            AdventureStoryOverlayUI.Play(scenario, () => AdventureViewModel.Instance.PlayPVE(scenario.Id));
        }

        private void OnAdventureStateChanged(AdventureViewModel.AdventureState state)
        {
            RefreshPlayButton();
        }
    }
}
