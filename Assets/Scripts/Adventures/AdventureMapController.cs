using System.Collections.Generic;
using Data.Adventures;
using Data.Adventures.Domain;
using Global;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Adventures
{
    /// <summary>
    /// Builds the chapter map for the adventure currently selected on
    /// <see cref="Data.Adventures.CurrentAdventure"/>: one round node per stage,
    /// placed along a fixed road inside the map card, plus the bottom stage-info
    /// card and its Play button.
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

        private static readonly Color CurrentColor = new Color(1f, 0.6039216f, 0.12156863f); // #FF9A1F
        private static readonly Color LockedColor = new Color(0.49411765f, 0.5294118f, 0.6f); // #7E8799
        // Same green ScenarioButton already uses for a finished scenario, reused here
        // so "cleared" reads the same way everywhere in the adventure UI.
        private static readonly Color ClearedColor = new Color(0.30f, 0.80f, 0.45f);

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

        // Used only to restore CurrentAdventure when this scene is reached straight
        // from ResultScene's "back to adventure" button; see Start().
        [SerializeField] private AdventureDataSource dataSource;

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

            Adventure current = CurrentAdventure.Instance != null ? CurrentAdventure.Instance.Adventure : null;
            if (current != null)
            {
                Show(current);
                return;
            }

            // Reached directly from ResultScene's "Back to adventure" button:
            // CurrentAdventure was destroyed when GameScene loaded (it is bound to
            // AdventureScene / AdventuresScene only), so there is nothing to show yet.
            // SceneContext.AdventureId survives the scene changes; refetch and
            // reselect that adventure before building the map.
            if (SceneContext.AdventureId.HasValue && dataSource != null)
            {
                long adventureId = SceneContext.AdventureId.Value;
                dataSource.GetAdventures(adventures => RestoreAdventure(adventures, adventureId));
                return;
            }

            Show(null);
        }

        private void OnDestroy()
        {
            if (AdventureViewModel.Instance != null)
            {
                AdventureViewModel.Instance.CurrentState.OnStateChange -= OnAdventureStateChanged;
            }
        }

        private void RestoreAdventure(List<Adventure> adventures, long adventureId)
        {
            Adventure restored = adventures.Find(a => a.Id == adventureId);
            if (restored != null && CurrentAdventure.Instance != null)
            {
                CurrentAdventure.Instance.SetAdventure(restored);
            }
            Show(restored);
        }

        /// <summary>Rebuilds the whole map for <paramref name="adventure"/>.</summary>
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
                // The first stage that still has an unfinished unlocked scenario is
                // the one the player should land on by default.
                if (stageToSelect == null && stage.EffectiveState == State.ACTIVE)
                {
                    stageToSelect = stage;
                    currentIndex = i;
                }

                CreateNode(stage, i);
            }

            if (stageToSelect == null)
            {
                // Nothing is in progress: the adventure is either brand new (unreachable
                // in practice, since the first scenario is pre-activated) or fully
                // cleared. Land on the last stage instead of snapping back to the first.
                currentIndex = adventure.Stages.Count - 1;
                stageToSelect = adventure.Stages[currentIndex];
            }

            PlaceCurrentMarker(currentIndex);
            SelectStage(stageToSelect);
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

            State status = stage.EffectiveState;
            bool isPlayable = status != State.INACTIVE;
            Image background = nodeObject.GetComponentInChildren<Image>();
            TMP_Text label = nodeObject.GetComponentInChildren<TMP_Text>();

            Color nodeColor;
            switch (status)
            {
                case State.FINISHED:
                    nodeColor = ClearedColor;
                    break;
                case State.ACTIVE:
                    nodeColor = CurrentColor;
                    break;
                default:
                    nodeColor = LockedColor;
                    break;
            }
            background.color = nodeColor;

            // A cleared stage shows a check mark instead of its number so progress
            // reads at a glance; locked and current stages keep the stage number.
            label.text = status == State.FINISHED ? "✓" : (index + 1).ToString();

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
            if (stage.Scenarios.Count > 1)
            {
                // A stage holds several matches; show how far into it the player is.
                int cleared = stage.Scenarios.FindAll(s => s.State == State.FINISHED).Count;
                stageCaptionText.text += $" · {cleared}/{stage.Scenarios.Count}";
            }

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
            bool isPlayable = selectedStage != null && selectedStage.EffectiveState != State.INACTIVE;
            playButton.interactable = isPlayable && !isRequesting;
        }

        private void OnPlayClicked()
        {
            if (selectedStage == null || selectedStage.Scenarios.Count == 0)
            {
                return;
            }

            Scenario scenario = NextScenario(selectedStage);

            // CurrentAdventure and this whole scene are gone by the time the match
            // ends (GameScene isn't in CurrentAdventure's bound scenes), so
            // ResultScene needs its own copy of the adventure/stage context to route
            // back to the right map and show the right caption.
            SceneContext.AdventureId = currentAdventure.Id;
            SceneContext.AdventureScenarioId = scenario.Id;
            SceneContext.AdventureName = currentAdventure.Name;
            SceneContext.AdventureStageNumber = currentAdventure.Stages.IndexOf(selectedStage) + 1;
            SceneContext.AdventureStageScenarioCount = selectedStage.Scenarios.Count;
            SceneContext.AdventureStageClearedBeforeMatch =
                selectedStage.Scenarios.FindAll(s => s.State == State.FINISHED).Count;

            AdventureStoryOverlayUI.Play(scenario, () => AdventureViewModel.Instance.PlayPVE(scenario.Id));
        }

        /// <summary>
        /// The match the play button starts: the stage's first unlocked, unfinished
        /// scenario, so a stage of several matches is played through in order. A fully
        /// cleared stage replays its last scenario.
        /// </summary>
        private static Scenario NextScenario(Stage stage)
        {
            Scenario active = stage.Scenarios.Find(s => s.State == State.ACTIVE);
            return active ?? stage.Scenarios[stage.Scenarios.Count - 1];
        }

        private void OnAdventureStateChanged(AdventureViewModel.AdventureState state)
        {
            RefreshPlayButton();
        }
    }
}
