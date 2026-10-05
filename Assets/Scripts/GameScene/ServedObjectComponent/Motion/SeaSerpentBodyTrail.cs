using System;
using System.Collections.Generic;
using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    /// <summary>
    /// Draws the sea serpent's body as a chain of segment sprites that follow the ground path the
    /// head walked, with a sine wave laid across them so the body slithers while standing still.
    /// <para>
    /// The segments are not children of the head sprite. The head carries the idle hop, the attack
    /// swing and <see cref="SpriteRenderer.flipX"/>, and a body parented under it would swing and
    /// mirror along with it instead of trailing where the serpent actually walked.
    /// </para>
    /// </summary>
    public class SeaSerpentBodyTrail : ServedObjectBehaviour
    {
        private const string BodyRootName = "SeaSerpentBody";

        /// <summary>The master name <see cref="ServedObject.SetMaster"/> reads; its units face -X.</summary>
        private const string RightPlayerMaster = "RightPlayer";

        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private Sprite segmentSprite;
        [SerializeField] private Sprite tailSprite;

        [SerializeField] private int segmentCount = 6;

        /// <summary>Path distance between two segments, world units. The server collider radius is 1.2.</summary>
        [SerializeField] private float segmentSpacing = 0.45f;

        [SerializeField] private float segmentScale = 1f;

        /// <summary>Scale of the last segment relative to the first, so the body tapers to the tail.</summary>
        [SerializeField] private float tailScaleRatio = 0.55f;

        /// <summary>Sideways swing of one segment, world units, measured in the camera plane.</summary>
        [SerializeField] private float slitherAmplitude = 0.18f;

        [SerializeField] private float slitherSpeed = 4.5f;

        /// <summary>Phase the wave loses per segment, radians. Larger values curl the body tighter.</summary>
        [SerializeField] private float slitherPhaseStep = 0.9f;

        /// <summary>
        /// How far a segment may tilt off screen-horizontal. A serpent walking toward the camera
        /// aims its body straight down the screen, which would stand every hump on end, so the lean
        /// is capped rather than free.
        /// </summary>
        [SerializeField] private float maxLeanDegrees = 55f;

        /// <summary>Distance the head walks before the recorded path gains another point, world units.</summary>
        [SerializeField] private float trailSampleSpacing = 0.12f;

        /// <summary>Recorded head positions, newest first. Index 0 is always the head itself.</summary>
        private readonly List<Vector3> trail = new List<Vector3>();

        private Transform bodyRoot;
        private Transform[] segments;
        private SpriteRenderer[] segmentRenderers;
        private Vector3[] pathPositions;
        private Vector3[] segmentPositions;

        protected override void OnBound()
        {
            if (Owner == null || segmentCount <= 0)
            {
                return;
            }

            if (segmentSprite == null)
            {
                WDebug.LogWarning($"{nameof(SeaSerpentBodyTrail)} on {name} has no segment sprite; the body is not drawn.");
                return;
            }

            BuildSegments();
            SeedTrail(Owner.transform.position);
            Owner.OnDestroyed += RemoveSegments;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnDestroyed -= RemoveSegments;
            }

            RemoveSegments();
        }

        /// <summary>
        /// Runs after the move tween so the body reads the head position of the frame the player
        /// sees, not the one it had before <see cref="PositionUpdater"/> advanced it.
        /// </summary>
        private void LateUpdate()
        {
            if (Owner == null || bodyRoot == null)
            {
                return;
            }

            Vector3 headPosition = Owner.transform.position;
            RecordPath(headPosition);
            PlaceSegments(headPosition);
        }

        private void BuildSegments()
        {
            SpriteRenderer resolvedHeadRenderer = ResolveRenderer(headRenderer);

            bodyRoot = new GameObject(BodyRootName).transform;
            bodyRoot.SetParent(transform, false);
            bodyRoot.localPosition = Vector3.zero;
            bodyRoot.localRotation = Quaternion.identity;
            bodyRoot.localScale = Vector3.one;

            segments = new Transform[segmentCount];
            segmentRenderers = new SpriteRenderer[segmentCount];
            pathPositions = new Vector3[segmentCount];
            segmentPositions = new Vector3[segmentCount];

            for (int i = 0; i < segmentCount; i++)
            {
                GameObject segment = new GameObject($"{BodyRootName}_{i}");
                segment.transform.SetParent(bodyRoot, false);

                bool isTail = i == segmentCount - 1;
                SpriteRenderer segmentRenderer = segment.AddComponent<SpriteRenderer>();
                segmentRenderer.sprite = isTail && tailSprite != null ? tailSprite : segmentSprite;

                if (resolvedHeadRenderer != null)
                {
                    segmentRenderer.sharedMaterial = resolvedHeadRenderer.sharedMaterial;
                    segmentRenderer.sortingLayerID = resolvedHeadRenderer.sortingLayerID;

                    // Falls one step per segment so the head draws in front of its own body.
                    segmentRenderer.sortingOrder = resolvedHeadRenderer.sortingOrder - (i + 1);
                }

                float scale = segmentScale * Mathf.Lerp(1f, tailScaleRatio, GetTaper(i));
                segment.transform.localScale = new Vector3(scale, scale, 1f);

                segments[i] = segment.transform;
                segmentRenderers[i] = segmentRenderer;
            }
        }

        private float GetTaper(int index)
        {
            return segmentCount <= 1 ? 0f : (float) index / (segmentCount - 1);
        }

        private void RemoveSegments()
        {
            if (bodyRoot != null)
            {
                Destroy(bodyRoot.gameObject);
            }

            bodyRoot = null;
            segments = null;
            segmentRenderers = null;
            trail.Clear();
        }

        /// <summary>
        /// Keeps index 0 on the head and drops a new anchor once the head has walked
        /// <see cref="trailSampleSpacing"/> from the last one.
        /// </summary>
        private void RecordPath(Vector3 headPosition)
        {
            if (trail.Count == 0)
            {
                SeedTrail(headPosition);
                return;
            }

            trail[0] = headPosition;

            if (trail.Count < 2 ||
                (headPosition - trail[1]).sqrMagnitude >= trailSampleSpacing * trailSampleSpacing)
            {
                trail.Insert(1, headPosition);
            }

            TrimPath();
        }

        /// <summary>
        /// Lays a straight path behind the head so a serpent that has not moved yet still shows a
        /// body. LeftPlayer's units face +X and RightPlayer's face -X, so the body starts opposite.
        /// </summary>
        private void SeedTrail(Vector3 headPosition)
        {
            bool facesLeft = Owner != null &&
                             string.Equals(Owner.GetMaster(), RightPlayerMaster, StringComparison.Ordinal);
            Vector3 facing = facesLeft ? Vector3.left : Vector3.right;

            trail.Clear();
            trail.Add(headPosition);
            trail.Add(headPosition - facing * GetRequiredPathLength());
        }

        private void TrimPath()
        {
            float required = GetRequiredPathLength();
            float kept = 0f;

            for (int i = 0; i + 1 < trail.Count; i++)
            {
                kept += Vector3.Distance(trail[i], trail[i + 1]);
                if (kept < required)
                {
                    continue;
                }

                int firstUnused = i + 2;
                if (firstUnused < trail.Count)
                {
                    trail.RemoveRange(firstUnused, trail.Count - firstUnused);
                }

                return;
            }
        }

        private float GetRequiredPathLength()
        {
            return segmentSpacing * (segmentCount + 1);
        }

        /// <summary>
        /// The point <paramref name="distanceBehindHead"/> back along the recorded path. The path is
        /// walked on the ground, so its length is a plain world distance — the server's speed is in
        /// world units. This is not a length read off a billboarded sprite, which is the case
        /// scene-space.md warns about.
        /// </summary>
        private Vector3 GetPathPoint(float distanceBehindHead)
        {
            if (trail.Count == 0)
            {
                return transform.position;
            }

            float remaining = distanceBehindHead;
            for (int i = 0; i + 1 < trail.Count; i++)
            {
                float step = Vector3.Distance(trail[i], trail[i + 1]);
                if (step <= Mathf.Epsilon)
                {
                    continue;
                }

                if (remaining <= step)
                {
                    return Vector3.Lerp(trail[i], trail[i + 1], remaining / step);
                }

                remaining -= step;
            }

            return trail[trail.Count - 1];
        }

        private void PlaceSegments(Vector3 headPosition)
        {
            Camera camera = GameScene.Object.PresentationWorld.CameraFor(this);

            for (int i = 0; i < segmentCount; i++)
            {
                pathPositions[i] = GetPathPoint(segmentSpacing * (i + 1));
            }

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 aheadPosition = i == 0 ? headPosition : pathPositions[i - 1];
                Vector2 travelDirection = GetScreenDirection(pathPositions[i], aheadPosition);
                float wave = slitherAmplitude * Mathf.Sin(Time.time * slitherSpeed - i * slitherPhaseStep);
                segmentPositions[i] = pathPositions[i] + GetLateralOffset(camera, travelDirection, wave);
            }

            for (int i = 0; i < segmentCount; i++)
            {
                Transform segment = segments[i];
                if (segment == null)
                {
                    continue;
                }

                Vector3 aheadPosition = i == 0 ? headPosition : segmentPositions[i - 1];
                Vector2 towardAhead = GetScreenDirection(segmentPositions[i], aheadPosition);
                segment.position = segmentPositions[i];
                segment.rotation = GetLeanRotation(camera, towardAhead);

                // The tail sprite's fin points right, so it is mirrored whenever the rest of the
                // body lies to its right and the tip has to trail away to the left.
                if (i == segmentCount - 1 && segmentRenderers[i] != null)
                {
                    segmentRenderers[i].flipX = towardAhead.x > 0f;
                }
            }
        }

        /// <summary>
        /// Screen-plane direction from one world point to another, read the way
        /// <c>ProjectileUtil.GetRotation</c> reads it. The camera is tilted, so a world delta is not
        /// the direction the player sees.
        /// </summary>
        private Vector2 GetScreenDirection(Vector3 from, Vector3 to)
        {
            Camera camera = GameScene.Object.PresentationWorld.CameraFor(this);
            Vector3 delta = camera != null
                ? camera.WorldToScreenPoint(to) - camera.WorldToScreenPoint(from)
                : to - from;

            Vector2 direction = new Vector2(delta.x, delta.y);
            return direction.sqrMagnitude < Mathf.Epsilon ? Vector2.zero : direction.normalized;
        }

        /// <summary>
        /// The slither offset, taken perpendicular to travel inside the camera plane so the body
        /// waves across the player's view at every heading. A world-space perpendicular would
        /// shrink to nothing when the serpent walks along +Z.
        /// </summary>
        private static Vector3 GetLateralOffset(Camera camera, Vector2 travelDirection, float distance)
        {
            if (travelDirection == Vector2.zero || Mathf.Approximately(distance, 0f))
            {
                return Vector3.zero;
            }

            Vector2 screenNormal = new Vector2(-travelDirection.y, travelDirection.x);
            if (camera == null)
            {
                return new Vector3(screenNormal.x, screenNormal.y, 0f) * distance;
            }

            return (camera.transform.right * screenNormal.x + camera.transform.up * screenNormal.y) * distance;
        }

        /// <summary>
        /// Lays a segment along the line toward the point ahead of it, in the screen plane and about
        /// the camera's forward axis, the same construction <c>ProjectileUtil.GetRotation</c> uses.
        /// Zero lean is screen-right because the segment sprite is a hump whose back runs along
        /// local +X. The hump is symmetric, so a body heading left needs no mirrored copy: the angle
        /// folds into [-90, 90] and the hump lies on the same line either way.
        /// </summary>
        private Quaternion GetLeanRotation(Camera camera, Vector2 direction)
        {
            float lean = 0f;
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float folded = Mathf.Abs(Mathf.DeltaAngle(0f, angle)) > 90f
                    ? Mathf.DeltaAngle(180f, angle)
                    : Mathf.DeltaAngle(0f, angle);
                lean = Mathf.Clamp(folded, -maxLeanDegrees, maxLeanDegrees);
            }

            Quaternion screenRotation = Quaternion.AngleAxis(lean, Vector3.forward);
            return camera != null ? camera.transform.rotation * screenRotation : screenRotation;
        }
    }
}
