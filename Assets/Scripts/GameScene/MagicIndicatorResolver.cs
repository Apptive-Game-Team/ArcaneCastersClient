using System.Collections.Generic;
using Data.GameConfig;
using Data.Magic;
using Global;
using Global.Serialization;
using UnityEngine;

namespace GameScene
{
    /// <summary>indicator layer 하나를 그릴 수 있는 도형으로 푼 결과.</summary>
    public readonly struct ResolvedIndicatorShape
    {
        public enum Kind
        {
            Circle,
            Lane
        }

        public readonly Kind kind;

        /// <summary>circle 이면 중심, lane 이면 시작점.</summary>
        public readonly Vector3 origin;

        /// <summary>lane 이 향하는 점. <see cref="SkillIndicatorShapeRenderer.SetLine"/> 은 방향만 쓴다.</summary>
        public readonly Vector3 target;

        /// <summary>circle 반지름.</summary>
        public readonly float radius;

        /// <summary>lane 길이.</summary>
        public readonly float length;

        /// <summary>lane 의 중심선에서 가장자리까지 거리.</summary>
        public readonly float halfWidth;

        /// <summary>circle 테두리 두께. 0 이면 속을 채운다.</summary>
        public readonly float edgeWidth;

        /// <summary>
        /// <c>grounded: false</c> lane 이 조준점 높이를 따를 때만 true. 바닥 그림자 외에 3D 공중 선을
        /// <see cref="airStart"/> 에서 <see cref="airEnd"/> 까지 그린다.
        /// </summary>
        public readonly bool hasAirLine;

        public readonly Vector3 airStart;
        public readonly Vector3 airEnd;

        private ResolvedIndicatorShape(Kind kind, Vector3 origin, Vector3 target,
            float radius, float length, float halfWidth, float edgeWidth,
            bool hasAirLine = false, Vector3 airStart = default, Vector3 airEnd = default)
        {
            this.kind = kind;
            this.origin = origin;
            this.target = target;
            this.radius = radius;
            this.length = length;
            this.halfWidth = halfWidth;
            this.edgeWidth = edgeWidth;
            this.hasAirLine = hasAirLine;
            this.airStart = airStart;
            this.airEnd = airEnd;
        }

        public static ResolvedIndicatorShape Circle(Vector3 center, float radius, float edgeWidth)
        {
            return new ResolvedIndicatorShape(Kind.Circle, center, center, radius, 0f, 0f, edgeWidth);
        }

        public static ResolvedIndicatorShape Lane(Vector3 start, Vector3 target, float length, float halfWidth)
        {
            return new ResolvedIndicatorShape(Kind.Lane, start, target, 0f, length, halfWidth, 0f);
        }

        public static ResolvedIndicatorShape Lane(
            Vector3 start, Vector3 target, float length, float halfWidth, Vector3 airStart, Vector3 airEnd)
        {
            return new ResolvedIndicatorShape(
                Kind.Lane, start, target, 0f, length, halfWidth, 0f, true, airStart, airEnd);
        }
    }

    /// <summary>
    /// 마법의 indicator document 와 지금 위치를 받아 그릴 도형 목록을 내놓는다.
    /// <para>
    /// <see cref="FieldSelector"/> 는 입력과 GameObject 수명만 맡고, 무엇을 그릴지는 여기서 정한다.
    /// </para>
    /// <para>
    /// 결과는 호출자가 들고 있는 list 에 담는다. 이 경로는 매 프레임 돌기 때문에
    /// list 를 새로 만들어 돌려주면 프레임마다 GC 가 생긴다.
    /// </para>
    /// </summary>
    public static class MagicIndicatorResolver
    {
        /// <summary>indicator document 가 없을 때 쓰는 반지름 parameter 이름.</summary>
        private const string FallbackRadiusParameter = "radius";

        private const string LeftPlayerMaster = "LeftPlayer";

        /// <summary>조준점 높이가 시전자와 이만큼 넘게 달라야 공중 선을 그린다.</summary>
        private const float AirLineHeightEpsilon = 0.01f;

