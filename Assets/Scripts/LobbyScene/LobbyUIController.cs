using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Data;
using Data.Deck;
using Data.Localization;
using Data.Magic;
using DeckScene;
using GameScene.Card;
using Global;
using Global.Util;
using RewardChest;
using RewardChest.Renderers;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Global.Serialization;

namespace LobbyScene
{
    public class LobbyUIController : MonoBehaviour
    {
        [SerializeField] LobbyUserNameUI lobbyUserNameUI;
        [SerializeField] private TMP_Dropdown deckDropdown;
        [SerializeField] private UnityEngine.UI.Button arrowButton;
        [SerializeField] private GameObject rewardUiPrefab;
        [SerializeField] private RewardTileView rewardTilePrefab;
        [SerializeField] private LobbySummonShowcase summonShowcase;
        [SerializeField] private Image avatarHeadImage;
        [SerializeField] private BattleHoverPresenter battleHoverPresenter;
        private static DeckResponseDto[] userDecks;

        /// <summary>
        /// Drops decks cached from the matching server, so the next lobby load refetches them.
        /// </summary>
        public static void ClearCachedDecks()
        {
            userDecks = null;
        }

        public LocalizedString deckLoadFailed;
        public LocalizedString noDecksAvailable;
        public LocalizedString deckSelectionFailed;
        public LocalizedString deckSelectionSuccess;
        public LocalizedString randomDeckPlay;
        
        private bool initializing = true;
        private LoadingHandle loadingHandle;

        private IEnumerator Start()
        {
            WDebug.Log("LobbyUIController Start");
            loadingHandle = LoadingPage.Begin(this);
            deckDropdown.onValueChanged.AddListener(OnDropdownChanged);
            yield return GameDataRefresh.Refresh();
            yield return LoadUserInfo();
        }
    
        private IEnumerator LoadUserInfo()
        {
            if (SceneContext.User == null)
            {
                yield return UserInfoGetter.GetUserInfo();
            }
        
            lobbyUserNameUI.SetUserName(SceneContext.User.name);
            ApplyOwnAppearance(SceneContext.User.appearance);
            yield return QuestRewardTracker.CheckAndShowRewards(rewardUiPrefab, rewardTilePrefab);
            yield return FetchDecks();
        }

        /// <summary>
        /// The profile avatar, the lobby character and its hover pose all come from one resolved set.
        /// When no complete set exists, including the default set, the sprites serialized in the scene stay.
        /// </summary>
        private void ApplyOwnAppearance(string appearance)
        {
            string loadedId = PlayerAppearanceResolver.Resolve(
                appearance,
                Resources.Load<Sprite>,
                out Sprite idle,
                out Sprite raised,
                out Sprite attacking);
            if (loadedId == null)
            {
                return;
            }

            if (avatarHeadImage != null)
            {
                avatarHeadImage.sprite = idle;
            }

            if (battleHoverPresenter != null)
            {
                battleHoverPresenter.SetPoseSprites(raised, attacking);
            }
        }

        public IEnumerator FetchDecks()
        {
            if (userDecks != null)
            {
                PopulateDropdown();
            }
        
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/decks";
            using var www = UnityWebRequest.Get(url);
            Server.SetAcceptLanguage(www);
            Server.SetAuthorization(www);
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                SystemMessageUI.Instance.ShowMessage(deckLoadFailed);
                WDebug.LogError($"덱 리스트 로드 실패: {www.error}");
                loadingHandle?.Dispose();
                SceneManager.LoadScene("LoginScene");
                yield break;
            }

            string body = www.downloadHandler.text;
            if (!JsonCodec.TryDeserialize(body, out userDecks, out string parseError))
            {
                WDebug.LogError($"덱 리스트 파싱 실패: {parseError} / {JsonCodec.Excerpt(body)}");
            }

            userDecks ??= Array.Empty<DeckResponseDto>();
        
            PopulateDropdown();
        
            LobbySceneViewModel.Instance.CheckIfInQueue();
        }

