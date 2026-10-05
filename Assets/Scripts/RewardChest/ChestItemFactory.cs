using System;
using UnityEngine;

namespace RewardChest
{
    /// <summary>Spawns one <see cref="ChestItem"/> per chest under the list content.</summary>
    public class ChestItemFactory : MonoBehaviour
    {
        /// <summary>The prefab asset. It is never a child of <see cref="itemRoot"/>, so clearing the list cannot hit it.</summary>
        [SerializeField] private ChestItem itemPrefab;
        [SerializeField] private Transform itemRoot;

        public void ClearItems()
        {
            if (itemRoot == null)
            {
                return;
            }

            // Unlike GameHistoryItemFactory there is no scene template at index 0, so every child goes.
            for (int index = itemRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = itemRoot.GetChild(index);
                if (itemPrefab != null && child == itemPrefab.transform)
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        public ChestItem Create(ChestDto chest, RewardTileRendererSelector selector, Action<ChestItem> onOpenRequested)
        {
            if (itemPrefab == null || itemRoot == null)
            {
                return null;
            }

            ChestItem item = Instantiate(itemPrefab, itemRoot);
            item.gameObject.SetActive(true);
            item.Bind(chest, selector, onOpenRequested);
            return item;
        }
    }
}
