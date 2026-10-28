using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public readonly struct RayHit {
        public readonly SimObject Object;
        public readonly Fix64 Distance;
        public readonly Vector3 Point;
        public RayHit(SimObject owner, Fix64 distance, Vector3 point) { Object = owner; Distance = distance; Point = point; }
    }
    public static class RayQueries {
        public static RayHit[] RaycastAll(SimulationContext context, Vector3 origin, Vector3 direction, Fix64 maximum) {
            var length = direction.Length(); if (length == Fix64.Zero) return new RayHit[0];
            var normalized = new Vector3(direction.X / length, direction.Y / length, direction.Z / length); var hits = new List<RayHit>();
            foreach (var owner in context.Objects) {
                // 원본은 첫 collider가 원형일 때만 검사하고 객체 상태와 trigger는 거르지 않는다.
                if (!(owner.Colliders.FirstOrDefault() is CircleCollider circle)) continue;
                var offset = circle.Position - origin; var along = Vector3.Dot(offset, normalized);
                var perpendicularSquared = Vector3.Dot(offset, offset) - along * along; var radiusSquared = circle.Radius * circle.Radius;
                if (perpendicularSquared > radiusSquared) continue;
                var distance = along - Fix64.Sqrt(radiusSquared - perpendicularSquared);
                if (distance < 0 || distance > maximum) continue;
                hits.Add(new RayHit(owner, distance, origin + normalized * distance));
            }
            return hits.OrderBy(hit => hit.Distance).ToArray();
        }
    }
}