        // document 없는 마법을 마법당 한 번만 log 하기 위한 표. 프레임마다 경고가 쏟아지면 안 된다.
        private static readonly HashSet<string> LoggedMissingDocument = new HashSet<string>();

        /// <summary>
        /// 시전자가 보는 방향. 필드는 X 축으로 놓여 있다. <c>InteractiveTutorialScene.unity</c> 에서
        /// LeftPlayer 가 x=1, RightPlayer 가 x=17 이고 필드는 월드 X ∈ [0, 18] 이므로,
        /// LeftPlayer 는 X 가 커지는 쪽(<see cref="Vector3.right"/>),
        /// RightPlayer 는 X 가 작아지는 쪽(<see cref="Vector3.left"/>)을 본다.
        /// </summary>
        public static Vector3 GetForwardDirection()
        {
            return SceneContext.Me == LeftPlayerMaster ? Vector3.right : Vector3.left;
        }

        /// <summary>
        /// 튜토리얼이 옛 <c>aimShape</c> 자리에 쓰던 갈림길. 첫 번째로 읽을 수 있는 layer 가 lane 인지 본다.
        /// </summary>
        public static bool IsLaneAim(CombinedMagicData magic)
        {
            MagicIndicatorDocument document = magic?.indicator;
            if (document == null || !document.Prepare(magic.serverName))
            {
                return false;
            }

            for (int i = 0; i < document.layers.Count; i++)
            {
                MagicIndicatorLayer layer = document.layers[i];
                if (layer != null && layer.IsReadable)
                {
                    return layer.Shape == MagicIndicatorShape.Lane;
                }
            }

            return false;
        }

        /// <summary>
        /// <paramref name="shapes"/> 를 비우고 이 프레임에 그릴 도형을 순서대로 채운다.
        /// 값을 풀지 못한 layer 는 건너뛰고 나머지는 그대로 그린다.
        /// </summary>
        /// <param name="range">마법의 cast range. <c>end: "target"</c> lane 의 길이다.</param>
        public static void Resolve(
            CombinedMagicData magic,
            Vector3 casterPosition,
            Vector3 targetPosition,
            Vector3 forward,
            float range,
            List<ResolvedIndicatorShape> shapes)
        {
            Resolve(magic, casterPosition, targetPosition, targetPosition, forward, range, shapes);
        }

        /// <summary>
        /// <paramref name="aimPoint"/> 는 높이를 포함한 조준점이다. <paramref name="targetPosition"/> 은 그 발자국
        /// (지면에 놓인 점)이라 circle 과 지면 lane 은 이것만 쓰고, <c>grounded: false</c> lane 만 aimPoint 의 높이를 쓴다.
        /// 유닛을 겨누지 않으면 둘은 같다.
        /// </summary>
        public static void Resolve(
            CombinedMagicData magic,
            Vector3 casterPosition,
            Vector3 targetPosition,
            Vector3 aimPoint,
            Vector3 forward,
            float range,
            List<ResolvedIndicatorShape> shapes)
        {
            shapes.Clear();
            if (magic == null)
            {
                return;
            }

            MagicIndicatorDocument document = magic.indicator;
            if (document == null || !document.Prepare(magic.serverName))
            {
                AppendFallbackShape(magic, targetPosition, shapes);
                return;
            }

            for (int i = 0; i < document.layers.Count; i++)
            {
                MagicIndicatorLayer layer = document.layers[i];
                if (layer == null || !layer.IsReadable)
                {
                    continue;
                }

                if (TryResolveLayer(magic, layer, casterPosition, targetPosition, aimPoint, forward, range,
                        out ResolvedIndicatorShape shape))
                {
                    shapes.Add(shape);
                }
            }
        }

