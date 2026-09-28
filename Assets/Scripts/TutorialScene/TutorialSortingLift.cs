using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TutorialScene
{
    /// <summary>
    /// 안내가 짚는 UI를 마스크 위로 올린다. 예전에는 대상을 SetAsLastSibling으로 마스크 뒤로
    /// 보냈는데, 그 방식은 대상이 마스크의 형제일 때만 통한다. 새 UI는 짚을 대상을 카드나
    /// 줄 안에 넣어 두어서 형제 순서만 바뀌고 여전히 마스크에 덮였고, layout group 안에서는
    /// 순서가 바뀌며 배치까지 흐트러졌다. 대신 대상에 sorting을 덮어쓰는 Canvas를 붙여
    /// 계층 깊이와 상관없이 위로 그리고, 끝나면 건드린 것을 원래대로 되돌린다.
    /// </summary>
    internal sealed class TutorialSortingLift
    {
        private class Entry
        {
            public Canvas Canvas;
            public bool AddedCanvas;
            public bool OriginalOverrideSorting;
            public int OriginalSortingOrder;
            public int OriginalSortingLayerId;
            public GraphicRaycaster AddedRaycaster;
        }

        private readonly List<Entry> entries = new List<Entry>();

        public void Lift(Transform target, int sortingLayerId, int sortingOrder)
        {
            if (target == null)
            {
                return;
            }

            Canvas canvas = target.GetComponent<Canvas>();
            bool addedCanvas = canvas == null;
            if (addedCanvas)
            {
                canvas = target.gameObject.AddComponent<Canvas>();
            }

            Entry entry = new Entry
            {
                Canvas = canvas,
                AddedCanvas = addedCanvas,
                OriginalOverrideSorting = canvas.overrideSorting,
                OriginalSortingOrder = canvas.sortingOrder,
                OriginalSortingLayerId = canvas.sortingLayerID
            };

            // 자식 Canvas의 Graphic은 부모 Canvas의 raycaster에 잡히지 않는다. 버튼이 계속
            // 눌리도록 raycaster도 같이 붙인다.
            if (target.GetComponent<GraphicRaycaster>() == null)
            {
                entry.AddedRaycaster = target.gameObject.AddComponent<GraphicRaycaster>();
            }

            canvas.overrideSorting = true;
            canvas.sortingLayerID = sortingLayerId;
            canvas.sortingOrder = sortingOrder;

            entries.Add(entry);
        }

        public void RestoreAll()
        {
            // 같은 대상을 두 번 올렸을 수 있으므로 역순으로 되돌려야 처음 값이 남는다.
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                Entry entry = entries[i];

                // GraphicRaycaster는 Canvas를 RequireComponent로 잡고 있어 먼저 떼어야 한다.
                if (entry.AddedRaycaster != null)
                {
                    Object.DestroyImmediate(entry.AddedRaycaster);
                }

                if (entry.Canvas == null)
                {
                    continue;
                }

                if (entry.AddedCanvas)
                {
                    Object.DestroyImmediate(entry.Canvas);
                    continue;
                }

                entry.Canvas.overrideSorting = entry.OriginalOverrideSorting;
                entry.Canvas.sortingLayerID = entry.OriginalSortingLayerId;
                entry.Canvas.sortingOrder = entry.OriginalSortingOrder;
            }

            entries.Clear();
        }
    }
}
