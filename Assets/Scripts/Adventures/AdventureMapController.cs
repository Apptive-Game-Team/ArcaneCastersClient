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
    /// <see cref="Data.Adventures.CurrentAdventure"/>: one round node per scenario
    /// (one match), spread evenly along the road inside the map card, plus the bottom
    /// info card and its Play button. A stage groups several scenarios on the server
    /// and in rewards, but the player sees and picks individual matches.
    /// </summary>
    public class AdventureMapController : MonoBehaviour
    {
        // Nodes sit on one horizontal line under the road (normalized yFromTop), spread
        // evenly between these x bounds however many scenarios the adventure has.
        private const float NodeRoadY = 0.70f;
        private const float NodeFirstX = 0.12f;
        private const float NodeLastX = 0.88f;

        /// <summary>One map node: a scenario with the stage it belongs to.</summary>
        private sealed class MapNode
        {
            public Stage Stage;
            public int StageNumber;
            public Scenario Scenario;
            public int ScenarioNumber;
        }

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
        private readonly List<MapNode> nodes = new List<MapNode>();
        private MapNode selectedNode;
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
            selectedNode = null;
            nodes.Clear();

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

            for (int stageIndex = 0; stageIndex < adventure.Stages.Count; stageIndex++)
            {
                Stage stage = adventure.Stages[stageIndex];
                for (int scenarioIndex = 0; scenarioIndex < stage.Scenarios.Count; scenarioIndex++)
                {
                    nodes.Add(new MapNode
                    {
                        Stage = stage,
                        StageNumber = stageIndex + 1,
                        Scenario = stage.Scenarios[scenarioIndex],
                        ScenarioNumber = scenarioIndex + 1,
                    });
                }
            }

            // Land on the first unlocked, unfinished match; when everything is cleared,
            // on the last one instead of snapping back to the start.
            int currentIndex = nodes.FindIndex(n => n.Scenario.State == State.ACTIVE);
            for (int i = 0; i < nodes.Count; i++)
            {
                CreateNode(nodes[i], i);
            }

            if (nodes.Count == 0)
            {
                infoCard.SetActive(false);
                return;
            }

            PlaceMarker(currentIndex >= 0 ? currentIndex : nodes.Count - 1, false);
            SelectNode(nodes[currentIndex >= 0 ? currentIndex : nodes.Count - 1]);
        }

        private const float MarkerMoveSeconds = 0.25f;
        private Coroutine markerMove;

        /// <summary>
        /// Puts the player's head marker above node <paramref name="index"/>. It starts on the
        /// current match and walks to whichever node the player selects, so the map shows where
        /// the Play button will take them.
        /// </summary>
        private void PlaceMarker(int index, bool animate)
        {
            if (currentMarker == null)
            {
                return;
            }

            if (index < 0)
            {
                currentMarker.gameObject.SetActive(false);
                return;
            }

            Vector2 waypoint = GetNodeWaypoint(index);
            Vector2 target = new Vector2(waypoint.x, 1f - waypoint.y);
            bool wasVisible = currentMarker.gameObject.activeSelf;
            currentMarker.gameObject.SetActive(true);

            if (markerMove != null)
            {
                StopCoroutine(markerMove);
                markerMove = null;
            }

            if (!animate || !wasVisible)
            {
                currentMarker.anchorMin = target;
                currentMarker.anchorMax = target;
                return;
            }

            markerMove = StartCoroutine(MoveMarker(currentMarker.anchorMin, target));
        }

        private System.Collections.IEnumerator MoveMarker(Vector2 from, Vector2 to)
        {
            float elapsed = 0f;
            while (elapsed < MarkerMoveSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / MarkerMoveSeconds));
                Vector2 anchor = Vector2.Lerp(from, to, t);
                currentMarker.anchorMin = anchor;
                currentMarker.anchorMax = anchor;
                yield return null;
            }
            markerMove = null;
        }

        private void CreateNode(MapNode node, int index)
        {
            GameObject nodeObject = Instantiate(stageNodePrefab, nodesContainer);
            var rect = (RectTransform)nodeObject.transform;
            Vector2 waypoint = GetNodeWaypoint(index);
            Vector2 anchor = new Vector2(waypoint.x, 1f - waypoint.y);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;

            State status = node.Scenario.State;
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

            // Every node shows its order number; the node color alone marks it cleared. A check
            // mark character is not in the game fonts (Lilita One, Jua) and rendered as a box.
            label.text = (index + 1).ToString();

            Button button = nodeObject.GetComponent<Button>();
            button.interactable = isPlayable;
            if (isPlayable)
            {
                button.onClick.AddListener(() => SelectNode(node));
            }
        }

        private Vector2 GetNodeWaypoint(int index)
        {
            if (nodes.Count <= 1)
            {
                return new Vector2((NodeFirstX + NodeLastX) / 2f, NodeRoadY);
            }

            float t = index / (float)(nodes.Count - 1);
            return new Vector2(Mathf.Lerp(NodeFirstX, NodeLastX, t), NodeRoadY);
        }

        private void SelectNode(MapNode node)
        {
            selectedNode = node;
            PlaceMarker(nodes.IndexOf(node), true);
            infoCard.SetActive(true);
            stageIconImage.sprite = currentAdventure.IconImage;

            // "Forest · 1-2": stage 1, its second match.
            stageCaptionText.text = $"{currentAdventure.Name} · {node.StageNumber}-{node.ScenarioNumber}";

            Stage stage = node.Stage;
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
            bool isPlayable = selectedNode != null && selectedNode.Scenario.State != State.INACTIVE;
            playButton.interactable = isPlayable && !isRequesting;
        }

        private void OnPlayClicked()
        {
            if (selectedNode == null)
            {
                return;
            }

            Scenario scenario = selectedNode.Scenario;

            // CurrentAdventure and this whole scene are gone by the time the match
            // ends (GameScene isn't in CurrentAdventure's bound scenes), so
            // ResultScene needs its own copy of the adventure/stage context to route
            // back to the right map and show the right caption.
            SceneContext.AdventureId = currentAdventure.Id;
            SceneContext.AdventureScenarioId = scenario.Id;
            SceneContext.AdventureName = currentAdventure.Name;
            SceneContext.AdventureStageNumber = selectedNode.StageNumber;
            SceneContext.AdventureScenarioNumber = selectedNode.ScenarioNumber;

            AdventureStoryOverlayUI.Play(scenario, () => AdventureViewModel.Instance.PlayPVE(scenario.Id));
        }

        private void OnAdventureStateChanged(AdventureViewModel.AdventureState state)
        {
            RefreshPlayButton();
        }
    }
}
