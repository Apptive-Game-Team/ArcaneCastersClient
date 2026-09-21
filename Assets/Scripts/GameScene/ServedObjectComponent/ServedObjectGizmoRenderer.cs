#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GameScene.Dto.debug;
using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public class ServedObjectGizmoRenderer : MonoBehaviour
    {
        private const int GizmoCircleSegments = 40;
        private const float GizmoLineWidth = 0.035f;
        // Successive gizmos are nudged along the ground plane's normal (local up), not along
        // the plane itself, so overlapping shapes don't z-fight without shifting their footprint.
        private const float GizmoNormalOffsetStep = 0.01f;

        private static Material _gizmoLineMaterial;

        [SerializeField] private ServedObject servedObject;

        private readonly List<Gizmo> _gizmos = new List<Gizmo>();
        private readonly List<LineRenderer> _gizmoRenderers = new List<LineRenderer>();
        private Transform _gizmoContainer;
        private SpriteRenderer _spriteRenderer;

        public IReadOnlyList<Gizmo> Gizmos => _gizmos;

        private void Awake()
        {
            if (servedObject == null)
            {
                servedObject = GetComponent<ServedObject>();
            }
        }

        public void SetGizmos(List<Gizmo> gizmos)
        {
            _gizmos.Clear();
            if (gizmos != null)
            {
                _gizmos.AddRange(gizmos);
            }

            RebuildGizmoRenderers();
        }

        private void RebuildGizmoRenderers()
        {
            EnsureGizmoContainer();
            ClearGizmoRenderers();

            for (int i = 0; i < _gizmos.Count; i++)
            {
                Gizmo gizmo = _gizmos[i];
                if (gizmo == null)
                {
                    continue;
                }

                CreateGizmoRenderer(gizmo, i);
            }
        }

        private void EnsureGizmoContainer()
        {
            if (_gizmoContainer != null)
            {
                return;
            }

            GameObject gizmoContainerObject = new GameObject("DebugGizmos");
            _gizmoContainer = gizmoContainerObject.transform;
            _gizmoContainer.SetParent(GetAnchorTransform(), false);
            _gizmoContainer.localPosition = Vector3.zero;
            _gizmoContainer.localRotation = Quaternion.identity;
            _gizmoContainer.localScale = Vector3.one;
        }

        /// <summary>
        /// The transform <see cref="Gizmo.relativePosition"/> is relative to. This is the
        /// ServedObject's own transform, not <see cref="ServedObject.GetActualTransform"/>: that
        /// method can resolve to a child sprite transform carrying its own local offset and scale
        /// (for example <c>WindTotem.prefab</c>'s sprite, offset and scaled 1.5x), which would
        /// move and resize the gizmo away from the range the server actually described.
        /// <see cref="PositionUpdater"/> writes the server position onto this same transform.
        /// </summary>
        private Transform GetAnchorTransform()
        {
            if (servedObject != null)
            {
                return servedObject.transform;
            }

            return transform;
        }

        private void LateUpdate()
        {
            if (_gizmoContainer == null)
            {
                return;
            }

            // The anchor transform is the ServedObject's own transform, which SetMaster rotates
            // 180° on Y for RightPlayer to face the other side. SetGizmos and SetMaster can run in
            // either order, so correct the world rotation every frame instead of once at creation;
            // otherwise a RightPlayer gizmo would be flipped a second time on top of the
            // already-mirrored relativePosition the server sent.
            _gizmoContainer.rotation = Quaternion.identity;
        }

        private void ClearGizmoRenderers()
        {
            foreach (LineRenderer renderer in _gizmoRenderers)
            {
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
            }

            _gizmoRenderers.Clear();
        }

        private void CreateGizmoRenderer(Gizmo gizmo, int index)
        {
            GameObject gizmoObject = new GameObject(GetGizmoObjectName(gizmo, index));
            gizmoObject.transform.SetParent(_gizmoContainer, false);

            LineRenderer lineRenderer = gizmoObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.widthMultiplier = GizmoLineWidth;
            lineRenderer.positionCount = 0;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            // The shape now lies flat in the ground XZ plane instead of standing in the
            // container's local XY plane, and the container's rotation is kept at world identity
            // (see LateUpdate), so TransformZ would face the ribbon along world Z — edge-on to the
            // tilted camera and barely visible for segments running along X. View billboards each
            // segment's width to the camera instead, so the outline reads at a consistent
            // thickness no matter which way a segment runs on the ground.
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.sharedMaterial = GetGizmoLineMaterial();
            ApplyGizmoSorting(lineRenderer, index);
            lineRenderer.startColor = GetGizmoColor(gizmo.category);
            lineRenderer.endColor = lineRenderer.startColor;

            WDebug.Log($"[Gizmo] Creating gizmo renderer for gizmo at relative position {gizmo.relativePosition}, type: {gizmo.type}, category: {gizmo.category}");
            Vector3[] points = BuildGizmoPoints(gizmo, index);
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);

            _gizmoRenderers.Add(lineRenderer);
        }

        private static string GetGizmoObjectName(Gizmo gizmo, int index)
        {
            string category = string.IsNullOrWhiteSpace(gizmo.category) ? "Unknown" : gizmo.category;
            string type = string.IsNullOrWhiteSpace(gizmo.type) ? "Shape" : gizmo.type;
            return $"Gizmo_{index}_{category}_{type}";
        }

        private void ApplyGizmoSorting(LineRenderer lineRenderer, int index)
        {
            EnsureSpriteRenderer();
            if (_spriteRenderer != null)
            {
                lineRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
                lineRenderer.sortingOrder = _spriteRenderer.sortingOrder + 20 + index;
                return;
            }

            lineRenderer.sortingLayerName = "Default";
            lineRenderer.sortingOrder = 500 + index;
        }

        private void EnsureSpriteRenderer()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private static Material GetGizmoLineMaterial()
        {
            if (_gizmoLineMaterial != null)
            {
                return _gizmoLineMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            _gizmoLineMaterial = new Material(shader);
            return _gizmoLineMaterial;
        }

        private static Color GetGizmoColor(string category)
        {
            switch (category)
            {
                case "Collider":
                    return new Color(0.27f, 0.87f, 0.44f, 0.95f);
                case "AttackRange":
                    return new Color(0.95f, 0.28f, 0.28f, 0.95f);
                case "AreaOfEffect":
                    return new Color(0.98f, 0.58f, 0.17f, 0.95f);
                case "DetectionRange":
                    return new Color(0.28f, 0.75f, 1f, 0.95f);
                case "SpawnArea":
                    return new Color(0.72f, 0.41f, 0.97f, 0.95f);
                default:
                    return new Color(1f, 0.95f, 0.35f, 0.95f);
            }
        }

        private static Vector3[] BuildGizmoPoints(Gizmo gizmo, int index)
        {
            Vector3 center = gizmo.relativePosition + Vector3.up * (index * GizmoNormalOffsetStep);
            if (string.Equals(gizmo.type, "Box", StringComparison.OrdinalIgnoreCase))
            {
                return BuildBoxPoints(center, gizmo.boxSize);
            }

            return BuildCirclePoints(center, gizmo.radius);
        }

        // World X-Z is the ground plane and Y is height, same as the ranges the server sends
        // (relativePosition, boxSize) and SkillIndicatorShapeRenderer clips to, so both shapes are
        // built in local X-Z rather than local X-Y.
        private static Vector3[] BuildCirclePoints(Vector3 center, float radius)
        {
            float safeRadius = Mathf.Max(radius, 0.05f);
            Vector3[] points = new Vector3[GizmoCircleSegments];

            for (int i = 0; i < GizmoCircleSegments; i++)
            {
                float angle = Mathf.PI * 2f * i / GizmoCircleSegments;
                float x = Mathf.Cos(angle) * safeRadius;
                float z = Mathf.Sin(angle) * safeRadius;
                points[i] = center + new Vector3(x, 0f, z);
            }

            return points;
        }

        private static Vector3[] BuildBoxPoints(Vector3 center, Vector3 boxSize)
        {
            WDebug.Log($"[Gizmo] Building box points for gizmo at {center} with size {boxSize}");
            // boxSize.y is the height of a ground-plane footprint and plays no part in this
            // outline, so only x and z are floored. z keeps a 0 floor rather than x's 0.1 floor: a
            // zero-depth box (for example a launcher's line-shaped attack range) is meant to
            // collapse into a line along x, not thicken into a band.
            float halfWidth = Mathf.Max(boxSize.x, 0.1f) * 0.5f;
            float halfDepth = Mathf.Max(boxSize.z, 0f) * 0.5f;

            return new[]
            {
                center + new Vector3(-halfWidth, 0f, -halfDepth),
                center + new Vector3(-halfWidth, 0f, halfDepth),
                center + new Vector3(halfWidth, 0f, halfDepth),
                center + new Vector3(halfWidth, 0f, -halfDepth),
            };
        }

        private void OnDestroy()
        {
            ClearGizmoRenderers();
        }
    }
}
#endif
