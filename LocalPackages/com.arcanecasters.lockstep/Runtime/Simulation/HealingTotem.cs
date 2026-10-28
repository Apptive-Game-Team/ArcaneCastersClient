using System.Linq;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class Item : SimComponent {
        public SimObject Parent { get; private set; }
        public Item(SimObject owner) : base(owner) { }
        public override void Update() {
            if (Parent != null) { Owner.SetPosition(Parent.Position); return; }
            var collider = Owner.Colliders.FirstOrDefault(value => value.CircleRadius.HasValue);
            if (collider == null) return;
            foreach (var other in SimulationQueries.OverlapSphereAll(Context, Owner.Position, collider.CircleRadius.Value)) {
                if (other.Id == Owner.Id || other.Master != Master.None && other.Master != Owner.Master) continue;
                Parent = other; return;
            }
        }
    }
    public static class HealingTotemPrefab {
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("HealingTotem", owner => {
                var p = context.GameParameters;
                owner.AddComponentImmediately(new RigidBody(owner, (int)(p["healing_totem.mass"].RawValue / (1L << 32))));
                owner.AddCollider(new CircleCollider(owner, p["healing_totem.radius"], true));
                owner.AddComponentImmediately(new Totem(owner, (int)(p["healing_totem.hp"].RawValue / (1L << 32)), (int)(p["healing_totem.damage"].RawValue / (1L << 32)), p["healing_totem.attack_interval"], p["healing_totem.effect_radius"], 1));
                owner.AddComponent(new Item(owner)); owner.Elements.AddNative(Element.Nature); owner.Elements.AddNative(Element.Water);
                owner.AddComponentImmediately(new TimedSelfDestroyer(owner, (Fix64)(p["healing_totem.duration"].RawValue / (1L << 32))));
                owner.AddComponentImmediately(new CommonEffectReceiver(owner));
            });
        }
    }
}