        // 2) 드랍다운 옵션 갱신
        private void PopulateDropdown()
        {
            // 옵션 이름만 뽑아서 리스트로
            string randomDeckName = randomDeckPlay.GetLocalizedString();
            var names = new List<string> { randomDeckName };
            names.AddRange(userDecks.Select(d => d.name));

            // 드랍다운 옵션 클리어 후 추가
            deckDropdown.ClearOptions();
            deckDropdown.AddOptions(names);
            
            // 현재 선택된 덱 인덱스 찾아 세팅
            int savedDeckIndex = userDecks
                .Select(d => d.id)
                .ToList()
                .IndexOf(SceneContext.User.selectedDeckId);

            int dropdownIndex;
            if (savedDeckIndex == -1 && userDecks.Length > 0)
            {
                StartCoroutine(SelectDeckCoroutine(userDecks[0].id));
                savedDeckIndex = 0;
            }

            if (savedDeckIndex >= 0)
            {
                dropdownIndex = savedDeckIndex + 1;
                LobbySceneViewModel.Instance.DeckMode = MatchDeckMode.Selected;
                DeckSceneContext.CurrentDeck = userDecks[savedDeckIndex];
            }
            else
            {
                dropdownIndex = 0;
                LobbySceneViewModel.Instance.DeckMode = MatchDeckMode.Random;
                DeckSceneContext.CurrentDeck = null;
            }

            deckDropdown.SetValueWithoutNotify(dropdownIndex);
            deckDropdown.RefreshShownValue();
            UpdateCaption(names[dropdownIndex]);
            ShowDeckSummons();
        
            loadingHandle?.Dispose();
            initializing = false;
        }

        // 3) 드랍다운에서 선택 바뀌었을 때
        public void OnDropdownChanged(int newIndex)
        {
            if (newIndex == 0)
            {
                LobbySceneViewModel.Instance.DeckMode = MatchDeckMode.Random;
                DeckSceneContext.CurrentDeck = null;
                UpdateCaption(randomDeckPlay.GetLocalizedString());
                ShowDeckSummons();
                WDebug.Log("랜덤 덱 플레이 선택");
                return;
            }

            var selected = userDecks[newIndex - 1];
            LobbySceneViewModel.Instance.DeckMode = MatchDeckMode.Selected;
            DeckSceneContext.CurrentDeck = selected;     // 컨텍스트 갱신
            WDebug.Log($"index: {newIndex} 선택된 덱: {selected.name} (ID: {selected.id})");
            UpdateCaption(selected.name);                // 상단 텍스트 갱신
            ShowDeckSummons();
            StartCoroutine(SelectDeckCoroutine(DeckSceneContext.CurrentDeck.id));
        }
        private IEnumerator SelectDeckCoroutine(long deckId)
        {
            string url = $"{ServerList.MatchingServer.url}/api/users/mine/decks/{deckId}";
            using var www = UnityWebRequest.Post(url, new WWWForm());
        
            Server.SetAuthorization(www);
            Server.SetAcceptLanguage(www);
        
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                SystemMessageUI.Instance.ShowMessage(deckSelectionFailed);
                WDebug.LogError($"덱 선택 실패: {www.responseCode} / {www.error}");
            }
            else
            {
                if (!initializing)
                    SystemMessageUI.Instance.ShowMessage(deckSelectionSuccess);
                WDebug.Log("덱 선택 성공: " + www.downloadHandler.text);
            }
        }
        // 플레이어 곁의 소환수 두 마리를 고른 덱의 유닛으로 바꾼다. 랜덤 덱이면 기본 소환수로 돌아간다.
        private void ShowDeckSummons()
        {
            if (summonShowcase != null)
            {
                summonShowcase.Show(DeckSceneContext.CurrentDeck);
            }
        }

        private void UpdateCaption(string deckName)
        {
            if (deckDropdown.captionText != null)
                deckDropdown.captionText.text = deckName;
        }
    }

    public static class QuestRewardTracker
    {
        private const string ChestHintObjectName = "ChestHint";

#if UNITY_EDITOR
        private const string RewardUiPrefabEditorPath = "Assets/Prefabs/UI/RewardUI.prefab";
#endif

        public static IEnumerator CheckAndShowRewards(GameObject rewardUiPrefab, RewardTileView rewardTilePrefab)
        {
            QuestRewardDto[] rewards = Array.Empty<QuestRewardDto>();
            yield return CheckRewards(result => rewards = result ?? Array.Empty<QuestRewardDto>());

            // A quest claimed by hand (the chest at the end of an adventure) already showed its rewards
            // on the claim screen. If the check ever reports it as well, do not show the same chest twice.
            rewards = Data.Quests.ClaimedQuestLedger.Session.WithoutClaimed(rewards);

            if (rewards.Length == 0)
            {
                yield break;
            }

            List<RewardView> views = QuestRewardPayload.ToRewardViews(rewards);
            if (!TryShowRewardUI(views, rewardUiPrefab, rewardTilePrefab))
            {
                ShowRewardMessage(rewards);
            }
        }

        private static IEnumerator CheckRewards(Action<QuestRewardDto[]> onSuccess)
        {
            var url = $"{ServerList.MatchingServer.url}/api/users/mine/quests/check";

            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            Server.SetAcceptLanguage(request);
            Server.SetAuthorization(request);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 404 || request.responseCode == 405)
                {
                    WDebug.Log("[CheckQuestRewards] endpoint is not ready yet. Skip reward visualization.");
                }
                else
                {
                    WDebug.LogError($"[CheckQuestRewards] fail: {request.responseCode} / {request.error}");
                }
                onSuccess?.Invoke(Array.Empty<QuestRewardDto>());
                yield break;
            }

            if (request.responseCode == 204 || string.IsNullOrWhiteSpace(request.downloadHandler.text))
            {
                onSuccess?.Invoke(Array.Empty<QuestRewardDto>());
                yield break;
            }

            try
            {
                onSuccess?.Invoke(QuestRewardPayload.Parse(request.downloadHandler.text));
            }
            catch (Exception e)
            {
                WDebug.LogError($"[CheckQuestRewards] parse error: {e}\n{request.downloadHandler.text}");
                onSuccess?.Invoke(Array.Empty<QuestRewardDto>());
            }
        }

        /// <summary>
        /// Every reward gets a tile drawn by its <see cref="IRewardTileRenderer"/>, including types this client
        /// does not know. A <c>CHEST</c> reward also adds a line pointing at the chest screen.
        /// </summary>
        private static bool TryShowRewardUI(
            IReadOnlyList<RewardView> rewards,
            GameObject rewardUiPrefab,
            RewardTileView rewardTilePrefab)
        {
            if (rewards.Count == 0)
            {
                return false;
            }

            var rewardUiInstance = ResolveRewardUIInstance(rewardUiPrefab);
            if (rewardUiInstance == null)
            {
                WDebug.LogWarning("[CheckQuestRewards] RewardUI could not be resolved.");
                return false;
            }

            rewardUiInstance.transform.localScale = Vector3.one;
            rewardUiInstance.SetActive(true);
            PopulateRewardUI(rewardUiInstance, rewards, rewardTilePrefab, RewardTileRenderers.CreateDefaultSelector());
            return true;
        }

        private static GameObject ResolveRewardUIInstance(GameObject rewardUiPrefab)
        {
            if (rewardUiPrefab != null)
            {
                if (rewardUiPrefab.scene.IsValid())
                {
                    return rewardUiPrefab;
                }

                return UnityEngine.Object.Instantiate(rewardUiPrefab);
            }

            var sceneRewardUi = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(gameObject => gameObject.name == "RewardUI" && gameObject.scene.IsValid());
            if (sceneRewardUi != null)
            {
                return sceneRewardUi;
            }

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RewardUiPrefabEditorPath);
            if (prefab != null)
            {
                return UnityEngine.Object.Instantiate(prefab);
            }
