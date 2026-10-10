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

        // Tab Buttons
        private GameObject searchTabBtn;
        private GameObject friendsTabBtn;
        private GameObject requestsTabBtn;

        // Core Palette (Matches Lobby/SettingPage Theme)
        private static readonly Color InkColor = new Color32(0x1C, 0x1A, 0x2B, 0xFF);          // #1C1A2B - 메인 텍스트
        private static readonly Color SlateTextColor = new Color32(0x5B, 0x62, 0x75, 0xFF);     // #5B6275 - 보조 텍스트
        private static readonly Color MutedTextColor = new Color32(0x8E, 0x95, 0xA5, 0xFF);     // #8E95A5 - 플레이스홀더 / 안내
        private static readonly Color PrimaryOrange = new Color32(0xFF, 0x9A, 0x1F, 0xFF);      // #FF9A1F - 시그니처 앰버 오렌지
        private static readonly Color TabInactiveBg = new Color32(0xEE, 0xF3, 0xF8, 0xFF);      // #EEF3F8 - 비선택 탭 배경
        private static readonly Color CardBg = Color.white;                                     // 순백색 카드/윈도우 배경
        private static readonly Color ChipBg = new Color32(0xF4, 0xF7, 0xFA, 0xFF);             // #F4F7FA - 목록 아이템 카드 배경
        private static readonly Color InputBg = new Color32(0xEE, 0xF2, 0xF6, 0xFF);            // #EEF2F6 - 검색창 배경
        private static readonly Color TealColor = new Color32(0x2F, 0xB8, 0xA8, 0xFF);          // #2FB8A8 - 친선전 초대 / 온라인
        private static readonly Color DangerRed = new Color32(0xF0, 0x44, 0x3A, 0xFF);          // #F0443A - 삭제 / 거절
        private static readonly Color DimOverlay = new Color(0.04f, 0.07f, 0.12f, 0.62f);       // 모달 배경 딤

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
        }

        // Entry for the lobby menu's friend item (LobbyMenu.OpenFriends). The
        // bootstrap is created by LobbyUserNameUI.Awake before any click.
        public static void ToggleFriendModal()
        {
            if (instance == null)
            {
                Debug.LogWarning("[FriendBootstrap] Friend button pressed before FriendBootstrap.Attach ran.");
                return;
            }

            instance.ToggleModal();
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
            modalRoot.GetComponent<Image>().color = DimOverlay;

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
            winImg.color = CardBg;

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
            titleText.color = InkColor;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0.6f, 1f);
            titleRect.anchoredPosition = new Vector2(30f, 0f);

            // Close Button [X]
            CreateStyledButton(header.transform, "X", new Vector2(330f, -5f), new Vector2(36f, 36f), () => modalRoot.SetActive(false), TabInactiveBg, 16, iconCloseSprite, SlateTextColor);

            // Tabs Row
            GameObject tabsRow = new GameObject("TabsRow", typeof(RectTransform));
            tabsRow.transform.SetParent(window.transform, false);
            RectTransform tabsRect = tabsRow.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = new Vector2(1f, 1f);
            tabsRect.pivot = new Vector2(0.5f, 1f);
            tabsRect.sizeDelta = new Vector2(-60f, 44f);
            tabsRect.anchoredPosition = new Vector2(0f, -60f);

            searchTabBtn = CreateStyledButton(tabsRow.transform, "친구 검색", new Vector2(-220f, 0f), new Vector2(180f, 42f), ShowSearchTab, PrimaryOrange, 16, null, Color.white);
            friendsTabBtn = CreateStyledButton(tabsRow.transform, "친구 목록", new Vector2(0f, 0f), new Vector2(180f, 42f), ShowFriendsTab, TabInactiveBg, 16, null, SlateTextColor);
            requestsTabBtn = CreateStyledButton(tabsRow.transform, "친구 요청", new Vector2(220f, 0f), new Vector2(180f, 42f), ShowRequestsTab, TabInactiveBg, 16, null, SlateTextColor);

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
            scrollBg.color = new Color32(0xFA, 0xFB, 0xFC, 0xFF);

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

        #region Tab Helpers

        private void UpdateTabStyles()
        {
            SetTabBtnStyle(searchTabBtn, currentTab == "search");
            SetTabBtnStyle(friendsTabBtn, currentTab == "friends");
            SetTabBtnStyle(requestsTabBtn, currentTab == "requests");
        }

        private static void SetTabBtnStyle(GameObject btnObj, bool isSelected)
        {
            if (btnObj == null) return;
            Image img = btnObj.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? PrimaryOrange : TabInactiveBg;
            }
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.color = isSelected ? Color.white : SlateTextColor;
            }
        }

        #endregion

        #region Search Tab

        public void ShowSearchTab()
        {
            currentTab = "search";
            UpdateTabStyles();
            if (titleText != null) titleText.text = "친구 검색 및 추가";
            ClearContent();

            // Search Bar Card
            GameObject searchBar = new GameObject("SearchBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            searchBar.transform.SetParent(contentContainer, false);
            searchBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);

            HorizontalLayoutGroup hlg = searchBar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlWidth = false;

            // Input Field
            GameObject inputGo = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(searchBar.transform, false);
            RectTransform inputRect = inputGo.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(510f, 48f);

            Image inputImg = inputGo.GetComponent<Image>();
            inputImg.sprite = flatButtonSprite ?? flatCardSprite;
            inputImg.type = Image.Type.Sliced;
            inputImg.pixelsPerUnitMultiplier = 4f;
            inputImg.color = InputBg;

            // Text Area (Viewport with RectMask2D for clipping and drag handling)
            GameObject textAreaGo = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textAreaGo.transform.SetParent(inputGo.transform, false);
            RectTransform textAreaRect = textAreaGo.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = new Vector2(-24f, -10f);
            textAreaRect.anchoredPosition = Vector2.zero;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textAreaGo.transform, false);
            TextMeshProUGUI inputText = textGo.GetComponent<TextMeshProUGUI>();
            if (defaultFont != null) inputText.font = defaultFont;
            inputText.fontSize = 18;
            inputText.color = InkColor;
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGo.transform.SetParent(textAreaGo.transform, false);
            TextMeshProUGUI placeholderText = placeholderGo.GetComponent<TextMeshProUGUI>();
            if (defaultFont != null) placeholderText.font = defaultFont;
            placeholderText.text = "닉네임 또는 이메일로 검색...";
            placeholderText.fontSize = 17;
            placeholderText.color = MutedTextColor;
            RectTransform phRect = placeholderGo.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = Vector2.zero;
            phRect.anchoredPosition = Vector2.zero;

            searchInput = inputGo.GetComponent<TMP_InputField>();
            searchInput.textViewport = textAreaRect;
            searchInput.textComponent = inputText;
            searchInput.placeholder = placeholderText;
            searchInput.lineType = TMP_InputField.LineType.SingleLine;
            searchInput.onSubmit.AddListener(_ => OnClickSearch());

            CreateStyledButton(searchBar.transform, "검색", Vector2.zero, new Vector2(110f, 48f), OnClickSearch, PrimaryOrange, 17, null, Color.white);

            CreateLabel(contentContainer, "친구의 닉네임이나 이메일을 입력한 뒤 [검색]을 눌러 요청을 보내보세요.", 15, SlateTextColor);
        }

        private void OnClickSearch()
        {
            if (searchInput == null || FriendManager.Instance == null) return;
            string query = searchInput.text?.Trim();
            if (string.IsNullOrEmpty(query)) return;

            ClearSearchResultsOnly();
            CreateLabel(contentContainer, $"'{query}' 검색 중...", 16, PrimaryOrange, "SearchStatus");

            StartCoroutine(FriendManager.Instance.ApiClient.SearchMembers(query, (success, results) =>
            {
                DestroyExisting("SearchStatus");

                if (!success || results == null || results.Count == 0)
                {
                    CreateLabel(contentContainer, "검색 결과가 없습니다.", 16, SlateTextColor, "SearchResultRow");
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

            CreateLabel(row.transform, $"{user.name}  ({user.email})", 17, InkColor);

            if (user.isFriend)
            {
                CreateLabel(row.transform, "이미 친구입니다", 15, TealColor);
            }
            else if (user.hasPendingRequest)
            {
                CreateLabel(row.transform, "요청 대기중", 15, PrimaryOrange);
            }
            else
            {
                CreateStyledButton(row.transform, "친구 요청", Vector2.zero, new Vector2(110f, 38f), () =>
                {
                    StartCoroutine(FriendManager.Instance.ApiClient.SendFriendRequest(user.name, (ok, _) =>
                    {
                        if (ok) OnClickSearch();
                    }));
                }, PrimaryOrange, 15, null, Color.white);
            }
        }

        #endregion

        #region Friends Tab

        public void ShowFriendsTab()
        {
            currentTab = "friends";
            UpdateTabStyles();
            if (titleText != null) titleText.text = "친구 목록";
            ClearContent();

            CreateLabel(contentContainer, "친구 목록 불러오는 중...", 16, PrimaryOrange, "LoadingFriends");

            if (FriendManager.Instance == null) return;
            StartCoroutine(FriendManager.Instance.ApiClient.GetFriends((success, friends) =>
            {
                DestroyExisting("LoadingFriends");

                if (!success || friends == null || friends.Count == 0)
                {
                    CreateLabel(contentContainer, "등록된 친구가 없습니다. [친구 검색]에서 친구를 추가해보세요!", 16, SlateTextColor);
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
            bool isBusy = friend.status != null && (friend.status.Equals("OnMatching", StringComparison.OrdinalIgnoreCase) || friend.status.Equals("OnPlaying", StringComparison.OrdinalIgnoreCase));
            Color statusColor = isOnline ? TealColor : (isBusy ? PrimaryOrange : MutedTextColor);

            CreateLabel(row.transform, $"{friend.name}  MMR {friend.mmr}", 17, InkColor);
            CreateLabel(row.transform, $"[{FormatStatus(friend.status)}]", 15, statusColor);

            if (isOnline)
            {
                CreateStyledButton(row.transform, "친선전 초대", Vector2.zero, new Vector2(110f, 38f), () =>
                {
                    StartCoroutine(FriendManager.Instance.ApiClient.InviteFriend(friend.userId, (ok, _) => { }));
                }, TealColor, 15, null, Color.white);
            }

            CreateStyledButton(row.transform, "삭제", Vector2.zero, new Vector2(70f, 38f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.DeleteFriend(friend.userId, ok =>
                {
                    if (ok) ShowFriendsTab();
                }));
            }, DangerRed, 15, null, Color.white);
        }

        #endregion

        #region Requests Tab

        public void ShowRequestsTab()
        {
            currentTab = "requests";
            UpdateTabStyles();
            if (titleText != null) titleText.text = "친구 요청 관리";
            ClearContent();

            CreateLabel(contentContainer, "받은 친구 요청", 19, InkColor);

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
                    CreateLabel(contentContainer, "받은 친구 요청이 없습니다.", 15, SlateTextColor);
                }

                CreateLabel(contentContainer, "\n보낸 친구 요청", 19, InkColor);
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
                        CreateLabel(contentContainer, "보낸 친구 요청이 없습니다.", 15, SlateTextColor);
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

            CreateLabel(row.transform, $"{req.senderName} 님의 친구 요청", 17, InkColor);

            CreateStyledButton(row.transform, "수락", Vector2.zero, new Vector2(80f, 36f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.AcceptFriendRequest(req.id, (ok, _) =>
                {
                    if (ok) ShowRequestsTab();
                }));
            }, PrimaryOrange, 15, null, Color.white);

            CreateStyledButton(row.transform, "거절", Vector2.zero, new Vector2(80f, 36f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.RejectFriendRequest(req.id, (ok, _) =>
                {
                    if (ok) ShowRequestsTab();
                }));
            }, DangerRed, 15, null, Color.white);
        }

        private void BuildSentRequestRow(FriendRequestItem req)
        {
            GameObject row = CreateItemCard("SentReqRow", 52f);
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 6, 6);
            hlg.spacing = 15;
            hlg.childControlWidth = false;

            CreateLabel(row.transform, $"{req.receiverName} 님에게 보낸 요청 (대기중)", 17, SlateTextColor);

            CreateStyledButton(row.transform, "취소", Vector2.zero, new Vector2(80f, 36f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.CancelFriendRequest(req.id, ok =>
                {
                    if (ok) ShowRequestsTab();
                }));
            }, TabInactiveBg, 15, null, SlateTextColor);
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
            img.color = ChipBg;
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
            Sprite icon = null,
            Color? textColor = null)
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
            lbl.color = textColor ?? Color.white;

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
            lbl.fontStyle = FontStyles.Bold;
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
