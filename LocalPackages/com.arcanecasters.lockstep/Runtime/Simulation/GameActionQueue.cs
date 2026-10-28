using System;
using System.Collections.Generic;

namespace ArcaneCasters.Simulation {
    public sealed class GameActionQueue<T> {
        public const int Capacity = 256;
        private readonly object gate = new object();
        private readonly Queue<(string name, Action<T> action)> actions = new Queue<(string, Action<T>)>();
        public event Action<string, Exception> Failed;
        public int Count { get { lock (gate) return actions.Count; } }
        public bool Submit(string name, Action<T> action) {
            if (action == null) throw new ArgumentNullException(nameof(action));
            lock (gate) {
                if (actions.Count >= Capacity) return false;
                actions.Enqueue((name, action)); return true;
            }
        }
        public void Drain(T context) {
            int remaining = Count;
            while (remaining-- > 0) {
                (string name, Action<T> action) queued;
                lock (gate) {
                    if (actions.Count == 0) return;
                    queued = actions.Dequeue();
                }
                try { queued.action(context); }
                catch (Exception exception) { Failed?.Invoke(queued.name, exception); }
            }
        }
    }
    public sealed class GameActionSender<T> {
        private readonly GameActionQueue<T> queue;
        public GameActionSender(GameActionQueue<T> queue) { this.queue = queue; }
        public bool Submit(string name, Action<T> action) => queue.Submit(name, action);
    }
    public sealed class GameActionSystem : IGameSystem {
        public void Update(SimulationContext context) => context.DrainActions();
    }
}
