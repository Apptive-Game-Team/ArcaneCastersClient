using System.Collections.Generic;
using UnityEngine;

namespace GameScene
{
    /// <summary>
    /// 스킬 인디케이터의 원/직선 도형을 메시로 그린다.
    /// <para>
    /// FieldSelector가 매 프레임 세 개(사거리 원, 조준 원, 스킬 도형)를 갱신하므로 두 가지를 지킨다.
    /// (1) 마지막으로 그린 파라미터와 같으면 아무것도 다시 만들지 않는다.
    /// (2) 다시 만들 때도 폴리곤/정점 버퍼를 재사용해 프레임마다 GC를 유발하지 않는다.
    /// </para>
    /// </summary>
    public class SkillIndicatorShapeRenderer : MonoBehaviour
    {
        private const int CircleSegments = 64;
        private const float DefaultSurfaceOffset = 0.02f;

        // 반원 캡 세그먼트 수 (클수록 부드럽고, 삼각형 수 증가)
        private const int CapSegments = 16;

        // 바닥 패널 월드 범위: X ∈ [0, 18], Z ∈ [0, 10]
        private static readonly Vector2 FieldMin = new Vector2(0f, 0f);
        private static readonly Vector2 FieldMax = new Vector2(18f, 10f);

        private static readonly Color DefaultFillColor = new Color(0.95f, 0.62f, 0.18f, 0.24f);
        private static readonly Color DefaultEdgeColor = new Color(0.18f, 0.88f, 0.82f, 0.62f);
        private static Material sharedMaterial;

        public static Color AccentLineColor => new Color(
            DefaultFillColor.r,
            DefaultFillColor.g,
            DefaultFillColor.b,
            DefaultEdgeColor.a);

        /// <summary>
        /// indicator document layer 채움에 쓰는 색. <see cref="DefaultEdgeColor"/>의 색상에
        /// <see cref="DefaultFillColor"/>의 alpha 를 섞어, 사거리 원/조준 원과 같은
        /// <see cref="DefaultFillColor"/>(주황)를 쓰지 않고도 테두리와 같은 계열(청록)로 보이게 한다.
        /// </summary>
        public static Color LayerFillColor => new Color(
            DefaultEdgeColor.r,
            DefaultEdgeColor.g,
            DefaultEdgeColor.b,
            DefaultFillColor.a);

        public static Material SharedMaterial => GetSharedMaterial();

        private enum ShapeMode
        {
            None,
            Circle,
            LocalCircle,
            Line
        }

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private LineRenderer edgeRenderer;
        private SpriteRenderer spriteRenderer;
        private Transform fillTransform;
        private Transform edgeTransform;
        private Mesh shapeMesh;

        // 도형 생성용 재사용 버퍼. 매 프레임 새로 할당하면 WebGL에서 GC 히칭이 눈에 띈다.
        private readonly List<Vector2> polygonBuffer = new List<Vector2>(CircleSegments);
        private readonly List<Vector2> clipBufferA = new List<Vector2>(CircleSegments + 8);
        private readonly List<Vector2> clipBufferB = new List<Vector2>(CircleSegments + 8);
        private readonly List<Vector3> vertexBuffer = new List<Vector3>(CircleSegments + 8);
        private readonly List<Color> colorBuffer = new List<Color>(CircleSegments + 8);
        private readonly List<int> triangleBuffer = new List<int>((CircleSegments + 8) * 3);

        // 마지막으로 그린 도형의 입력값. 동일하면 재생성을 건너뛴다.
        private ShapeMode lastMode = ShapeMode.None;
        private Vector3 lastPosition;
        private Vector3 lastTargetPosition;
        private float lastSize;
        private float lastWidth;
        private int lastSortingOrder;
        private bool lastIncludeFill;
        private Color lastFillColor;
        private bool hasBuiltShape;

        private void Awake()
        {
            EnsureRenderers();
            DisableSpriteRenderer();
        }

        private void OnDestroy()
        {
            if (shapeMesh != null)
            {
                Destroy(shapeMesh);
                shapeMesh = null;
            }
        }