        /// <summary>
        /// document 가 없거나 읽을 수 없는 마법은 이 변경 전과 똑같이 조준점에 <c>radius</c> 만큼 채운 원을 그린다.
        /// </summary>
        private static void AppendFallbackShape(
            CombinedMagicData magic,
            Vector3 targetPosition,
            List<ResolvedIndicatorShape> shapes)
        {
            if (LoggedMissingDocument.Add(magic.serverName))
            {
                WDebug.LogWarning(
                    $"[MagicIndicator] {magic.serverName}: no usable indicator document, drawing the {FallbackRadiusParameter} circle.");
            }

            if (!GameParameterResolver.TryGetMagicParameter(magic, FallbackRadiusParameter, out float radius))
            {
                return;
            }

            shapes.Add(ResolvedIndicatorShape.Circle(targetPosition, radius, 0f));
        }

        private static bool TryResolveLayer(
            CombinedMagicData magic,
            MagicIndicatorLayer layer,
            Vector3 casterPosition,
            Vector3 targetPosition,
            Vector3 aimPoint,
            Vector3 forward,
            float range,
            out ResolvedIndicatorShape shape)
        {
            shape = default;
            Vector3 origin = layer.Origin == MagicIndicatorOrigin.Caster ? casterPosition : targetPosition;

            if (layer.Shape == MagicIndicatorShape.Circle)
            {
                return TryResolveCircle(magic, layer, origin, forward, out shape);
            }

            return TryResolveLane(magic, layer, origin, targetPosition, aimPoint, forward, range, out shape);
        }

        private static bool TryResolveCircle(
            CombinedMagicData magic,
            MagicIndicatorLayer layer,
            Vector3 origin,
            Vector3 forward,
            out ResolvedIndicatorShape shape)
        {
            shape = default;
            if (!TryResolveValue(magic, layer.radius, out float radius))
            {
                return false;
            }

            // forwardOffset 과 edgeWidth 는 없으면 0 이다. 다만 적혀 있는데 값을 풀지 못하면
            // 0 으로 때우지 않고 layer 를 건너뛴다. 0 으로 그려도 되는 자리는 document 가 fallback 을 적는다.
            if (!TryResolveOptionalValue(magic, layer.forwardOffset, out float forwardOffset))
            {
                return false;
            }

            if (!TryResolveOptionalValue(magic, layer.edgeWidth, out float edgeWidth))
            {
                return false;
            }

            shape = ResolvedIndicatorShape.Circle(origin + forward * forwardOffset, radius, edgeWidth);
            return true;
        }

        private static bool TryResolveLane(
            CombinedMagicData magic,
            MagicIndicatorLayer layer,
            Vector3 origin,
            Vector3 targetPosition,
            Vector3 aimPoint,
            Vector3 forward,
            float range,
            out ResolvedIndicatorShape shape)
        {
            shape = default;
            if (!TryResolveValue(magic, layer.halfWidth, out float halfWidth))
            {
                return false;
            }

            if (layer.End == MagicIndicatorLaneEnd.Target)
            {
                // length 가 없으면 이 변경 전의 직선 조준과 같다 — 조준점 쪽을 향하고 길이는 cast range 다.
                // 적혀 있으면 그 길이로 자른다. vine_toss 처럼 사거리가 cast range 도, 커서까지 거리도
                // 아닌 고정 값인 마법이 있다.
                if (!TryResolveOptionalValue(magic, layer.length, out float targetLength))
                {
                    return false;
                }

                float laneLength = layer.length.IsPresent ? targetLength : range;
                if (layer.Grounded)
                {
                    shape = ResolvedIndicatorShape.Lane(origin, targetPosition, laneLength, halfWidth);
                    return true;
                }

                // 빔은 laneLength 만큼 3D 로 나간다. 바닥에는 그 그림자(수평 성분)만 눕는다.
                Vector3 toAim = aimPoint - origin;
                float fullDistance = toAim.magnitude;
                float horizontalDistance = GetGroundDistance(origin, aimPoint);
                float shadowLength = fullDistance > Mathf.Epsilon
                    ? laneLength * horizontalDistance / fullDistance
                    : laneLength;
                shape = CreateLane(origin, targetPosition, shadowLength, halfWidth, toAim, fullDistance, laneLength);
                return true;
            }

            if (layer.End == MagicIndicatorLaneEnd.Aim)
            {
                float groundDistance = GetGroundDistance(origin, targetPosition);
                if (layer.Grounded)
                {
                    shape = ResolvedIndicatorShape.Lane(origin, targetPosition, groundDistance, halfWidth);
                    return true;
                }

                Vector3 toAim = aimPoint - origin;
                float fullDistance = toAim.magnitude;
                shape = CreateLane(origin, targetPosition, groundDistance, halfWidth, toAim, fullDistance, fullDistance);
                return true;
            }

            if (!TryResolveValue(magic, layer.length, out float length))
            {
                return false;
            }

            shape = ResolvedIndicatorShape.Lane(origin, origin + forward, length, halfWidth);
            return true;
        }

