using System;
using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum GizmoCategory { Collider, AttackRange, AreaOfEffect, DetectionRange, SpawnArea }
    public enum GizmoType { Circle, Box }
    public readonly struct SimGizmo {
        public readonly Vector3 RelativePosition;
        public readonly Fix64 Radius;
        public readonly Vector3? BoxSize;
        public readonly GizmoType Type;
        public readonly GizmoCategory Category;
        private SimGizmo(Vector3 position, Fix64 radius, Vector3? size, GizmoType type, GizmoCategory category) { RelativePosition = position; Radius = radius; BoxSize = size; Type = type; Category = category; }
        public static SimGizmo Circle(Vector3 position, Fix64 radius, GizmoCategory category) => new SimGizmo(position, radius, null, GizmoType.Circle, category);
        public static SimGizmo Box(Vector3 position, Vector3 size, GizmoCategory category) => new SimGizmo(position, 0, size, GizmoType.Box, category);
    }
    public sealed class ObjectSnapshot {
        public int Id { get; }
        public string Prefab { get; }
        public Vector3 Position { get; }
        public Master Master { get; }
        public ObjectStatus Status { get; }
        public IReadOnlyList<VisualEffect> Effects { get; }
        public IReadOnlyList<SimGizmo> Gizmos { get; }
        public IReadOnlyList<Gauge> Gauges { get; }
        public ObjectSnapshot(SimObject owner, IEnumerable<Gauge> previousGauges = null) {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            Id = owner.Id; Prefab = owner.Prefab; Position = owner.Position; Master = owner.Master; Status = owner.Status;
            Effects = Array.AsReadOnly(owner.Effects); Gizmos = Array.AsReadOnly(owner.Gizmos);
            var gauges = previousGauges == null ? new List<Gauge>() : new List<Gauge>(previousGauges);
            GaugeExtractor.Merge(owner, gauges); Gauges = Array.AsReadOnly(gauges.ToArray());
        }
    }
}
