using System;
using System.Collections.Generic;
using Admin.Dto;
using GameScene;
using Global;
using TMPro;
using UnityEngine;

namespace Admin
{
    public class DebugPanelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AdminViewModel adminViewModel;
        [SerializeField] private DebugItemUI itemPrefab;
        [SerializeField] private GameObject magicParent;
        [SerializeField] private GameObject prefabParent;
        [SerializeField] private TMPro.TMP_Dropdown masterDropdown; // Dropdown for LeftPlayer, RightPlayer, None

        private const int SelectionIndicatorSortingOrder = 16;
        private const float SelectionIndicatorRadius = 0.18f;

        private enum SelectionType { None, Magic, Prefab }
        private SelectionType currentSelectionType = SelectionType.None;
        private int selectedMagicId;
        private string selectedPrefabId;
        private string selectedMaster = "None";
        private SkillIndicatorShapeRenderer selectionIndicator;
        private FieldSelector fieldSelector;

        private readonly List<(string name, GameObject item)> magicItems = new List<(string, GameObject)>();
        private readonly List<(string name, GameObject item)> prefabItems = new List<(string, GameObject)>();
        private UnityEngine.UI.InputField magicSearch;
        private UnityEngine.UI.InputField prefabSearch;

        private void Start()
        {
            if (adminViewModel == null) adminViewModel = FindObjectOfType<AdminViewModel>();
            selectionIndicator = CreateSelectionIndicator();
            selectionIndicator.gameObject.SetActive(false);

            InitializeMasterSelection();
            RefreshLists();
        }

        private void InitializeMasterSelection()
        {
            if (masterDropdown != null)
            {
                masterDropdown.onValueChanged.RemoveAllListeners();
                masterDropdown.onValueChanged.AddListener(index =>
                {
                    selectedMaster = masterDropdown.options[index].text;
                    WDebug.Log($"Master changed to: {selectedMaster}");
                });

                // Try to set initial value from SceneContext.Me
                string currentMe = SceneContext.Me;
                int defaultIndex = masterDropdown.options.FindIndex(option => option.text.Equals(currentMe));
                if (defaultIndex >= 0)
                {
                    masterDropdown.value = defaultIndex;
                    selectedMaster = currentMe;
                }
                else
                {
                    selectedMaster = masterDropdown.options[masterDropdown.value].text;
                }
            }
            else
            {
                selectedMaster = SceneContext.Me ?? "None";
            }
        }

        public void SetMaster(string master)
        {
            selectedMaster = master;
            WDebug.Log($"Master manually set to: {selectedMaster}");
        }

        public void RefreshLists()
        {
            // Clear existing items
            foreach (Transform child in magicParent.transform) Destroy(child.gameObject);
            foreach (Transform child in prefabParent.transform) Destroy(child.gameObject);
            magicItems.Clear();
            prefabItems.Clear();

            magicSearch = CreateSearchField(magicParent.transform, "Search magic...", magicItems);
            prefabSearch = CreateSearchField(prefabParent.transform, "Search prefab...", prefabItems);

            // Fetch Magics
            adminViewModel.FetchMagics(magics =>
            {
                if (magics == null) return;
                foreach (var magic in magics)
                {
                    var item = Instantiate(itemPrefab, magicParent.transform);
                    item.SetItem(magic.name, () => SelectMagic(magic.id));
                    magicItems.Add((magic.name, item.gameObject));
                }
                ApplyFilter(magicItems, magicSearch.text);
            });

            // Fetch Prefabs
            adminViewModel.FetchPrefabs(prefabs =>
            {
                if (prefabs == null) return;
                foreach (var prefab in prefabs)
                {
                    var item = Instantiate(itemPrefab, prefabParent.transform);
                    item.SetItem(prefab.name, () => SelectPrefab(prefab.id));
                    prefabItems.Add((prefab.name, item.gameObject));
                }
                ApplyFilter(prefabItems, prefabSearch.text);
            });
        }

        private static UnityEngine.UI.InputField CreateSearchField(
            Transform parent,
            string placeholder,
            List<(string name, GameObject item)> items)
        {
            GameObject root = UnityEngine.UI.DefaultControls.CreateInputField(new UnityEngine.UI.DefaultControls.Resources());
            root.name = "SearchField";
            root.transform.SetParent(parent, false);
            root.transform.SetAsFirstSibling();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = parent.gameObject.layer;
            }

