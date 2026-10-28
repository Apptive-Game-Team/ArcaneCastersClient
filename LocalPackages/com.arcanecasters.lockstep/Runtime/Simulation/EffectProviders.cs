using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IAreaExplode { Fix64 Radius { get; } }
    public class EffectProvider : SimComponent, ICollidable {
        public VisualEffect Effect { get; }
        public EffectProvider(SimObject owner, VisualEffect effect) : base(owner) { Effect = effect; }
        public virtual void OnCollision(SimObject other) { }
        public virtual void OnCollisionWithEnemy(SimObject other) { other.GetComponent<IEffectReceiver>()?.Receive(new EffectApplication(Effect, Owner)); }
    }
    public sealed class KnockbackEffectProvider : EffectProvider {
        public KnockbackEffectProvider(SimObject owner, VisualEffect effect) : base(owner, effect) { }
        public override void OnCollisionWithEnemy(SimObject other) {
            var receiver = other.GetComponent<IEffectReceiver>(); if (receiver == null) return;
            Vector3 direction = Vector3.Zero; Fix64 proximity = Fix64.Zero;
            var shot = Owner.GetComponent<IDirectionalShot>(); var explosion = Owner.GetComponent<IAreaExplode>();
            if (shot != null) {
                direction = shot.Direction ?? throw new InvalidOperationException("Shot direction is not set");
                Fix64 radius = Owner.Colliders.FirstOrDefault(collider => collider.CircleRadius.HasValue)?.CircleRadius ?? Fix64.One;
                proximity = Vector3.Distance(Owner.Position, other.Position) / radius;
            } else if (explosion != null) {
                var offset = other.Position - Owner.Position; direction = offset.LengthSquared() == Fix64.Zero ? Vector3.Zero : Vector3.Normalize(offset);
                proximity = Vector3.Distance(Owner.Position, other.Position) / explosion.Radius;
            }
            receiver.ReceiveDirectional(Effect, direction, proximity);
        }
    }
}
