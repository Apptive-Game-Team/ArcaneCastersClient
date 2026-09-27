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

        [SerializeField] private Transform nodesContainer;
        [SerializeField] private GameObject stageNodePrefab;
        [SerializeField] private Image mapImage;
        [SerializeField] private GameObject noContentLabel;
        [SerializeField] private GameObject infoCard;
        [SerializeField] private Image stageIconImage;
        [SerializeField] private TMP_Text stageCaptionText;
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private Button playButton;

        private Adventure currentAdventure;
        private Stage selectedStage;

        private void Awake()
        {
            playButton.onClick.AddListener(OnPlayClicked);
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
            for (int i = 0; i < adventure.Stages.Count; i++)
            {
                Stage stage = adventure.Stages[i];
                bool isPlayable = stage.State != State.INACTIVE;
                if (isPlayable && (stageToSelect == null || stage.State == State.ACTIVE))
                {
                    stageToSelect = stage;
                }

                CreateNode(stage, i);
            }

            SelectStage(stageToSelect ?? adventure.Stages[0]);
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
