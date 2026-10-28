using System;
using System.Collections.Generic;

namespace ArcaneCasters.Simulation {
    public interface IGameSystem { void Update(SimulationContext context); }
    public sealed class GameObjectStateInitialSystem : IGameSystem {
        public void Update(SimulationContext context) {
            foreach (var owner in context.Objects) if (owner.Status != ObjectStatus.Idle) owner.SetStatus(ObjectStatus.Idle);
        }
    }
    public sealed class ComponentUpdateSystem : IGameSystem {
        private readonly HashSet<int> reported = new HashSet<int>();
        public event Action<SimObject, Exception> Failed;
        public void Update(SimulationContext context) {
            foreach (var owner in context.Objects) {
                if (owner.IsDestroyed) continue;
                try { owner.Update(); }
                catch (Exception exception) { if (reported.Add(owner.Id)) Failed?.Invoke(owner, exception); }
            }
        }
    }
    public sealed class GameObjectAddRemoveSystem : IGameSystem {
        public void Update(SimulationContext context) { context.FlushObjects(); }
    }
}