        /// <summary>
        /// <c>grounded: false</c> lane 을 만든다. 조준점 높이가 origin 과 <see cref="AirLineHeightEpsilon"/> 넘게
        /// 다를 때만 origin 에서 <c>origin + normalize(toAim) * airLength</c> 까지 가는 공중 선을 붙인다.
        /// </summary>
        private static ResolvedIndicatorShape CreateLane(
            Vector3 origin, Vector3 targetPosition, float groundLength, float halfWidth,
            Vector3 toAim, float fullDistance, float airLength)
        {
            if (fullDistance <= Mathf.Epsilon || Mathf.Abs(toAim.y) <= AirLineHeightEpsilon)
            {
                return ResolvedIndicatorShape.Lane(origin, targetPosition, groundLength, halfWidth);
            }

            return ResolvedIndicatorShape.Lane(
                origin, targetPosition, groundLength, halfWidth, origin, origin + toAim / fullDistance * airLength);
        }

        /// <summary>
        /// 지면에 눕는 도형의 길이라 높이는 빼고 잰다. <see cref="FieldSelector"/> 가 조준점을 cast range
        /// 로 자를 때 쓰는 거리와 같은 값이어야, <c>end: "aim"</c> lane 이 잘린 조준점에서 정확히 끝난다.
        /// 카메라 평면 거리(<c>ProjectileUtil.GetCameraPlaneLength</c>)는 sprite 를 늘릴 때 쓰는 것이고
        /// 여기서는 틀린다.
        /// </summary>
        private static float GetGroundDistance(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            return offset.magnitude;
        }

        /// <summary>
        /// 숫자면 그대로, parameter 이름이면 parameter 표에서 읽는다. object 이름이 함께 있으면
        /// 마법 자신의 game object 가 아니라 그 이름의 game object 에서 읽는다.
        /// 못 찾고 fallback 도 없으면 풀지 못한 것이다.
        /// </summary>
        private static bool TryResolveValue(CombinedMagicData magic, in MagicIndicatorValue value, out float resolved)
        {
            resolved = 0f;
            if (!value.IsPresent)
            {
                return false;
            }

            if (!value.IsParameter)
            {
                resolved = value.Number;
                return true;
            }

            bool found = value.HasObject
                ? GameParameterResolver.TryGetObjectParameter(value.ObjectName, value.ParameterName, out resolved)
                : GameParameterResolver.TryGetMagicParameter(magic, value.ParameterName, out resolved);

            if (found)
            {
                return true;
            }

            if (value.HasFallback)
            {
                resolved = value.FallbackNumber;
                return true;
            }

            resolved = 0f;
            return false;
        }

        /// <summary>적혀 있지 않으면 0 으로 성공. 적혀 있으면 <see cref="TryResolveValue"/> 와 같다.</summary>
        private static bool TryResolveOptionalValue(
            CombinedMagicData magic,
            in MagicIndicatorValue value,
            out float resolved)
        {
            if (!value.IsPresent)
            {
                resolved = 0f;
                return true;
            }

            return TryResolveValue(magic, value, out resolved);
        }
    }
}
