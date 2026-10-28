using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcaneCasters.Simulation {
    public sealed class PveChannelState : IEquatable<PveChannelState> {
        public string Type => "pveState";
        public readonly string Channel, Value;
        public readonly int Seq;
        public PveChannelState(string channel, string value, int seq) { Channel = channel; Value = value; Seq = seq; }
        public bool Equals(PveChannelState other) => other != null && Channel == other.Channel && Value == other.Value && Seq == other.Seq;
        public override bool Equals(object other) => Equals(other as PveChannelState);
        public override int GetHashCode() => unchecked(((Channel?.GetHashCode() ?? 0) * 397 ^ (Value?.GetHashCode() ?? 0)) * 397 ^ Seq);
    }
    public sealed class PveStateStore {
        public const string Bgm = "bgm";
        private readonly Dictionary<string, PveChannelState> states = new Dictionary<string, PveChannelState>(StringComparer.Ordinal);
        private int lastSeq;
        public PveChannelState Set(string channel, string value) {
            if (states.TryGetValue(channel, out var current) && current.Value == value) return null;
            var next = new PveChannelState(channel, value, unchecked(++lastSeq)); states[channel] = next; return next;
        }
        public PveChannelState[] Snapshot() => states.Values.OrderBy(state => state.Seq).ToArray();
    }
}
