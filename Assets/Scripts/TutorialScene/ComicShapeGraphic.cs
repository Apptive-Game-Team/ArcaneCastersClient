using UnityEngine;
using UnityEngine.UI;

namespace TutorialScene
{
    // 중심에서 모든 경계가 보이는 윤곽만 사용한다. 도형 생성은 Editor에서 처리한다.
    [RequireComponent(typeof(CanvasRenderer))]
    public class ComicShapeGraphic : MaskableGraphic
    {
        [SerializeField] private Vector2[] vertices = {
            Vector2.zero, Vector2.right, Vector2.one, Vector2.up
        };
        [SerializeField, Min(0)] private float inset;
        [SerializeField, Min(0)] private float strokeWidth = 2f;
        [SerializeField] private Color strokeColor = Color.black;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (vertices == null || vertices.Length < 3)
            {
                return;
            }
            if (strokeWidth > 0)
            {
                AddFill(mesh, inset, strokeColor);
            }
            AddFill(mesh, inset + strokeWidth, color);
        }

        private void AddFill(VertexHelper mesh, float padding, Color tint)
        {
            Rect rect = rectTransform.rect;
            if (rect.width <= padding * 2f || rect.height <= padding * 2f)
            {
                return;
            }
            int start = mesh.currentVertCount;
            Vector2 size = rect.size - Vector2.one * padding * 2f;
            Vector2 origin = rect.min + Vector2.one * padding;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = tint;
            vertex.position = origin + size * 0.5f;
            mesh.AddVert(vertex);
            foreach (Vector2 point in vertices)
            {
                vertex.position = origin + Vector2.Scale(point, size);
                mesh.AddVert(vertex);
            }
            for (int i = 0; i < vertices.Length; i++)
            {
                mesh.AddTriangle(start, start + i + 1, start + (i + 1) % vertices.Length + 1);
            }
        }
    }
}
