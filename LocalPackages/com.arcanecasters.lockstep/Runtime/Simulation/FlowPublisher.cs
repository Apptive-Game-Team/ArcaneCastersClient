using System;
using System.Collections.Generic;

namespace ArcaneCasters.Simulation {
    public sealed class FlowSubscription {
        private readonly Action cancel;
        internal FlowSubscription(Action cancel) { this.cancel = cancel; }
        public void Cancel() { cancel?.Invoke(); }
    }
    public sealed class FlowPublisher<T> {
        private readonly List<KeyValuePair<long, Action<T>>> entries = new List<KeyValuePair<long, Action<T>>>();
        private long nextId;
        public bool IsClosed { get; private set; }
        public int SubscriberCount => entries.Count;
        public FlowSubscription Subscribe(Action<T> callback) {
            if (IsClosed) return new FlowSubscription(null);
            long id = checked(nextId++);
            entries.Add(new KeyValuePair<long, Action<T>>(id, callback));
            return new FlowSubscription(() => entries.RemoveAll(entry => entry.Key == id));
        }
        public void Publish(T value) {
            if (IsClosed) return;
            foreach (var entry in entries.ToArray())
                if (entries.Exists(current => current.Key == entry.Key)) entry.Value(value);
        }
        public void Close() { IsClosed = true; entries.Clear(); }
    }
}