        /// <param name="fillColor">
        /// 채움 색. 생략하면(<c>null</c>) 사거리 원/조준 원이 쓰던 <see cref="DefaultFillColor"/> 그대로다.
        /// indicator document layer 는 <see cref="LayerFillColor"/> 를 넘겨 두 원과 색으로 구분된다.
        /// </param>
        public void SetCircle(
            Vector3 position, float radius, bool includeFill, int sortingOrder, float edgeWidth,
            Color? fillColor = null)
        {
            Color resolvedFillColor = fillColor ?? DefaultFillColor;
            if (IsUnchanged(ShapeMode.Circle, position, Vector3.zero, radius, edgeWidth, sortingOrder, includeFill,
                    resolvedFillColor))
            {
                return;
            }

            RecordShapeState(ShapeMode.Circle, position, Vector3.zero, radius, edgeWidth, sortingOrder, includeFill,
                resolvedFillColor);

            EnsureRenderers();
            DisableSpriteRenderer();

            transform.position = GetSurfacePosition(position);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            float safeRadius = Mathf.Max(radius, 0f);
            if (safeRadius <= Mathf.Epsilon)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            SetSorting(sortingOrder);
            BuildCircleMesh(safeRadius, includeFill, resolvedFillColor);
            if (edgeWidth > 0f)
            {
                BuildCircleEdge(safeRadius, edgeWidth);
            }
            else
            {
                edgeRenderer.enabled = false;
            }
        }

        public void SetLocalCircle(float radius, int sortingOrder)
        {
            if (IsUnchanged(ShapeMode.LocalCircle, Vector3.zero, Vector3.zero, radius, 0f, sortingOrder, true,
                    DefaultFillColor))
            {
                return;
            }

            RecordShapeState(ShapeMode.LocalCircle, Vector3.zero, Vector3.zero, radius, 0f, sortingOrder, true,
                DefaultFillColor);

            EnsureRenderers();
            DisableSpriteRenderer();

            transform.localScale = Vector3.one;

            float safeRadius = Mathf.Max(radius, 0f);
            if (safeRadius <= Mathf.Epsilon)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            SetSorting(sortingOrder);
            BuildCircleMesh(safeRadius, true, DefaultFillColor);
            edgeRenderer.enabled = false;
        }

        /// <param name="fillColor">
        /// 채움 색. 생략하면(<c>null</c>) 기존 line indicator(<see cref="LineSkillIndicator"/>)가 쓰던
        /// <see cref="DefaultFillColor"/> 그대로다. indicator document 의 lane layer 는
        /// <see cref="LayerFillColor"/> 를 넘겨 사거리 원/조준 원과 색으로 구분된다.
        /// </param>
        public void SetLine(
            Vector3 startPosition, Vector3 targetPosition, float length, int sortingOrder, float width,
            Color? fillColor = null)
        {
            Color resolvedFillColor = fillColor ?? DefaultFillColor;
            if (IsUnchanged(ShapeMode.Line, startPosition, targetPosition, length, width, sortingOrder, true,
                    resolvedFillColor))
            {
                return;
            }

            RecordShapeState(ShapeMode.Line, startPosition, targetPosition, length, width, sortingOrder, true,
                resolvedFillColor);

            EnsureRenderers();
            DisableSpriteRenderer();

            float safeLength = Mathf.Max(length, 0f);
            if (safeLength <= Mathf.Epsilon)
            {
                SetVisible(false);
                return;
            }

            Vector3 direction = targetPosition - startPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            SetSorting(sortingOrder);

            transform.position = GetSurfacePosition(startPosition);
            transform.rotation = Quaternion.Euler(0f, -Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg, 0f);
            transform.localScale = Vector3.one;

            float halfWidth = Mathf.Max(width, 0.01f) * 0.5f;
            BuildLineMesh(safeLength, halfWidth, resolvedFillColor);
            edgeRenderer.enabled = false;
        }

        // ─── Dirty check ─────────────────────────────────────────────────────────