#endif

            return null;
        }

        private static void PopulateRewardUI(
            GameObject rewardUI,
            IReadOnlyList<RewardView> rewards,
            RewardTileView rewardTilePrefab,
            RewardTileRendererSelector selector)
        {
            var panel = RewardUiPanelLookup.FindRewardPanel(rewardUI.transform);

            var contentRoot = EnsureContentRoot(panel);
            ClearContent(contentRoot);

            bool hasChest = false;
            for (var i = 0; i < rewards.Count; i++)
            {
                RewardView reward = rewards[i];
                RewardTileContent content = selector.Build(reward);
                hasChest |= reward.Type == RewardTypes.Chest;

                if (rewardTilePrefab != null)
                {
                    RewardTileView tile = UnityEngine.Object.Instantiate(rewardTilePrefab, contentRoot);
                    tile.gameObject.SetActive(true);
                    tile.Render(reward, content);
                    continue;
                }

                CreateRewardItem(contentRoot, reward, content, i);
            }

            ShowChestHint(panel, hasChest);
        }

        private static RectTransform EnsureContentRoot(Transform panel)
        {
            var contentRoot = panel.Find("RewardContent") as RectTransform;
            if (contentRoot != null)
            {
                return contentRoot;
            }

            var contentObject = new GameObject("RewardContent", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentRoot = contentObject.GetComponent<RectTransform>();
            contentRoot.SetParent(panel, false);
            contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
            contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            contentRoot.pivot = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = new Vector2(0f, -30f);
            contentRoot.sizeDelta = new Vector2(430f, 230f);

            var layout = contentObject.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 14f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(6, 6, 6, 6);

            return contentRoot;
        }

        private static void ClearContent(Transform contentRoot)
        {
            for (var childIndex = contentRoot.childCount - 1; childIndex >= 0; childIndex--)
            {
                UnityEngine.Object.Destroy(contentRoot.GetChild(childIndex).gameObject);
            }
        }

        /// <summary>Used only when the scene has no RewardTile prefab wired: icon and amount, built in code.</summary>
        private static void CreateRewardItem(Transform contentRoot, RewardView reward, RewardTileContent content, int index)
        {
            var itemObject = new GameObject(
                $"{reward.Type}_{reward.Id}_{index}",
                typeof(RectTransform),
                typeof(LayoutElement));

            var itemRect = itemObject.GetComponent<RectTransform>();
            itemRect.SetParent(contentRoot, false);
            itemRect.sizeDelta = new Vector2(120f, 170f);

            var itemLayout = itemObject.GetComponent<LayoutElement>();
            itemLayout.preferredWidth = 120f;
            itemLayout.preferredHeight = 170f;

            var imageObject = new GameObject("Image", typeof(RectTransform), typeof(Image));
            var imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(itemRect, false);
            imageRect.anchorMin = new Vector2(0.5f, 1f);
            imageRect.anchorMax = new Vector2(0.5f, 1f);
            imageRect.pivot = new Vector2(0.5f, 1f);
            imageRect.anchoredPosition = new Vector2(0f, -8f);
            imageRect.sizeDelta = new Vector2(104f, 104f);

            var image = imageObject.GetComponent<Image>();
            image.sprite = content?.Icon;
            image.preserveAspect = true;

            var amountObject = new GameObject("Amount", typeof(RectTransform), typeof(TextMeshProUGUI));
            var amountRect = amountObject.GetComponent<RectTransform>();
            amountRect.SetParent(itemRect, false);
            amountRect.anchorMin = new Vector2(0f, 0f);
            amountRect.anchorMax = new Vector2(1f, 0f);
            amountRect.pivot = new Vector2(0.5f, 0f);
            amountRect.anchoredPosition = new Vector2(0f, 8f);
            amountRect.sizeDelta = new Vector2(0f, 42f);

            var amountText = amountObject.GetComponent<TextMeshProUGUI>();
            amountText.text = $"x{reward.Amount}";
            amountText.alignment = TextAlignmentOptions.Center;
            amountText.fontSize = 30f;
            amountText.color = Color.black;
        }

        /// <summary>
        /// A chest is not usable from the lobby, so the popup says where to open it. The line copies the
        /// popup title's font, which carries the Hangul fallback.
        /// </summary>
        private static void ShowChestHint(Transform panel, bool visible)
        {
            var hint = panel.Find(ChestHintObjectName);
            if (hint == null)
            {
                if (!visible)
                {
                    return;
                }

                hint = CreateChestHint(panel);
            }

            hint.gameObject.SetActive(visible);
            if (visible)
            {
                ApplyChestHintText(hint.GetComponent<TMP_Text>());
            }
        }

        private static Transform CreateChestHint(Transform panel)
        {
            var hintObject = new GameObject(ChestHintObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            var hintRect = hintObject.GetComponent<RectTransform>();
            hintRect.SetParent(panel, false);
            hintRect.anchorMin = new Vector2(0.5f, 0.5f);
            hintRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintRect.pivot = new Vector2(0.5f, 0.5f);
            hintRect.anchoredPosition = new Vector2(0f, -140f);
            hintRect.sizeDelta = new Vector2(440f, 26f);

            var hintText = hintObject.GetComponent<TextMeshProUGUI>();
            var title = panel.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                hintText.font = title.font;
            }

            hintText.alignment = TextAlignmentOptions.Center;
            hintText.enableAutoSizing = true;
            hintText.fontSizeMin = 9f;
            hintText.fontSizeMax = 16f;
            hintText.color = new Color32(0x1C, 0x1A, 0x2B, 0xFF);
            hintText.raycastTarget = false;
            return hintRect;
        }

        private static async void ApplyChestHintText(TMP_Text hintText)
        {
            if (hintText == null)
            {
                return;
            }

            hintText.text = "You got a chest! Open it on the chest screen.";
            string localized = await LocaleUtils.GetStringAsync(RewardNameKeys.Table, "ChestGotChest");
            if (hintText != null && !string.IsNullOrEmpty(localized))
            {
                hintText.text = localized;
            }
        }

        private static void ShowRewardMessage(QuestRewardDto[] rewards)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Quest rewards received:");

            foreach (var reward in rewards)
            {
                var rewardType = QuestRewardPayload.GetRewardType(reward);
                var rewardId = QuestRewardPayload.GetRewardId(reward);
                var amount = QuestRewardPayload.GetAmount(reward);
                builder.Append("- ").Append(rewardType).Append(" #").Append(rewardId).Append(" x").Append(amount);
                if (reward.questId > 0)
                {
                    builder.Append(" (quest ").Append(reward.questId).Append(")");
                }
                builder.AppendLine();
            }

            if (SystemMessageUI.Instance != null)
            {
                SystemMessageUI.Instance.ShowMessage(builder.ToString().TrimEnd());
                return;
            }

            WDebug.Log(builder.ToString());
        }
    }
}
