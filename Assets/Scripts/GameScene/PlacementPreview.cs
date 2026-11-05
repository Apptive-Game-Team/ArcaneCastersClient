using Data.GameConfig;
using Data.Magic;
using GameScene.Dto;
using GameScene.Object;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene
{
    /// <summary>
    /// 유닛과 건물을 놓을 자리가 땅 위의 다른 몸과 겹치는지 미리 보고, 겹치면 가까운 빈자리를 찾는다.
    /// 서버 CastPlacement 와 같은 기준이다. 상수와 탐색 순서를 서버와 한 글자도 다르게 두면 안 된다.
    /// HP 가 있는 땅 위 몸과 <see cref="GroundBlockingCell"/> 을 단 강 맵의 물 칸만 장애물이고, 공중(높이 2 이상)에 뜬 몸은 겹쳐도 된다.
    /// 공중에 나타나는 소환(object parameter <c>spawn_height</c> 가 2 이상)은 막히지도 비켜 나지도 않는다.
    /// </summary>
    public static class PlacementPreview
    {
        private const float AerialStandardHeight = 2f;
        private const string ColliderGizmoCategory = "Collider";
        private const string HpGaugeCategory = "HP";

        /// <summary>두 몸의 반지름 합에 곱하는 비율. 0.6 이면 몸이 살짝 겹치는 자리까지 놓을 수 있다.</summary>
        public const float PlacementOverlapRatio = 0.6f;

        // 서버 MagicInputHandler 의 MAP_MIN_X / MAP_MAX_X / MAP_MIN_Z / MAP_MAX_Z 와 같은 값이다.
        // 이 밖의 자리는 서버가 시전을 거절하므로 빈자리 후보에서 뺀다.
        private const float MapMinX = 0f;
        private const float MapMaxX = 18f;
        private const float MapMinZ = 0f;
        private const float MapMaxZ = 10f;

        private const int SearchRingCount = 12;
        private const float SearchRingStep = 0.25f;
        private const int SearchDirections = 16;

        private const string SpawnHeightParameter = "spawn_height";
        private const string RadiusParameter = "radius";

        /// <summary>서버 CastPlacement.DEFAULT_BODY_RADIUS. 유닛에 radius parameter 가 없을 때 쓴다.</summary>
        private const float DefaultUnitBodyRadius = 0.3f;

        public static readonly Color BlockedFillColor = new Color(0.95f, 0.2f, 0.2f, 0.35f);

        /// <summary>빈자리가 없을 때 인디케이터 전체에 쓰는 색. 채움이 옅으면 안 보이므로 알파를 높였다.</summary>
        public static readonly Color NoSpotFillColor = new Color(0.95f, 0.15f, 0.15f, 0.55f);

        public static readonly Color NoSpotEdgeColor = new Color(1f, 0.15f, 0.15f, 0.95f);

        /// <summary>필드에 몸을 남기는 마법(유닛, 건물)만 자리를 검사한다.</summary>
        public static bool ChecksOverlap(CombinedMagicData magic)
        {
            return magic != null && MagicCastKinds.LeavesBody(magic.castKind);
        }

        /// <summary>
        /// 소환되는 몸이 공중에 나타나는지. <c>spawn_height</c> 가 2 이상이면 공중이다.
        /// 값이 없으면(아직 database 에 없으면) 땅으로 본다.
        /// </summary>
        public static bool SummonsAirborne(CombinedMagicData magic)
        {
            return GameParameterResolver.TryGetMagicParameter(magic, SpawnHeightParameter, out float height) &&
                   height >= AerialStandardHeight;
        }

        /// <summary>
        /// 유닛 몸 반지름. 서버처럼 radius parameter 를 읽고 없으면 <see cref="DefaultUnitBodyRadius"/> 다.
        /// 건물은 indicator 의 첫 원을 발자국으로 쓰므로 호출하는 쪽이 따로 정한다.
        /// </summary>
        public static float GetUnitBodyRadius(CombinedMagicData magic)
        {
            return GameParameterResolver.TryGetMagicParameter(magic, RadiusParameter, out float radius) && radius > 0f
                ? radius
                : DefaultUnitBodyRadius;
        }

        /// <summary>
        /// 조준점이 막히지 않았으면 그대로, 막혔으면 조준점 둘레 고리 1..12 (반지름 k*0.25)를 안쪽부터 돌며
        /// 첫 빈자리를 찾는다. 한 고리에서 방향 16개를 +x 부터 반시계로(x = cos, z = sin) 본다.
        /// 후보는 사거리 안이고 지도 안이어야 한다. 하나도 없으면 false.
        /// 막힌 경우에만 고리 탐색이 돌고, 어느 경로에서도 할당하지 않는다.
        /// </summary>
        public static bool TryResolveSpot(
            Vector3 aimPoint, Vector3 casterPosition, float range, float bodyRadius, out Vector3 spot)
        {
            if (!IsBlocked(aimPoint, bodyRadius))
            {
                spot = aimPoint;
                return true;
            }

            for (int ring = 1; ring <= SearchRingCount; ring++)
            {
                float distance = ring * SearchRingStep;
                for (int i = 0; i < SearchDirections; i++)
                {
                    float angle = 2f * Mathf.PI * i / SearchDirections;
                    Vector3 candidate = aimPoint;
                    candidate.x += Mathf.Cos(angle) * distance;
                    candidate.z += Mathf.Sin(angle) * distance;
                    if (!InsideMap(candidate) || GroundDistance(candidate, casterPosition) > range)
                    {
                        continue;
                    }

                    if (!IsBlocked(candidate, bodyRadius))
                    {
                        spot = candidate;
                        return true;
                    }
                }
            }

            spot = aimPoint;
            return false;
        }

        public static bool IsBlocked(Vector3 groundPoint, float radius)
        {
            ObjectContainer container = ObjectContainer.Instance;
            if (container == null)
            {
                return false;
            }

            foreach (ServedObject other in container.Values)
            {
                if (other == null)
                {
                    continue;
                }

                // 강 맵의 물 칸은 정사각형이라 몸 중심 거리가 아니라 칸의 가장 가까운 점까지 거리로 막는다.
                if (other.TryGetComponent(out GroundBlockingCell cell))
                {
                    Vector3 center = other.transform.position;
                    if (PlacementSquareRule.Blocks(
                            groundPoint.x, groundPoint.z, center.x, center.z, cell.HalfSize, radius))
                    {
                        return true;
                    }

                    continue;
                }

                if (!IsGroundBody(other, out float otherRadius))
                {
                    continue;
                }

                if (GroundDistance(groundPoint, other.transform.position) < (otherRadius + radius) * PlacementOverlapRatio)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GroundDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static bool InsideMap(Vector3 position)
        {
            return position.x >= MapMinX && position.x <= MapMaxX && position.z >= MapMinZ && position.z <= MapMaxZ;
        }

        private static bool IsGroundBody(ServedObject other, out float radius)
        {
            radius = 0f;
            if (other.transform.position.y >= AerialStandardHeight)
            {
                return false;
            }

            bool hasHp = false;
            foreach (var gauge in other.gauges)
            {
                if (gauge != null && gauge.category == HpGaugeCategory)
                {
                    hasHp = true;
                    break;
                }
            }

            return hasHp && other.TryGetGizmoRadius(ColliderGizmoCategory, out radius);
        }
    }
}