        /// <summary>
        /// 메시는 여기 담긴 입력값만으로 완전히 결정되므로, 값이 같으면 재생성이 필요 없다.
        /// </summary>
        private bool IsUnchanged(
            ShapeMode mode,
            Vector3 position,
            Vector3 targetPosition,
            float size,
            float width,
            int sortingOrder,
            bool includeFill,
            Color fillColor)
        {
            return hasBuiltShape &&
                   lastMode == mode &&
                   lastPosition == position &&
                   lastTargetPosition == targetPosition &&
                   lastSize == size &&
                   lastWidth == width &&
                   lastSortingOrder == sortingOrder &&
                   lastIncludeFill == includeFill &&
                   lastFillColor == fillColor;
        }

        private void RecordShapeState(
            ShapeMode mode,
            Vector3 position,
            Vector3 targetPosition,
            float size,
            float width,
            int sortingOrder,
            bool includeFill,
            Color fillColor)
        {
            lastMode = mode;
            lastPosition = position;
            lastTargetPosition = targetPosition;
            lastSize = size;
            lastWidth = width;
            lastSortingOrder = sortingOrder;
            lastIncludeFill = includeFill;
            lastFillColor = fillColor;
            hasBuiltShape = true;
        }

        private void EnsureRenderers()
        {
            if (meshFilter == null)
            {
                fillTransform = GetOrCreateChild("ShapeFill");
                meshFilter = fillTransform.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = fillTransform.gameObject.AddComponent<MeshFilter>();
                }
            }

            if (meshRenderer == null)
            {
                if (fillTransform == null)
                {
                    fillTransform = GetOrCreateChild("ShapeFill");
                }

                meshRenderer = fillTransform.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = fillTransform.gameObject.AddComponent<MeshRenderer>();
                }
                meshRenderer.sharedMaterial = GetSharedMaterial();
            }

