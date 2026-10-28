using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class Rune : SimComponent, ICollidable {
        public Rune(SimObject owner) : base(owner) { }
        public void OnCollision(SimObject other) { }
        public void OnCollisionWithEnemy(SimObject other) {
            var radius = Context.GameParameters["rune.attack_range"];
            var damage = (int)(Context.GameParameters["rune.damage"].RawValue / (1L << 32)); var hit = new Hit(damage, Owner.Id, Owner.Elements.Total);
            foreach (var target in SimulationQueries.OverlapSphereAll(Context, Owner.Position, radius)) target.GetComponent<MobComponent>()?.OnDamaged(hit);
            Owner.Destroy();
        }
    }
    public static class RunePrefabs {
        public static void Register(SimulationContext context) {
            foreach (var element in new[] { Element.Fire, Element.Water, Element.Nature, Element.Rock, Element.Lightning, Element.Wind }) {
                var runeElement = element;
                context.RegisterPrefab(element + "Rune", owner => {
                    owner.Elements.AddNative(runeElement); owner.AddCollider(new CircleCollider(owner, context.GameParameters["rune.radius"], true)); owner.AddComponent(new Rune(owner));
                });
            }
        }
    }
}
