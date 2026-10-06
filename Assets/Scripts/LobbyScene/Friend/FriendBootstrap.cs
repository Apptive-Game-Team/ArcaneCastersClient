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
        private string currentTab = "search"; // "friends", "requests", "search"

        public static void Attach(Transform pill)
        {
            if (pill == null) return;

            // userNamePill 옆에 친구 버튼 생성
            Transform canvasTransform = pill.root;
            if (instance == null)
            {
                GameObject bootstrapGo = new GameObject("FriendBootstrap");
                bootstrapGo.transform.SetParent(canvasTransform, false);
                instance = bootstrapGo.AddComponent<FriendBootstrap>();
                instance.EnsureFriendManager();
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
            btnRect.sizeDelta = new Vector2(110f, 40f);
            btnRect.anchoredPosition = pillRect.anchoredPosition + new Vector2(pillRect.sizeDelta.x + 120f, 0f);

            Image img = friendBtnObj.GetComponent<Image>();
            img.color = new Color(0.15f, 0.2f, 0.35f, 0.95f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(friendBtnObj.transform, false);
            TextMeshProUGUI label = textObj.GetComponent<TextMeshProUGUI>();
            label.text = "친구 (F)";
            label.fontSize = 20;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            UnityEngine.UI.Button btn = friendBtnObj.GetComponent<UnityEngine.UI.Button>();
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
            Canvas canvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Modal Root Background (Dim)
            modalRoot = new GameObject("FriendModalOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            modalRoot.transform.SetParent(canvas.transform, false);
            RectTransform overlayRect = modalRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            modalRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            // Modal Window Dialog
            GameObject window = new GameObject("Window", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            window.transform.SetParent(modalRoot.transform, false);
            RectTransform winRect = window.GetComponent<RectTransform>();
            winRect.anchorMin = new Vector2(0.5f, 0.5f);
            winRect.anchorMax = new Vector2(0.5f, 0.5f);
            winRect.pivot = new Vector2(0.5f, 0.5f);
            winRect.sizeDelta = new Vector2(720f, 540f);
            window.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 0.98f);

            // Header Bar
            GameObject header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(window.transform, false);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 55f);
            headerRect.anchoredPosition = Vector2.zero;

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.text = "친구 관리";
            titleText.fontSize = 24;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(25f, 0f);

            // Close Button [X]
            CreateButton(header.transform, "X", new Vector2(280f, 0f), new Vector2(40f, 40f), () => modalRoot.SetActive(false), new Color(0.7f, 0.2f, 0.2f));

            // Tabs Row
            GameObject tabsRow = new GameObject("TabsRow", typeof(RectTransform));
            tabsRow.transform.SetParent(window.transform, false);
            RectTransform tabsRect = tabsRow.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = new Vector2(1f, 1f);
            tabsRect.pivot = new Vector2(0.5f, 1f);
            tabsRect.sizeDelta = new Vector2(0f, 45f);
            tabsRect.anchoredPosition = new Vector2(0f, -55f);

            CreateButton(tabsRow.transform, "친구 검색", new Vector2(-180f, 0f), new Vector2(160f, 38f), ShowSearchTab, new Color(0.2f, 0.4f, 0.6f));
            CreateButton(tabsRow.transform, "친구 목록", new Vector2(0f, 0f), new Vector2(160f, 38f), ShowFriendsTab, new Color(0.2f, 0.4f, 0.6f));
            CreateButton(tabsRow.transform, "친구 요청", new Vector2(180f, 0f), new Vector2(160f, 38f), ShowRequestsTab, new Color(0.2f, 0.4f, 0.6f));

            // Body Content Container
            GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(window.transform, false);
            RectTransform scrollRect = scrollObj.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.sizeDelta = new Vector2(-40f, -120f);
            scrollRect.anchoredPosition = new Vector2(0f, -60f);
            scrollObj.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.9f);

            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform cRect = contentObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(15, 15, 15, 15);
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

            // Search Input Row
            GameObject inputRow = new GameObject("SearchInputRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            inputRow.transform.SetParent(contentContainer, false);
            RectTransform rowRect = inputRow.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 50f);

            HorizontalLayoutGroup hlg = inputRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;

            // TMP Input Field
            GameObject inputGo = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(inputRow.transform, false);
            RectTransform inputRect = inputGo.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(480f, 44f);
            inputGo.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 1f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(inputGo.transform, false);
            TextMeshProUGUI inputText = textGo.GetComponent<TextMeshProUGUI>();
            inputText.fontSize = 20;
            inputText.color = Color.white;
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-20f, 0f);

            searchInput = inputGo.GetComponent<TMP_InputField>();
            searchInput.textComponent = inputText;

            // Placeholder
            GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGo.transform.SetParent(inputGo.transform, false);
            TextMeshProUGUI placeholderText = placeholderGo.GetComponent<TextMeshProUGUI>();
            placeholderText.text = "닉네임 또는 이메일 입력...";
            placeholderText.fontSize = 18;
            placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            RectTransform phRect = placeholderGo.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = new Vector2(-20f, 0f);
            searchInput.placeholder = placeholderText;

            // Search Button
            CreateButton(inputRow.transform, "검색", Vector2.zero, new Vector2(120f, 44f), OnClickSearch, new Color(0.25f, 0.55f, 0.35f));

            // Help info
            CreateLabel(contentContainer, "검색할 닉네임이나 이메일을 입력하고 [검색]을 누르세요.", 16, Color.gray);
        }

        private void OnClickSearch()
        {
            if (searchInput == null || FriendManager.Instance == null) return;
            string query = searchInput.text?.Trim();
            if (string.IsNullOrEmpty(query)) return;

            ClearSearchResultsOnly();
            CreateLabel(contentContainer, $"'{query}' 검색 중...", 18, Color.yellow, "SearchStatus");

            StartCoroutine(FriendManager.Instance.ApiClient.SearchMembers(query, (success, results) =>
            {
                DestroyExisting("SearchStatus");

                if (!success || results == null || results.Count == 0)
                {
                    CreateLabel(contentContainer, "검색 결과가 없습니다.", 18, Color.white, "SearchResultRow");
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
            GameObject row = new GameObject("SearchResultRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(contentContainer, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 50f);
            row.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.3f, 0.9f);

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(15, 15, 5, 5);
            hlg.spacing = 15;
            hlg.childControlWidth = false;

            CreateLabel(row.transform, $"{user.name} ({user.email})", 18, Color.white);

            if (user.isFriend)
            {
                CreateLabel(row.transform, "[친구]", 16, Color.green);
            }
            else if (user.hasPendingRequest)
            {
                CreateLabel(row.transform, "[요청 대기중]", 16, Color.yellow);
            }
            else
            {
                CreateButton(row.transform, "친구 요청", Vector2.zero, new Vector2(110f, 38f), () =>
                {
                    StartCoroutine(FriendManager.Instance.ApiClient.SendFriendRequest(user.name, (ok, _) =>
                    {
                        if (ok)
                        {
                            OnClickSearch(); // 새로고침
                        }
                    }));
                }, new Color(0.2f, 0.5f, 0.8f));
            }
        }

        #endregion

        #region Friends Tab

        public void ShowFriendsTab()
        {
            currentTab = "friends";
            if (titleText != null) titleText.text = "친구 목록";
            ClearContent();

            CreateLabel(contentContainer, "친구 목록 불러오는 중...", 18, Color.yellow, "LoadingFriends");

            if (FriendManager.Instance == null) return;
            StartCoroutine(FriendManager.Instance.ApiClient.GetFriends((success, friends) =>
            {
                DestroyExisting("LoadingFriends");

                if (!success || friends == null || friends.Count == 0)
                {
                    CreateLabel(contentContainer, "등록된 친구가 없습니다.", 18, Color.gray);
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
            GameObject row = new GameObject("FriendRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(contentContainer, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 50f);
            row.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.3f, 0.9f);

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(15, 15, 5, 5);
            hlg.spacing = 15;
            hlg.childControlWidth = false;

            Color statusColor = friend.status == "Online" ? Color.green : Color.gray;
            CreateLabel(row.transform, $"{friend.name}  [{friend.status}]  MMR: {friend.mmr}", 18, statusColor);

            // 친선전 초대 버튼 (Online일 때만)
            if (friend.status == "Online")
            {
                CreateButton(row.transform, "친선전 초대", Vector2.zero, new Vector2(110f, 38f), () =>
                {
                    StartCoroutine(FriendManager.Instance.ApiClient.InviteFriend(friend.userId, (ok, _) => { }));
                }, new Color(0.2f, 0.6f, 0.3f));
            }

            // 친구 삭제 버튼
            CreateButton(row.transform, "삭제", Vector2.zero, new Vector2(70f, 38f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.DeleteFriend(friend.userId, ok =>
                {
                    if (ok) ShowFriendsTab();
                }));
            }, new Color(0.7f, 0.25f, 0.25f));
        }

        #endregion

        #region Requests Tab

        public void ShowRequestsTab()
        {
            currentTab = "requests";
            if (titleText != null) titleText.text = "친구 요청 관리";
            ClearContent();

            CreateLabel(contentContainer, "받은 친구 요청 목록", 20, Color.cyan);

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
                    CreateLabel(contentContainer, "받은 친구 요청이 없습니다.", 16, Color.gray);
                }

                CreateLabel(contentContainer, "\n보낸 친구 요청 목록", 20, Color.cyan);
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
                        CreateLabel(contentContainer, "보낸 친구 요청이 없습니다.", 16, Color.gray);
                    }
                }));
            }));
        }

        private void BuildReceivedRequestRow(FriendRequestItem req)
        {
            GameObject row = new GameObject("ReceivedReqRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(contentContainer, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 45f);
            row.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.32f, 0.9f);

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(15, 15, 5, 5);
            hlg.spacing = 15;
            hlg.childControlWidth = false;

            CreateLabel(row.transform, $"{req.senderName} 님의 친구 요청", 18, Color.white);

            CreateButton(row.transform, "수락", Vector2.zero, new Vector2(80f, 36f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.AcceptFriendRequest(req.id, (ok, _) =>
                {
                    if (ok) ShowRequestsTab();
                }));
            }, new Color(0.2f, 0.6f, 0.3f));

            CreateButton(row.transform, "거절", Vector2.zero, new Vector2(80f, 36f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.RejectFriendRequest(req.id, (ok, _) =>
                {
                    if (ok) ShowRequestsTab();
                }));
            }, new Color(0.6f, 0.2f, 0.2f));
        }

        private void BuildSentRequestRow(FriendRequestItem req)
        {
            GameObject row = new GameObject("SentReqRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(contentContainer, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 45f);
            row.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.32f, 0.9f);

            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(15, 15, 5, 5);
            hlg.spacing = 15;
            hlg.childControlWidth = false;

            CreateLabel(row.transform, $"{req.receiverName} 님에게 보낸 요청 (대기중)", 18, Color.white);

            CreateButton(row.transform, "취소", Vector2.zero, new Vector2(80f, 36f), () =>
            {
                StartCoroutine(FriendManager.Instance.ApiClient.CancelFriendRequest(req.id, ok =>
                {
                    if (ok) ShowRequestsTab();
                }));
            }, new Color(0.5f, 0.5f, 0.5f));
        }

        #endregion

        #region Helpers

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

        private static GameObject CreateButton(Transform parent, string text, Vector2 pos, Vector2 size, Action onClick, Color bgColor)
        {
            GameObject btnObj = new GameObject(text + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UnityEngine.UI.Button));
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            Image img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI lbl = tObj.GetComponent<TextMeshProUGUI>();
            lbl.text = text;
            lbl.fontSize = 18;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = Color.white;

            RectTransform tRt = tObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            UnityEngine.UI.Button btn = btnObj.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btnObj;
        }

        private static GameObject CreateLabel(Transform parent, string text, int fontSize, Color color, string name = "Label")
        {
            GameObject lblObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            lblObj.transform.SetParent(parent, false);
            TextMeshProUGUI lbl = lblObj.GetComponent<TextMeshProUGUI>();
            lbl.text = text;
            lbl.fontSize = fontSize;
            lbl.color = color;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            return lblObj;
        }

        #endregion
    }
}
