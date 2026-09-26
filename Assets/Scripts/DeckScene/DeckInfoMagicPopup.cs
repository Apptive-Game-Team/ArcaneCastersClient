using System.Collections;
using System.Collections.Generic;
using Data.Localization;
using Data.Magic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckScene
{
    public class DeckInfoMagicPopup : MonoBehaviour
    {
        private const float DetailAnchorOffset = 12f;
        private const float DetailExitGraceSeconds = 0.05f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private RectTransform detailRoot;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailTextText;

        private int detailRequestId;
        private Coroutine pendingClearDetailCoroutine;
        private bool keepDetailWhilePointerInside;
        private HoverPopupTransition hoverTransition;

        private void Awake()
        {
            panelRoot ??= gameObject;
            hoverTransition = new HoverPopupTransition(this);
            Hide();
        }

        private void Update()
        {
            if (keepDetailWhilePointerInside && !IsPointerInsideDetail())
            {
                ClearDetail();
            }
        }

        public void Show(IReadOnlyList<CombinedMagicData> magics)
        {
            panelRoot ??= gameObject;
            ClearItems();
            ClearDetail();

            if (magics == null || magics.Count == 0)
            {
                AddEmptyItem();
            }
            else
            {
                foreach (CombinedMagicData magic in magics)
                {
                    AddMagicItem(magic);
                }
            }

            panelRoot.SetActive(true);
        }

        public void Hide()
        {
            ClearDetail();
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            root.SetActive(false);
        }

        public bool IsVisible()
        {
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            return root.activeSelf;
        }

        private void AddMagicItem(CombinedMagicData magic)
        {
            if (itemRoot == null)
            {
                Debug.LogWarning("[DeckInfoMagicPopup] itemRoot is not assigned.");
                return;
            }

            DeckMagicPopupItem item = DeckMagicPopupItem.Create(itemRoot);
            item.Bind(magic, RequestShowDetail, RequestClearDetail);
        }

        private void AddEmptyItem()
        {
            if (itemRoot == null)
            {
                return;
            }

            GameObject textObject = new GameObject("EmptyMagicText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(itemRoot, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "None";
            text.fontSize = 18f;
            text.alignment = TextAlignmentOptions.Center;
        }

        private void RequestShowDetail(CombinedMagicData magic, RectTransform anchor)
        {
            if (magic == null)
            {
                return;
            }

            ClearDetail();
            CancelPendingClearDetail();
            keepDetailWhilePointerInside = false;
            int requestId = ++detailRequestId;
            hoverTransition ??= new HoverPopupTransition(this);
            hoverTransition.RunAfterDelay(() => ShowDetail(magic, anchor, requestId));
        }

        private async void ShowDetail(CombinedMagicData magic, RectTransform anchor, int requestId)
        {
            string localizedName = await GetLocalizedTextAsync("Magic", magic.localizationKey, magic.localizationKey);
            string localizedText = await MagicBookDetailText.BuildAsync(magic);
            if (requestId != detailRequestId)
            {
                return;
            }

            RectTransform root = GetDetailRoot();
            hoverTransition ??= new HoverPopupTransition(this);
            hoverTransition.ShowNow(root != null ? root.gameObject : null, () =>
            {
                if (detailNameText != null)
                {
                    detailNameText.text = localizedName;
                }

                if (detailTextText != null)
                {
                    detailTextText.text = localizedText;
                    detailTextText.gameObject.SetActive(!string.IsNullOrWhiteSpace(localizedText));
                }

                if (root != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(root);
                }

                PlaceDetailNextTo(anchor);
            });
        }

        private void ClearDetail()
        {
            CancelPendingClearDetail();
            keepDetailWhilePointerInside = false;
            detailRequestId++;

            if (detailNameText != null)
            {
                detailNameText.text = string.Empty;
            }

            if (detailTextText != null)
            {
                detailTextText.text = string.Empty;
                detailTextText.gameObject.SetActive(false);
            }

            RectTransform root = GetDetailRoot();
            hoverTransition ??= new HoverPopupTransition(this);
            hoverTransition.HideImmediate(root != null ? root.gameObject : null);
        }

        private void RequestClearDetail()
        {
            if (!isActiveAndEnabled)
            {
                ClearDetail();
                return;
            }

            CancelPendingClearDetail();
            pendingClearDetailCoroutine = StartCoroutine(ClearDetailAfterPointerCheck());
        }

        private IEnumerator ClearDetailAfterPointerCheck()
        {
            yield return new WaitForSecondsRealtime(DetailExitGraceSeconds);
            pendingClearDetailCoroutine = null;

            keepDetailWhilePointerInside = IsPointerInsideDetail();
            if (!keepDetailWhilePointerInside)
            {
                ClearDetail();
            }
        }

        private void CancelPendingClearDetail()
        {
            if (pendingClearDetailCoroutine == null)
            {
                return;
            }

            StopCoroutine(pendingClearDetailCoroutine);
            pendingClearDetailCoroutine = null;
        }

        private bool IsPointerInsideDetail()
        {
            RectTransform root = GetDetailRoot();
            return root != null
                && root.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(root, Input.mousePosition, GetCanvasCamera(root));
        }

        private void PlaceDetailNextTo(RectTransform anchor)
        {
            RectTransform detailRect = GetDetailRoot();
            if (anchor == null || detailRect == null)
            {
                return;
            }

            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector3 rightCenter = (corners[2] + corners[3]) * 0.5f;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(detailRect), rightCenter);
            screenPoint.x += DetailAnchorOffset;

            detailRect.pivot = new Vector2(0f, 0.5f);
            RectTransform parentRect = detailRect.parent as RectTransform;
            if (parentRect == null)
            {
                detailRect.position = rightCenter + (anchor.right * DetailAnchorOffset);
                return;
            }

            Camera camera = GetCanvasCamera(parentRect);
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPoint, camera, out Vector3 worldPoint))
            {
                detailRect.position = worldPoint;
                detailRect.localPosition = ClampToParent(detailRect, parentRect);
            }
        }

        private RectTransform GetDetailRoot()
        {
            if (detailRoot != null)
            {
                return detailRoot;
            }

            if (detailNameText != null && detailNameText.transform.parent is RectTransform nameParent)
            {
                detailRoot = nameParent;
                return detailRoot;
            }

            if (detailTextText != null && detailTextText.transform.parent is RectTransform textParent)
            {
                detailRoot = textParent;
                return detailRoot;
            }

            return null;
        }

        private void ClearItems()
        {
            if (itemRoot == null)
            {
                return;
            }

            foreach (Transform child in itemRoot)
            {
                Destroy(child.gameObject);
            }
        }

        private static Camera GetCanvasCamera(Component component)
        {
            Canvas canvas = component.GetComponentInParent<Canvas>();
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }

        private static async System.Threading.Tasks.Task<string> GetLocalizedTextAsync(string tableName, string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            string localized = await LocaleUtils.GetStringAsync(tableName, key);
            return string.IsNullOrWhiteSpace(localized) || localized == key
                ? fallback
                : localized;
        }

        private static Vector3 ClampToParent(RectTransform rect, RectTransform parent)
        {
            Rect parentRect = parent.rect;
            Rect rectRect = rect.rect;
            Vector3 localPosition = rect.localPosition;

            float minX = parentRect.xMin;
            float maxX = parentRect.xMax - rectRect.width;
            float minY = parentRect.yMin + (rectRect.height * 0.5f);
            float maxY = parentRect.yMax - (rectRect.height * 0.5f);

            localPosition.x = Mathf.Clamp(localPosition.x, minX, Mathf.Max(minX, maxX));
            localPosition.y = Mathf.Clamp(localPosition.y, minY, Mathf.Max(minY, maxY));
            return localPosition;
        }
    }
}
