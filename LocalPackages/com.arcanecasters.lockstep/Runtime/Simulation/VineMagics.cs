using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public interface IVineFan { void Run(SimulationContext context, Master master, Vector3 origin, Vector3 target); }
    public sealed class VineFanMagic : IVineFan {
        public void Run(SimulationContext context, Master master, Vector3 origin, Vector3 target) {
            var center = VineMagics.Normalize(target - origin); if (center == Vector3.Zero) return;
            for (int index = 0; index < 3; index++) {
                var angle = Fix64.Pi * (index - 1) / 6; var cos = Fix64.Cos(angle); var sin = Fix64.Sin(angle);
                var direction = VineMagics.Normalize(new Vector3(center.X * cos - center.Z * sin, center.Y, center.X * sin + center.Z * cos));
                VineMagics.Toss(context, master, origin, origin + direction);
            }
        }
    }
    public static class VineMagics {
        internal static Vector3 Normalize(Vector3 value) => SimulationVector.Normalize(value);
        public static void Toss(SimulationContext context, Master master, Vector3? origin, Vector3 target) {
            if (!origin.HasValue) return; var direction = Normalize(target - origin.Value); if (direction != Vector3.Zero) VineLine.Spawn(context, master, origin.Value, direction);
        }
        public static void Register(MagicRegistry registry) {
            registry.Register("vine_toss", new MagicImplementation(Toss));
            var fan = new VineFanMagic(); registry.Register("vine_fan", new MagicImplementation((context, master, origin, target) => { if (origin.HasValue) fan.Run(context, master, origin.Value, target); }, isVineFan: true));
            registry.Register("will_o_wisp", new MagicImplementation((context, master, origin, target) => {
                if (!origin.HasValue) return; var owner = context.Spawn(master, "WillOWisp", origin.Value);
                var shot = owner.GetComponent<MindControlShot>() ?? throw new System.InvalidOperationException("will_o_wisp has no MindControlShot"); shot.SetTarget(target);
            }));
        }
        public static void RegisterWillOWisp(SimulationContext context) {
            context.RegisterPrefab("WillOWisp", owner => {
                owner.AddCollider(new CircleCollider(owner, context.GameParameters["will_o_wisp.radius"], true)); owner.Elements.AddNative(Element.None);
                owner.AddComponentImmediately(new MindControlShot(owner, context.GameParameters["will_o_wisp.speed"]));
            });
        }
    }
}
