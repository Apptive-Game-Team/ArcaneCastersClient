using System.Collections.Generic;
using Data.Quests;
using Global;
using UnityEngine;

namespace ProfileScene
{
    /// <summary>
    /// Instantiates <see cref="QuestItem"/> rows under the quest list's scroll content, the way
    /// <see cref="GameHistoryItemFactory"/> does for game history.
    /// <para>
    /// <c>GameHistoryItemFactory.ClearItems</c> loops <c>i &gt; 0</c> and so never removes the content's
    /// first child, which only works because that scene keeps an inactive placeholder there. This one
    /// clears every child except a template that sits in the content itself, so it does not depend on
    /// what happens to be first.
    /// </para>
    /// </summary>
    public class QuestItemFactory : MonoBehaviour
    {
        [SerializeField] private QuestItem itemPrefab;
        [SerializeField] private Transform itemRoot;

        private void Awake()
        {
            if (itemRoot == null)
            {
                itemRoot = transform;
            }

            // A template placed inside the content (instead of a prefab asset reference) must not show.
            if (itemPrefab != null && itemPrefab.gameObject.scene.IsValid())
            {
                itemPrefab.gameObject.SetActive(false);
            }
        }

        public void ClearItems()
        {
            if (itemRoot == null)
            {
                return;
            }

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

        public void CreateRange(IEnumerable<QuestDto> quests, QuestProgressPresenterRegistry registry, IQuestText text)
        {
            if (quests == null)
            {
                return;
            }

            foreach (QuestDto quest in quests)
            {
                Create(quest, registry, text);
            }
        }

        public QuestItem Create(QuestDto quest, QuestProgressPresenterRegistry registry, IQuestText text)
        {
            if (itemPrefab == null)
            {
                WDebug.LogError("QuestItemFactory has no item prefab.");
                return null;
            }

            Transform parent = itemRoot != null ? itemRoot : transform;
            QuestItem item = Instantiate(itemPrefab, parent);
            item.gameObject.SetActive(true);
            item.Render(quest, registry, text);
            return item;
        }
    }
}
