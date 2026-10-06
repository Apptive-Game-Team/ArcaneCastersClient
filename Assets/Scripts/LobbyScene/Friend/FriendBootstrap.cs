using System;
using System.Collections.Generic;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyScene
{
    public class FriendBootstrap : MonoBehaviour
    {
        private static FriendBootstrap instance;
        private GameObject modalRoot;
        private TMP_Text titleText;
        private Transform contentContainer;
        private TMP_InputField searchInput;
        private string currentTab = "search";

        // Cached UI Sprites
        private static Sprite flatCardSprite;
        private static Sprite flatButtonSprite;
        private static Sprite flatPillSprite;
        private static Sprite flatChipSprite;
        private static Sprite iconCloseSprite;
        private static Sprite iconSearchSprite;
        private static TMP_FontAsset defaultFont;

        private static void LoadSprites()
        {
            if (flatCardSprite == null) flatCardSprite = Resources.Load<Sprite>("UI/Flat/FlatCard");
            if (flatButtonSprite == null) flatButtonSprite = Resources.Load<Sprite>("UI/Flat/FlatButton");
            if (flatPillSprite == null) flatPillSprite = Resources.Load<Sprite>("UI/Flat/FlatPill");
            if (flatChipSprite == null) flatChipSprite = Resources.Load<Sprite>("UI/Flat/FlatChip");
            if (iconCloseSprite == null) iconCloseSprite = Resources.Load<Sprite>("UI/Flat/IconClose");
            if (iconSearchSprite == null) iconSearchSprite = Resources.Load<Sprite>("UI/Flat/IconSearch");
        }

        public static void Attach(Transform pill)
        {
            if (pill == null) return;
            LoadSprites();

            Transform canvasTransform = pill.root;
            if (instance == null)
            {
                GameObject bootstrapGo = new GameObject("FriendBootstrap");
                bootstrapGo.transform.SetParent(canvasTransform, false);
                instance = bootstrapGo.AddComponent<FriendBootstrap>();
                instance.EnsureFriendManager();
            }

            // Capture font from UserNameText if available
            Transform textChild = pill.Find("UserNameText");
            if (textChild != null)
            {
                TextMeshProUGUI existingTmp = textChild.GetComponent<TextMeshProUGUI>();
                if (existingTmp != null)
                {
                    defaultFont = existingTmp.font;
                }
            }

            instance.CreateFriendPillButton(pill);
        }

        private void EnsureFriendManager()
        {
            if (FriendManager.Instance == null)
            {
                gameObject.AddComponent<FriendApiClient>();
                gameObject.AddComponent<FriendEventStream>();
                gameObject.AddComponent<FriendManager>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleModal();
            }
        }

        private void CreateFriendPillButton(Transform pill)
        {
            if (pill.parent == null) return;

            Transform existing = pill.parent.Find("FriendPillButton");
            if (existing != null) return;

            GameObject friendBtnObj = new GameObject("FriendPillButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UnityEngine.UI.Button));
            friendBtnObj.transform.SetParent(pill.parent, false);

            RectTransform pillRect = pill.GetComponent<RectTransform>();
            RectTransform btnRect = friendBtnObj.GetComponent<RectTransform>();

            btnRect.anchorMin = pillRect.anchorMin;
            btnRect.anchorMax = pillRect.anchorMax;
            btnRect.pivot = pillRect.pivot;
            btnRect.sizeDelta = new Vector2(120f, pillRect.sizeDelta.y > 0 ? pillRect.sizeDelta.y : 34f);
            btnRect.anchoredPosition = pillRect.anchoredPosition + new Vector2(pillRect.sizeDelta.x + 10f, 0f);

            Image img = friendBtnObj.GetComponent<Image>();
            img.sprite = flatPillSprite ?? pill.GetComponent<Image>()?.sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 4f;
            img.color = new Color(0.18f, 0.22f, 0.35f, 1f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(friendBtnObj.transform, false);
            TextMeshProUGUI label = textObj.GetComponent<TextMeshProUGUI>();
            if (defaultFont != null) label.font = defaultFont;
            label.text = "친구 (F)";
            label.fontSize = 16;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            UnityEngine.UI.Button btn = friendBtnObj.GetComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(ToggleModal);
        }

        public void ToggleModal()
        {
            if (modalRoot != null && modalRoot.activeSelf)
            {
                modalRoot.SetActive(false);
                return;
            }

            OpenModal();
        }

        public void OpenModal()
        {
            if (modalRoot == null)
            {
                BuildModalUI();
            }

            modalRoot.SetActive(true);
            ShowSearchTab();
        }

        private void BuildModalUI()
        {
            LoadSprites();
            Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Modal Dim Background
            modalRoot = new GameObject("FriendModalOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            modalRoot.transform.SetParent(canvas.transform, false);
            RectTransform overlayRect = modalRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            modalRoot.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.1f, 0.75f);

            // Modal Card Window
            GameObject window = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            window.transform.SetParent(modalRoot.transform, false);
            RectTransform winRect = window.GetComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.5f, 0.5f);
            winRect.anchorMax = new Vector2(0.5f, 0.5f);
            winRect.pivot = new Vector2(0.5f, 0.5f);
            winRect.sizeDelta = new Vector2(760f, 560f);

            Image winImg = window.GetComponent<Image>();
            winImg.sprite = flatCardSprite;
            winImg.type = Image.Type.Sliced;
            winImg.pixelsPerUnitMultiplier = 4f;
            winImg.color = new Color(0.12f, 0.14f, 0.22f, 0.98f);

            // Header Bar
            GameObject header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(window.transform, false);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 60f);
            headerRect.anchoredPosition = Vector2.zero;

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            titleText = titleObj.GetComponent<TextMeshProUGUI>();
            if (defaultFont != null) titleText.font = defaultFont;
            titleText.text = "친구 관리";
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.95f, 0.96f, 1f);
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0.6f, 1f);
            titleRect.anchoredPosition = new Vector2(30f, 0f);

            // Close Button [X]
            CreateStyledButton(header.transform, "X", new Vector2(320f, -5f), new Vector2(36f, 36f), () => modalRoot.SetActive(false), new Color(0.7f, 0.2f, 0.25f), 16, iconCloseSprite);

            // Tabs Row
            GameObject tabsRow = new GameObject("TabsRow", typeof(RectTransform));
            tabsRow.transform.SetParent(window.transform, false);
            RectTransform tabsRect = tabsRow.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = new Vector2(1f, 1f);
            tabsRect.pivot = new Vector2(0.5f, 1f);
            tabsRect.sizeDelta = new Vector2(-60f, 44f);
            tabsRect.anchoredPosition = new Vector2(0f, -60f);

            CreateStyledButton(tabsRow.transform, "친구 검색", new Vector2(-220f, 0f), new Vector2(180f, 42f), ShowSearchTab, new Color(0.28f, 0.42f, 0.85f), 17);
            CreateStyledButton(tabsRow.transform, "친구 목록", new Vector2(0f, 0f), new Vector2(180f, 42f), ShowFriendsTab, new Color(0.18f, 0.23f, 0.35f), 17);
            CreateStyledButton(tabsRow.transform, "친구 요청", new Vector2(220f, 0f), new Vector2(180f, 42f), ShowRequestsTab, new Color(0.18f, 0.23f, 0.35f), 17);

            // Scrollable Content
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(window.transform, false);
            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.sizeDelta = new Vector2(-60f, -140f);
            scrollRect.anchoredPosition = new Vector2(0f, -60f);

            Image scrollBg = scrollObj.GetComponent<Image>();
            scrollBg.sprite = flatCardSprite;
            scrollBg.type = Image.Type.Sliced;
            scrollBg.pixelsPerUnitMultiplier = 4f;
            scrollBg.color = new Color(0.08f, 0.1f, 0.16f, 0.95f);

            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform cRect = contentObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
            sr.content = cRect;
            sr.horizontal = false;
            sr.vertical = true;

            contentContainer = contentObj.transform;
        }

        #region Search Tab

        public void ShowSearchTab()
        {
            currentTab = "search";
            if (titleText != null) titleText.text = "친구 검색 및 추가";
            ClearContent();

            // Search Bar Card
            GameObject searchBar = new GameObject("SearchBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            searchBar.transform.SetParent(contentContainer, false);
            searchBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);

            HorizontalLayoutGroup hlg = searchBar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlWidth = false;

            // Input Field with FlatCard Background
            GameObject inputGo = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(searchBar.transform, false);
            RectTransform inputRect = inputGo.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(510f, 48f);

            Image inputImg = inputGo.GetComponent<Image>();
            inputImg.sprite = flatButtonSprite ?? flatCardSprite;
            inputImg.type = Image.Type.Sliced;
            inputImg.pixelsPerUnitMultiplier = 4f;
            inputImg.color = new Color(0.16f, 0.2f, 0.3f, 1f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(inputGo.transform, false);
            TextMeshProUGUI inputText = textGo.GetComponent<TextMeshProUGUI>();
            if (defaultFont != null) inputText.font = defaultFont;
            inputText.fontSize = 18;
            inputText.color = Color.white;
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-24f, 0f);

            searchInput = inputGo.GetComponent<TMP_InputField>();
            searchInput.textComponent = inputText;

            GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGo.transform.SetParent(inputGo.transform, false);
            TextMeshProUGUI placeholderText = placeholderGo.GetComponent<TextMeshProUGUI>();
            if (defaultFont != null) placeholderText.font = defaultFont;
            placeholderText.text = "닉네임 또는 이메일로 검색...";
            placeholderText.fontSize = 17;
            placeholderText.color = new Color(0.6f, 0.65f, 0.75f, 0.6f);
            RectTransform phRect = placeholderGo.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = new Vector2(-24f, 0f);
            searchInput.placeholder = placeholderText;

            CreateStyledButton(searchBar.transform, "검색", Vector2.zero, new Vector2(120f, 48f), OnClickSearch, new Color(0.2f, 0.6f, 0.45f), 18);

            CreateLabel(contentContainer, "친구의 닉네임이나 이메일을 입력한 뒤 [검색]을 눌러 요청을 보내보세요.", 15, new Color(0.65f, 0.7f, 0.8f));
        }

        private void OnClickSearch()
        {
            if (searchInput == null || FriendManager.Instance == null) return;
            string query = searchInput.text?.Trim();
            if (string.IsNullOrEmpty(query)) return;

            ClearSearchResultsOnly();
            CreateLabel(contentContainer, $"'{query}' 검색 중...", 17, new Color(0.9f, 0.75f, 0.3f), "SearchStatus");

            StartCoroutine(FriendManager.Instance.ApiClient.SearchMembers(query, (success, results) =>
            {
                DestroyExisting("SearchStatus");

                if (!success || results == null || results.Count == 0)
                {
                    CreateLabel(contentContainer, "검색 결과가 없습니다.", 17, new Color(0.7f, 0.75f, 0.85f), "SearchResultRow");
                    return;
                }

                foreach (var user in results)
                {
                    BuildSearchResultRow(user);
                }
            }));
        }

        private void BuildSearchResultRow(FriendSearchResult user)
        {
            GameObject row = CreateItemCard("SearchResultRow", 56f);
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 8, 8);
            hlg.spacing = 15;
            hlg.childControlWidth = false;

            CreateLabel(row.transform, $"{user.name}  ({user.email})", 18, Color.white);

            if (user.isFriend)
            {
                CreateLabel(row.transform, "이미 친구입니다", 15, new Color(0.4f, 0.85f, 0.5f));
            }
            else if (user.hasPendingRequest)
            {
                CreateLabel(row.transform, "요청 대기중", 15, new Color(0.95f, 0.8f, 0.3f));
            }
            else
            {
                CreateStyledButton(row.transform, "친구 요청", Vector2.zero, new Vector2(110f, 38f), () =>
                {
                    StartCoroutine(FriendManager.Instance.ApiClient.SendFriendRequest(user.name, (ok, _) =>
                    {
                        if (ok) OnClickSearch();
                    }));
                }, new Color(0.28f, 0.45f, 0.9f), 15);
            }
        }

        #endregion

        #region Friends Tab

        public void ShowFriendsTab()
        {
            currentTab = "friends";
            if (titleText != null) titleText.text = "친구 목록";
            ClearContent();

            CreateLabel(contentContainer, "친구 목록 불러오는 중...", 17, new Color(0.9f, 0.75f, 0.3f), "LoadingFriends");

            if (FriendManager.Instance == null) return;
            StartCoroutine(FriendManager.Instance.ApiClient.GetFriends((success, friends) =>
            {
                DestroyExisting("LoadingFriends");

                if (!success || friends == null || friends.Count == 0)
                {
                    CreateLabel(contentContainer, "등록된 친구가 없습니다. [친구 검색]에서 친구를 추가해보세요!", 16, new Color(0.65f, 0.7f, 0.8f));
                    return;
                }

                foreach (var friend in friends)
                {
                    BuildFriendRow(friend);
                }
            }));
        }

        private void BuildFriendRow(FriendSummary friend)
        {
            GameObject row = CreateItemCard("FriendRow", 56f);
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 8, 8);
            hlg.spacing = 15;
            hlg.childControlWidth = false;

            bool isOnline = friend.status != null && friend.status.Equals("Online", StringComparison.OrdinalIgnoreCase);
            Color statusColor = isOnline ? new Color(0.3f, 0.9f, 0.45f) : new Color(0.55f, 0.6f, 0.7f);

            CreateLabel(row.transform, $"{friend.name}  [{FormatStatus(friend.status)}]  MMR {friend.mmr}", 18, statusColor);

            if (isOnline)
            {
                CreateStyledButton(row.transform, "친선전 초대", Vector2.zero, new Vector2(110f, 38f), () =>
                {
                    StartCoroutine(FriendManager.Instance.ApiClient.InviteFriend(friend.userId, (ok, _) => { }));
                }, new Color(0.2f, 0.65f, 0.4f), 15);
            }

            CreateStyledButton(row.transform, "삭제", Vector2.zero, new Vector2(70f, 38f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.DeleteFriend(friend.userId, ok =>
                {
                    if (ok) ShowFriendsTab();
                }));
            }, new Color(0.7f, 0.25f, 0.25f), 15);
        }

        #endregion

        #region Requests Tab

        public void ShowRequestsTab()
        {
            currentTab = "requests";
            if (titleText != null) titleText.text = "친구 요청 관리";
            ClearContent();

            CreateLabel(contentContainer, "받은 친구 요청", 20, new Color(0.4f, 0.75f, 1f));

            if (FriendManager.Instance == null) return;
            StartCoroutine(FriendManager.Instance.ApiClient.GetReceivedRequests((success, reqs) =>
            {
                if (success && reqs != null && reqs.Count > 0)
                {
                    foreach (var req in reqs)
                    {
                        BuildReceivedRequestRow(req);
                    }
                }
                else
                {
                    CreateLabel(contentContainer, "받은 친구 요청이 없습니다.", 15, new Color(0.6f, 0.65f, 0.75f));
                }

                CreateLabel(contentContainer, "\n보낸 친구 요청", 20, new Color(0.4f, 0.75f, 1f));
                StartCoroutine(FriendManager.Instance.ApiClient.GetSentRequests((sentOk, sentReqs) =>
                {
                    if (sentOk && sentReqs != null && sentReqs.Count > 0)
                    {
                        foreach (var req in sentReqs)
                        {
                            BuildSentRequestRow(req);
                        }
                    }
                    else
                    {
                        CreateLabel(contentContainer, "보낸 친구 요청이 없습니다.", 15, new Color(0.6f, 0.65f, 0.75f));
                    }
                }));
            }));
        }

        private void BuildReceivedRequestRow(FriendRequestItem req)
        {
            GameObject row = CreateItemCard("ReceivedReqRow", 52f);
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 6, 6);
            hlg.spacing = 15;
            hlg.childControlWidth = false;

            CreateLabel(row.transform, $"{req.senderName} 님의 친구 요청", 17, Color.white);

            CreateStyledButton(row.transform, "수락", Vector2.zero, new Vector2(80f, 36f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.AcceptFriendRequest(req.id, (ok, _) =>
                {
                    if (ok) ShowRequestsTab();
                }));
            }, new Color(0.2f, 0.65f, 0.4f), 15);

            CreateStyledButton(row.transform, "거절", Vector2.zero, new Vector2(80f, 36f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.RejectFriendRequest(req.id, (ok, _) =>
                {
                    if (ok) ShowRequestsTab();
                }));
            }, new Color(0.7f, 0.25f, 0.25f), 15);
        }

        private void BuildSentRequestRow(FriendRequestItem req)
        {
            GameObject row = CreateItemCard("SentReqRow", 52f);
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 6, 6);
            hlg.spacing = 15;
            hlg.childControlWidth = false;

            CreateLabel(row.transform, $"{req.receiverName} 님에게 보낸 요청 (대기중)", 17, new Color(0.85f, 0.88f, 0.95f));

            CreateStyledButton(row.transform, "취소", Vector2.zero, new Vector2(80f, 36f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.CancelFriendRequest(req.id, ok =>
                {
                    if (ok) ShowRequestsTab();
                }));
            }, new Color(0.45f, 0.5f, 0.6f), 15);
        }

        #endregion

        #region UI Helpers

        private static GameObject CreateItemCard(string name, float height)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
            Image img = card.GetComponent<Image>();
            img.sprite = flatChipSprite ?? flatCardSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 4f;
            img.color = new Color(0.16f, 0.19f, 0.28f, 0.95f);
            return card;
        }

        private static GameObject CreateStyledButton(
            Transform parent,
            string text,
            Vector2 pos,
            Vector2 size,
            Action onClick,
            Color bgColor,
            int fontSize = 16,
            Sprite icon = null)
        {
            GameObject btnObj = new GameObject(text + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UnityEngine.UI.Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            Image img = btnObj.GetComponent<Image>();
            img.sprite = flatButtonSprite ?? flatPillSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 4f;
            img.color = bgColor;

            GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI lbl = tObj.GetComponent<TextMeshProUGUI>();
            if (defaultFont != null) lbl.font = defaultFont;
            lbl.text = text;
            lbl.fontSize = fontSize;
            lbl.fontStyle = FontStyles.Bold;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = Color.white;

            RectTransform tRt = tObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            UnityEngine.UI.Button btn = btnObj.GetComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btnObj;
        }

        private static GameObject CreateLabel(Transform parent, string text, int fontSize, Color color, string name = "Label")
        {
            GameObject lblObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            lblObj.transform.SetParent(parent, false);
            TextMeshProUGUI lbl = lblObj.GetComponent<TextMeshProUGUI>();
            if (defaultFont != null) lbl.font = defaultFont;
            lbl.text = text;
            lbl.fontSize = fontSize;
            lbl.color = color;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            return lblObj;
        }

        private static string FormatStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "오프라인";
            return status.ToUpperInvariant() switch
            {
                "ONLINE" => "온라인",
                "ONMATCHING" => "매칭 중",
                "ONPLAYING" => "게임 중",
                "OFFLINE" => "오프라인",
                _ => status
            };
        }

        private void ClearContent()
        {
            if (contentContainer == null) return;
            for (int i = contentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(contentContainer.GetChild(i).gameObject);
            }
        }

        private void ClearSearchResultsOnly()
        {
            if (contentContainer == null) return;
            for (int i = contentContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = contentContainer.GetChild(i);
                if (child.name.StartsWith("SearchResultRow") || child.name.StartsWith("SearchStatus"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void DestroyExisting(string objName)
        {
            if (contentContainer == null) return;
            Transform t = contentContainer.Find(objName);
            if (t != null) Destroy(t.gameObject);
        }

        #endregion
    }
}