            if (edgeRenderer == null)
            {
                edgeTransform = GetOrCreateChild("ShapeEdge");
                edgeRenderer = edgeTransform.GetComponent<LineRenderer>();
                if (edgeRenderer == null)
                {
                    edgeRenderer = edgeTransform.gameObject.AddComponent<LineRenderer>();
                }

                edgeRenderer.sharedMaterial = GetSharedMaterial();
                edgeRenderer.useWorldSpace = false;
                edgeRenderer.loop = true;
                edgeRenderer.numCornerVertices = 4;
                edgeRenderer.numCapVertices = 4;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private Transform GetOrCreateChild(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName).transform;
                child.SetParent(transform, false);
            }

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        private void DisableSpriteRenderer()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        private void SetVisible(bool visible)
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = visible;
            }

            if (edgeRenderer != null)
            {
                edgeRenderer.enabled = visible;
            }
        }

        private void SetSorting(int sortingOrder)
        {
            meshRenderer.sortingOrder = sortingOrder;
            edgeRenderer.sortingOrder = sortingOrder + 1;
        }

        // ─── Circle ──────────────────────────────────────────────────────────────

        private void BuildCircleMesh(float radius, bool includeFill, Color fillColor)
        {
            if (!includeFill)
            {
                meshFilter.sharedMesh = null;
                return;
            }

            // 원의 외곽 polygon을 로컬 XZ 좌표로 생성
            polygonBuffer.Clear();
            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = Mathf.PI * 2f * i / CircleSegments;
                polygonBuffer.Add(new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius));
            }

            // 월드 XZ 공간에서 필드 경계로 클리핑 후 로컬 XZ로 역변환
            ApplyPolygonToMesh(ClipPolygonToFieldInWorldSpace(polygonBuffer), fillColor);
        }

        private void BuildCircleEdge(float radius, float edgeWidth)
        {
            edgeRenderer.loop = true;
            edgeRenderer.positionCount = CircleSegments;
            edgeRenderer.startWidth = edgeWidth;
            edgeRenderer.endWidth = edgeWidth;
            edgeRenderer.startColor = DefaultEdgeColor;
            edgeRenderer.endColor = DefaultEdgeColor;

            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = Mathf.PI * 2f * i / CircleSegments;
                edgeRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        // ─── Line ────────────────────────────────────────────────────────────────

        private void BuildLineMesh(float length, float halfWidth, Color fillColor)
        {
            // Stadium(rounded rectangle) polygon:
            //  - 우측 반원 캡 (center: length, 0),  각도 -90° → +90° (CCW)
            //  - 좌측 반원 캡 (center: 0,      0),  각도 +90° → +270° (CCW)
            polygonBuffer.Clear();

            // 우측 캡: X = length, 반시계 방향 (아래 → 위)
            for (int i = 0; i <= CapSegments; i++)
            {
                float angle = Mathf.PI * (-0.5f + (float)i / CapSegments); // -90° → +90°
                polygonBuffer.Add(new Vector2(length + Mathf.Cos(angle) * halfWidth,
                                                       Mathf.Sin(angle) * halfWidth));
            }

            // 좌측 캡: X = 0, 반시계 방향 (위 → 아래)
            for (int i = 0; i <= CapSegments; i++)
            {
                float angle = Mathf.PI * (0.5f + (float)i / CapSegments); // 90° → 270°
                polygonBuffer.Add(new Vector2(0f + Mathf.Cos(angle) * halfWidth,
                                                    Mathf.Sin(angle) * halfWidth));
            }

            // 월드 XZ 공간에서 필드 경계로 클리핑 후 로컬 XZ로 역변환
            ApplyPolygonToMesh(ClipPolygonToFieldInWorldSpace(polygonBuffer), fillColor);
        }

        // ─── Mesh upload ─────────────────────────────────────────────────────────

        /// <summary>XZ polygon을 fan triangulation으로 메시에 올린다.</summary>
        private void ApplyPolygonToMesh(List<Vector2> polygon, Color fillColor)
        {
            if (polygon == null || polygon.Count < 3)
            {
                meshFilter.sharedMesh = null;
                return;
            }

            int vertCount = polygon.Count;
            vertexBuffer.Clear();
            colorBuffer.Clear();
            triangleBuffer.Clear();

            for (int i = 0; i < vertCount; i++)
            {
                Vector2 point = polygon[i];
                vertexBuffer.Add(new Vector3(point.x, 0f, point.y));
                colorBuffer.Add(fillColor);
            }

            for (int i = 0; i < vertCount - 2; i++)
            {
                triangleBuffer.Add(0);
                triangleBuffer.Add(i + 2);
                triangleBuffer.Add(i + 1);
            }

            Mesh mesh = GetShapeMesh();
            mesh.Clear();
            mesh.SetVertices(vertexBuffer);
            mesh.SetTriangles(triangleBuffer, 0);
            mesh.SetColors(colorBuffer);
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
        }

        // ─── Clipping helpers ────────────────────────────────────────────────────

        /// <summary>
        /// 로컬 XZ polygon을 월드 XZ로 변환 → 필드 AABB로 클리핑 → 다시 로컬 XZ로 역변환합니다.
        /// rotation이 있는 transform(예: LineSkillIndicator)에서도 정확히 동작합니다.
        /// 도형이 필드 안에 완전히 들어오는 흔한 경우에는 클리핑과 왕복 변환을 모두 건너뜁니다.
        /// </summary>
        private List<Vector2> ClipPolygonToFieldInWorldSpace(List<Vector2> localPolygon)
        {
            int count = localPolygon.Count;
            if (count < 3)
            {
                return null;
            }

            // 1) 로컬 XZ → 월드 XZ (동시에 월드 AABB 계산)
            clipBufferA.Clear();
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                Vector2 local = localPolygon[i];
                Vector3 worldPos = transform.TransformPoint(new Vector3(local.x, 0f, local.y));
                clipBufferA.Add(new Vector2(worldPos.x, worldPos.z));

                if (worldPos.x < minX) minX = worldPos.x;
                if (worldPos.x > maxX) maxX = worldPos.x;
                if (worldPos.z < minZ) minZ = worldPos.z;
                if (worldPos.z > maxZ) maxZ = worldPos.z;
            }

            // 필드 안에 완전히 들어오면 클리핑 결과가 원본과 같다 → 왕복 변환 생략
            if (minX >= FieldMin.x && maxX <= FieldMax.x && minZ >= FieldMin.y && maxZ <= FieldMax.y)
            {
                return localPolygon;
            }

            // 2) 월드 XZ AABB 클리핑
            List<Vector2> clippedWorld = ClipPolygonToAABB(clipBufferA, clipBufferB);
            if (clippedWorld == null)
            {
                return null;
            }

            // 3) 월드 XZ → 로컬 XZ (같은 버퍼에 제자리 변환)
            float worldY = transform.position.y;
            for (int i = 0; i < clippedWorld.Count; i++)
            {
                Vector2 world = clippedWorld[i];
                Vector3 localPos = transform.InverseTransformPoint(new Vector3(world.x, worldY, world.y));
                clippedWorld[i] = new Vector2(localPos.x, localPos.z);
            }

            return clippedWorld;
        }

        /// <summary>
        /// Sutherland-Hodgman algorithm: 필드 AABB로 XZ polygon을 클리핑합니다.
        /// 두 버퍼를 번갈아 쓰며(ping-pong) 중간 리스트를 할당하지 않습니다.
        /// </summary>
        private static List<Vector2> ClipPolygonToAABB(List<Vector2> input, List<Vector2> scratch)
        {
            if (input == null || input.Count < 3)
            {
                return null;
            }

            List<Vector2> source = input;
            List<Vector2> target = scratch;

            // 4개의 clip edge: left, right, bottom, top (XZ 평면)
            for (int edge = 0; edge < 4; edge++)
            {
                target.Clear();
                ClipByEdge(source, target, edge);
                if (target.Count < 3)
                {
                    return null;
                }

                List<Vector2> swap = source;
                source = target;
                target = swap;
            }

            return source;
        }

        private static void ClipByEdge(List<Vector2> input, List<Vector2> output, int edge)
        {
            int count = input.Count;
            if (count == 0)
            {
                return;
            }

            Vector2 previous = input[count - 1];
            bool prevInside = IsInsideEdge(previous, edge);

            for (int i = 0; i < count; i++)
            {
                Vector2 current = input[i];
                bool curInside = IsInsideEdge(current, edge);

                if (curInside)
                {
                    if (!prevInside)
                    {
                        output.Add(IntersectEdge(previous, current, edge));
                    }
                    output.Add(current);
                }
                else if (prevInside)
                {
                    output.Add(IntersectEdge(previous, current, edge));
                }

                previous = current;
                prevInside = curInside;
            }
        }

        private static bool IsInsideEdge(Vector2 point, int edge)
        {
            switch (edge)
            {
                case 0: return point.x >= FieldMin.x; // Left
                case 1: return point.x <= FieldMax.x; // Right
                case 2: return point.y >= FieldMin.y; // Bottom
                default: return point.y <= FieldMax.y; // Top
            }
        }

        private static Vector2 IntersectEdge(Vector2 a, Vector2 b, int edge)
        {
            switch (edge)
            {
                case 0:
                {
                    float t = (FieldMin.x - a.x) / (b.x - a.x);
                    return new Vector2(FieldMin.x, a.y + t * (b.y - a.y));
                }
                case 1:
                {
                    float t = (FieldMax.x - a.x) / (b.x - a.x);
                    return new Vector2(FieldMax.x, a.y + t * (b.y - a.y));
                }
                case 2:
                {
                    float t = (FieldMin.y - a.y) / (b.y - a.y);
                    return new Vector2(a.x + t * (b.x - a.x), FieldMin.y);
                }
                default:
                {
                    float t = (FieldMax.y - a.y) / (b.y - a.y);
                    return new Vector2(a.x + t * (b.x - a.x), FieldMax.y);
                }
            }
        }

        // ─── Shared helpers ──────────────────────────────────────────────────────

        private static Vector3 GetSurfacePosition(Vector3 position)
        {
            position.y += DefaultSurfaceOffset;
            return position;
        }

        private Mesh GetShapeMesh()
        {
            if (shapeMesh == null)
            {
                shapeMesh = new Mesh { name = "SkillIndicatorShape" };
                shapeMesh.MarkDynamic();
            }

            return shapeMesh;
        }

        private static Material GetSharedMaterial()
        {
            if (sharedMaterial != null)
            {
                return sharedMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            sharedMaterial = new Material(shader);
            return sharedMaterial;
        }
    }
}
