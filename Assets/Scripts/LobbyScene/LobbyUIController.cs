using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Data;
using Data.Deck;
using Data.Magic;
using DeckScene;
using GameScene.Card;
using Global;
using Global.Util;
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
            yield return QuestRewardTracker.CheckAndShowRewards(rewardUiPrefab);
            yield return FetchDecks();
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
                WDebug.Log("랜덤 덱 플레이 선택");
                return;
            }

            var selected = userDecks[newIndex - 1];
            LobbySceneViewModel.Instance.DeckMode = MatchDeckMode.Selected;
            DeckSceneContext.CurrentDeck = selected;     // 컨텍스트 갱신
            WDebug.Log($"index: {newIndex} 선택된 덱: {selected.name} (ID: {selected.id})");
            UpdateCaption(selected.name);                // 상단 텍스트 갱신
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
        private void UpdateCaption(string deckName)
        {
            if (deckDropdown.captionText != null)
                deckDropdown.captionText.text = deckName;
        }
    }

    public static class QuestRewardTracker
    {
        private const string RewardTypeCard = "CARD";
        private const string RewardTypeDecoration = "DECORATION";
        private const string RewardTypeMagic = "MAGIC";

#if UNITY_EDITOR
        private const string RewardUiPrefabEditorPath = "Assets/Prefabs/UI/RewardUI.prefab";
        private const string DecorationDbEditorPath = "Assets/Scripts/CustomizeScene/New Decoration Database.asset";
#endif

        private sealed class RewardVisual
        {
            public string RewardType { get; }
            public long RewardId { get; }
            public int Amount { get; }
            public Sprite Sprite { get; }

            public RewardVisual(string rewardType, long rewardId, int amount, Sprite sprite)
            {
                RewardType = rewardType;
                RewardId = rewardId;
                Amount = amount;
                Sprite = sprite;
            }
        }

        [Serializable]
        public class QuestRewardDto
        {
            public string rewardType;
            public long rewardId;
            public int amount;
            public long questId;

            // Compatibility fields for temporary backend naming differences.
            public string type;
            public long id;
            public int value;
        }

        [Serializable]
        private class QuestRewardResponseDto
        {
            public QuestRewardDto[] rewards;
        }

        public static IEnumerator CheckAndShowRewards(GameObject rewardUiPrefab)
        {
            QuestRewardDto[] rewards = Array.Empty<QuestRewardDto>();
            yield return CheckRewards(result => rewards = result ?? Array.Empty<QuestRewardDto>());

            if (rewards.Length == 0)
            {
                yield break;
            }

            if (!TryShowRewardUI(rewards, rewardUiPrefab))
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
                onSuccess?.Invoke(ParseRewards(request.downloadHandler.text));
            }
            catch (Exception e)
            {
                WDebug.LogError($"[CheckQuestRewards] parse error: {e}\n{request.downloadHandler.text}");
                onSuccess?.Invoke(Array.Empty<QuestRewardDto>());
            }
        }

        private static QuestRewardDto[] ParseRewards(string json)
        {
            var trimmed = json.TrimStart();
            if (trimmed.StartsWith("["))
            {
                return JsonCodec.Deserialize<QuestRewardDto[]>(json) ?? Array.Empty<QuestRewardDto>();
            }

            var response = JsonCodec.Deserialize<QuestRewardResponseDto>(json);
            return response?.rewards ?? Array.Empty<QuestRewardDto>();
        }

        private static bool TryShowRewardUI(
            QuestRewardDto[] rewards,
            GameObject rewardUiPrefab)
        {
            var visuals = new List<RewardVisual>();
            foreach (var reward in rewards)
            {
                var rewardType = GetRewardType(reward).ToUpperInvariant();
                var rewardId = GetRewardId(reward);
                var amount = Mathf.Max(1, GetAmount(reward));

                if (TryResolveSprite(rewardType, rewardId, out var sprite))
                {
                    visuals.Add(new RewardVisual(rewardType, rewardId, amount, sprite));
                }
            }

            if (visuals.Count == 0)
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
            PopulateRewardUI(rewardUiInstance, visuals);
            return true;
        }

        private static bool TryResolveSprite(
            string rewardType,
            long rewardId,
            out Sprite sprite)
        {
            sprite = null;

            // 카드 한 장이 마법 하나이므로 CARD 와 MAGIC 은 둘 다 magics.id 를 가리킨다.
            switch (rewardType)
            {
                case RewardTypeCard:
                case RewardTypeMagic:
                    return TryResolveMagicSprite(rewardId, out sprite);
                default:
                    return false;
            }
        }

        private static bool TryResolveMagicSprite(long rewardId, out Sprite sprite)
        {
            sprite = LocalCombinedMagicData.GetEffectiveDataList()
                .FirstOrDefault(magicData => magicData.id == rewardId)
                ?.GetSprite();
            return sprite != null;
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

        private static void PopulateRewardUI(GameObject rewardUI, IReadOnlyList<RewardVisual> visuals)
        {
            var panel = rewardUI.transform.Find("Panal");
            if (panel == null)
            {
                panel = rewardUI.transform;
            }

            var contentRoot = EnsureContentRoot(panel);
            ClearContent(contentRoot);

            for (var i = 0; i < visuals.Count; i++)
            {
                CreateRewardItem(contentRoot, visuals[i], i);
            }
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

        private static void CreateRewardItem(Transform contentRoot, RewardVisual visual, int index)
        {
            var itemObject = new GameObject(
                $"{visual.RewardType}_{visual.RewardId}_{index}",
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
            image.sprite = visual.Sprite;
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
            amountText.text = $"x{visual.Amount}";
            amountText.alignment = TextAlignmentOptions.Center;
            amountText.fontSize = 30f;
            amountText.color = Color.black;
        }

        private static void ShowRewardMessage(QuestRewardDto[] rewards)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Quest rewards received:");

            foreach (var reward in rewards)
            {
                var rewardType = GetRewardType(reward);
                var rewardId = GetRewardId(reward);
                var amount = GetAmount(reward);
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

        private static string GetRewardType(QuestRewardDto reward)
        {
            if (!string.IsNullOrEmpty(reward.rewardType))
            {
                return reward.rewardType;
            }

            if (!string.IsNullOrEmpty(reward.type))
            {
                return reward.type;
            }

            return "UNKNOWN";
        }

        private static long GetRewardId(QuestRewardDto reward)
        {
            if (reward.rewardId > 0)
            {
                return reward.rewardId;
            }

            return reward.id;
        }

        private static int GetAmount(QuestRewardDto reward)
        {
            if (reward.amount > 0)
            {
                return reward.amount;
            }

            if (reward.value > 0)
            {
                return reward.value;
            }

            return 1;
        }
    }
}
