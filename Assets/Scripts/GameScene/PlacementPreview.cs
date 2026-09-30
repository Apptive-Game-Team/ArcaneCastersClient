using Data.Magic;
using GameScene.Object;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene
{
    /// <summary>
    /// 건물을 놓을 자리가 땅 위의 다른 몸과 겹치는지 미리 본다. 서버 CastPlacement 와 같은 기준이다:
    /// HP 가 있는 땅 위 몸만 장애물이고, 공중(높이 2 이상)에 뜬 몸은 겹쳐도 된다.
    /// 유닛 소환은 공중 유닛인지 클라이언트가 알 수 없어 미리 막지 않고 서버 응답에 맡긴다.
    /// </summary>
    public static class PlacementPreview
    {
        private const float AerialStandardHeight = 2f;
        private const string ColliderGizmoCategory = "Collider";
        private const string HpGaugeCategory = "HP";

        public static readonly Color BlockedFillColor = new Color(0.95f, 0.2f, 0.2f, 0.35f);

        public static bool ChecksOverlap(CombinedMagicData magic)
        {
            return magic != null && magic.castKind == MagicCastKind.Building;
        }

        public static bool IsBlocked(Vector3 groundPoint, float radius)
        {
            ObjectContainer container = ObjectContainer.Instance;
            if (container == null)
            {
                return false;
            }

            Vector2 point = new Vector2(groundPoint.x, groundPoint.z);
            foreach (int id in container.GetIds())
            {
                ServedObject other = container.FindById(id);
                if (other == null || !IsGroundBody(other, out float otherRadius))
                {
                    continue;
                }

                Vector3 position = other.transform.position;
                if (Vector2.Distance(point, new Vector2(position.x, position.z)) < otherRadius + radius)
                {
                    return true;
                }
            }

            return false;
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
