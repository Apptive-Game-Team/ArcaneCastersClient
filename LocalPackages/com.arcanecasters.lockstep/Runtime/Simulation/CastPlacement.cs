using System;
using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public readonly struct PlacementBody {
        public readonly Vector3 Position;
        public readonly Fix64 Radius;
        public readonly bool Active, Destroyed, Damageable, Solid;
        public PlacementBody(Vector3 position, Fix64 radius, bool active = true, bool destroyed = false, bool damageable = true, bool solid = true) {
            if (radius < Fix64.Zero) throw new ArgumentOutOfRangeException(nameof(radius));
            Position = position; Radius = radius; Active = active; Destroyed = destroyed; Damageable = damageable; Solid = solid;
        }
    }

    public static class CastPlacement {
        public static readonly Fix64 OverlapRatio = (Fix64)0.6m;
        public static readonly Fix64 RingStep = (Fix64)0.25m;
        public const int Rings = 12, Directions = 16;

        public static bool InsideMap(Vector3 point) => point.X >= Fix64.Zero && point.X <= (Fix64)18
            && point.Z >= Fix64.Zero && point.Z <= (Fix64)10;

        // water[x,z]는 원본 Terrain의 물 칸이다. 다리 칸은 false여야 한다.
        public static bool Blocked(Vector3 point, Fix64 radius, bool airborne, IReadOnlyList<PlacementBody> bodies, bool[,] water) {
            if (radius < Fix64.Zero) throw new ArgumentOutOfRangeException(nameof(radius));
            if (airborne) return false;
            Fix64 waterRadius = radius * OverlapRatio;
            if (water != null) {
                for (int x = 0; x < water.GetLength(0); x++)
                    for (int z = 0; z < water.GetLength(1); z++) {
                        if (!water[x, z]) continue;
                        Fix64 dx = point.X - MathHelper.Clamp(point.X, (Fix64)x, (Fix64)(x + 1));
                        Fix64 dz = point.Z - MathHelper.Clamp(point.Z, (Fix64)z, (Fix64)(z + 1));
                        if (dx * dx + dz * dz < waterRadius * waterRadius) return true;
                    }
            }
            foreach (var body in bodies) {
                if (!body.Active || body.Destroyed || !body.Damageable || !body.Solid || body.Position.Y >= (Fix64)2) continue;
                Fix64 dx = point.X - body.Position.X, dz = point.Z - body.Position.Z;
                Fix64 overlap = (body.Radius + radius) * OverlapRatio;
                if (dx * dx + dz * dz < overlap * overlap) return true;
            }
            return false;
        }

        public static bool Resolve(Vector3 aim, Vector3 origin, Fix64 range, Fix64 radius, bool airborne,
            IReadOnlyList<PlacementBody> bodies, bool[,] water, out Vector3 result) {
            result = default;
            if (range < Fix64.Zero || !InsideMap(aim) || Vector3.Distance(origin, aim) > range) return false;
            if (!Blocked(aim, radius, airborne, bodies, water)) { result = aim; return true; }
            for (int ring = 1; ring <= Rings; ring++) {
                Fix64 distance = (Fix64)ring * RingStep;
                for (int direction = 0; direction < Directions; direction++) {
                    Fix64 angle = Fix64.PiTimes2 * direction / Directions;
                    var candidate = aim + new Vector3(Fix64.Cos(angle) * distance, Fix64.Zero, Fix64.Sin(angle) * distance);
                    if (!InsideMap(candidate) || Vector3.Distance(origin, candidate) > range) continue;
                    if (!Blocked(candidate, radius, airborne, bodies, water)) { result = candidate; return true; }
                }
            }
            return false;
        }
    }
}