            UnityEngine.UI.InputField input = root.GetComponent<UnityEngine.UI.InputField>();
            if (input.placeholder is UnityEngine.UI.Text placeholderText) placeholderText.text = placeholder;
            input.onValueChanged.AddListener(query => ApplyFilter(items, query));
            return input;
        }

        private static void ApplyFilter(List<(string name, GameObject item)> items, string query)
        {
            foreach ((string name, GameObject item) in items)
            {
                if (item == null) continue;
                item.SetActive(string.IsNullOrEmpty(query) ||
                               name.Contains(query, StringComparison.OrdinalIgnoreCase));
            }
        }

        private void SelectMagic(int id)
        {
            selectedMagicId = id;
            currentSelectionType = SelectionType.Magic;
            if (selectionIndicator != null) selectionIndicator.gameObject.SetActive(true);
            WDebug.Log($"Magic {id} selected. Master: {selectedMaster}. Click on field to summon.");
        }

        private void SelectPrefab(string id)
        {
            selectedPrefabId = id;
            currentSelectionType = SelectionType.Prefab;
            if (selectionIndicator != null) selectionIndicator.gameObject.SetActive(true);
            WDebug.Log($"Prefab {id} selected. Master: {selectedMaster}. Click on field to spawn.");
        }

        private void Update()
        {
            if (currentSelectionType == SelectionType.None) return;

            // Cancel selection on Right Click
            if (Input.GetMouseButtonDown(1))
            {
                CancelSelection();
                return;
            }

            // Same ground raycast the real cast path uses, so the sent position matches where you clicked.
            if (!TryGetGroundPosition(out Vector3 worldPos)) return;

            if (selectionIndicator != null)
            {
                selectionIndicator.SetCircle(worldPos, SelectionIndicatorRadius, true, SelectionIndicatorSortingOrder, 0f);
            }

            // Confirm selection on Left Click
            if (Input.GetMouseButtonDown(0))
            {
                // Check if clicking on UI
                if (PointerInputUtility.IsPointerOverUi()) return;

                ExecuteSelection(worldPos);
            }
        }

        private bool TryGetGroundPosition(out Vector3 groundPosition)
        {
            if (fieldSelector == null) fieldSelector = FindObjectOfType<FieldSelector>();
            if (fieldSelector != null) return fieldSelector.TryGetGroundPosition(Input.mousePosition, out groundPosition);

            groundPosition = Vector3.zero;
            WDebug.LogWarning("[DebugPanel] No FieldSelector in scene, cannot resolve ground position.");
            return false;
        }

        private void ExecuteSelection(Vector3 position)
        {
            string sessionId = SceneContext.MatchInfo?.sessionId ?? "debug-1";
            string master = selectedMaster;

            if (currentSelectionType == SelectionType.Magic)
            {
                var dto = new DebugSummonMagicRequestDto
                {
                    sessionId = sessionId,
                    master = master,
                    magicId = selectedMagicId,
                    position = position
                };
                adminViewModel.SummonMagic(dto, response =>
                {
                    if (!response.success) WDebug.LogError("Summon Magic Failed: " + response.message);
                });
            }
            else if (currentSelectionType == SelectionType.Prefab)
            {
                var dto = new DebugSpawnPrefabRequestDto
                {
                    sessionId = sessionId,
                    master = master,
                    prefabId = selectedPrefabId,
                    position = position
                };
                adminViewModel.SpawnPrefab(dto, response =>
                {
                    if (!response.success) WDebug.LogError("Spawn Prefab Failed: " + response.message);
                });
            }

            // Keep selection active for multiple spawns?
            // The user didn't specify, but usually for debug it's nice.
            // Let's reset it for now to avoid accidental spawns.
            CancelSelection();
        }

        private void CancelSelection()
        {
            currentSelectionType = SelectionType.None;
            if (selectionIndicator != null) selectionIndicator.gameObject.SetActive(false);
        }

        private static SkillIndicatorShapeRenderer CreateSelectionIndicator()
        {
            GameObject indicator = new GameObject("DebugSelectionIndicator");
            SkillIndicatorShapeRenderer shapeRenderer = indicator.AddComponent<SkillIndicatorShapeRenderer>();
            shapeRenderer.SetLocalCircle(SelectionIndicatorRadius, SelectionIndicatorSortingOrder);
            return shapeRenderer;
        }
    }
}
