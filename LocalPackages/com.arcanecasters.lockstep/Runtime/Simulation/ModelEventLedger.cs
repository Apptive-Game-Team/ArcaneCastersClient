using System;
using System.Collections.Generic;

namespace ArcaneCasters.Simulation {
    public interface IModelDecisionSource : IBotDecisionSource {
        void Bind(ModelRequestChannel channel);
        void Deliver(ModelEvent confirmed);
    }

    // 모델 소스가 Think 시점에 요청을 적는 유일한 경로다. 벽시계와 HTTP는 이 경로에 없다.
    public sealed class ModelRequestChannel {
        private readonly ModelEventLedger ledger;
        public Master Side { get; }
        public int Slot { get; }
        public long Generation { get; }
        public string SessionId => ledger.SessionId;
        public uint CurrentTick => ledger.CurrentTick;
        public bool HasOutstandingRequest => ledger.HasOutstandingRequest(Slot, Generation);
        public uint ResumeTick => ledger.ResumeTick(Slot);
        public ModelRequestChannel(ModelEventLedger ledger, Master side, int slot, long generation) {
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            Side = side; Slot = slot; Generation = generation;
        }
        // digest는 배정된 sequence와 관측 tick을 포함하므로 원장이 둘을 정한 뒤 계산한다.
        // payload는 그 관측의 정규 provider 입력이며 확정 기록에는 hash만 남는다.
        public long Submit(Func<long, uint, string> digest, ModelRequestPayload payload) => ledger.Submit(Slot, Generation, digest, payload);
    }

    // 확정 모델 이벤트의 재구성·검증·적용을 한곳에서 처리한다. 누락·추가·충돌·deadline 위반은 전부 프로토콜 오류다.
    public sealed class ModelEventLedger {
        private readonly struct Key : IEquatable<Key> {
            public readonly int Slot;
            public readonly long Generation, Sequence;
            public Key(int slot, long generation, long sequence) { Slot = slot; Generation = generation; Sequence = sequence; }
            public Key(ModelEvent item) : this(item.slot, item.generation, item.sequence) { }
            public Key(ModelRequest item) : this(item.slot, item.generation, item.sequence) { }
            public bool Equals(Key other) => Slot == other.Slot && Generation == other.Generation && Sequence == other.Sequence;
            public override bool Equals(object other) => other is Key key && Equals(key);
            public override int GetHashCode() => unchecked((Slot * 397 ^ Generation.GetHashCode()) * 397 ^ Sequence.GetHashCode());
            public override string ToString() => Slot + ":" + Generation + ":" + Sequence;
        }
        // 수락된 요청의 신원이다. terminal 이벤트는 이 observationTick과 일치해야 한다.
        private sealed class Accepted {
            public uint ObservationTick, TerminalTick;
            public string Digest, PayloadHash;
            // 봇이 교체되면 결과를 전달하지도 deadline을 보지도 않는다. 진짜 늦은 결과를 위조와 구분하려고 항목은 남긴다.
            public bool Live;
        }
        private sealed class SlotState {
            public long MaxGeneration = -1, LiveGeneration = -1;
            public IModelDecisionSource Source;
            public long NextSequence;
            public uint ResumeTick;
        }
        private readonly Func<int> frameNumber;
        private readonly SlotState[] slots = { new SlotState(), new SlotState() };
        private readonly SortedDictionary<uint, List<ModelRequest>> expected = new SortedDictionary<uint, List<ModelRequest>>();
        private readonly Dictionary<Key, Accepted> accepted = new Dictionary<Key, Accepted>();
        private readonly Dictionary<Key, string> resolved = new Dictionary<Key, string>();
        public string SessionId { get; }
        public bool Enabled { get; private set; }
        public uint CurrentTick => checked((uint)Math.Max(0, frameNumber()));
        public ModelEventLedger(string sessionId, Func<int> frameNumber) {
            if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException(nameof(sessionId));
            SessionId = sessionId; this.frameNumber = frameNumber ?? throw new ArgumentNullException(nameof(frameNumber));
        }
        public void Enable() {
            if (frameNumber() != 0) throw new InvalidOperationException("Enable the model protocol before the first confirmed frame.");
            Enabled = true;
        }
        private static int Index(Master side) => side == Master.LeftPlayer ? 0 : side == Master.RightPlayer ? 1 : throw new ArgumentOutOfRangeException(nameof(side));

        // generation은 슬롯마다 엄격히 증가한다. 같은 generation을 다시 붙이면 sequence 0부터 재사용되어 신원이 겹친다.
        public void Attach(Master side, long generation, IBotDecisionSource source) {
            int index = Index(side); var slot = slots[index];
            if (generation <= slot.MaxGeneration) throw new InvalidOperationException("Bot generations must strictly increase.");
            var model = source as IModelDecisionSource;
            // Bind가 실패하면 슬롯 상태를 하나도 바꾸지 않는다.
            model?.Bind(new ModelRequestChannel(this, side, index, generation));
            Abandon(index);
            slot.MaxGeneration = generation; slot.LiveGeneration = generation; slot.NextSequence = 0; slot.Source = model;
        }
        public void Detach(Master side) {
            int index = Index(side);
            Abandon(index);
            slots[index].LiveGeneration = -1; slots[index].Source = null;
        }
        private void Abandon(int index) {
            foreach (var pair in accepted) if (pair.Key.Slot == index) pair.Value.Live = false;
            slots[index].ResumeTick = 0;
        }

        public bool HasOutstandingRequest(int slot, long generation) {
            foreach (var pair in accepted) if (pair.Key.Slot == slot && pair.Key.Generation == generation && pair.Value.Live) return true;
            foreach (var pair in expected)
                foreach (var request in pair.Value)
                    if (request.slot == slot && request.generation == generation) return true;
            return false;
        }
        public uint ResumeTick(int slot) => slots[slot].ResumeTick;

        public long Submit(int slot, long generation, Func<long, uint, string> digest, ModelRequestPayload payload) {
            if (digest == null) throw new ArgumentNullException(nameof(digest));
            if (payload == null) throw new BotDecisionProtocolException("A model request carries its provider input payload.");
            if (!Enabled) throw new BotDecisionProtocolException("Model requests require the lockstep-v2 protocol.");
            var state = slots[slot];
            if (state.LiveGeneration != generation || state.Source == null) throw new BotDecisionProtocolException("Model request from a replaced bot generation.");
            uint tick = CurrentTick;
            if (tick == 0 || tick >= ModelProtocol.MaxTick) throw new BotDecisionProtocolException("Model request outside the confirmed tick range.");
            if (HasOutstandingRequest(slot, generation)) throw new BotDecisionProtocolException("A model request is already outstanding for this bot.");
            if (!expected.TryGetValue(tick, out var list)) expected[tick] = list = new List<ModelRequest>();
            if (list.Count >= ModelProtocol.MaxRequestsPerBatch) throw new BotDecisionProtocolException("Too many model requests in one confirmed tick.");
            long sequence = state.NextSequence;
            string value = digest(sequence, tick);
            if (!ModelProtocol.ValidDigest(value)) throw new BotDecisionProtocolException("Model request digest is not canonical.");
            ModelRequest request;
            try {
                request = ModelRequest.Copy(new ModelRequest {
                    slot = slot, generation = generation, sequence = sequence, observationTick = tick,
                    digest = value, payloadHash = ModelRequestPayload.Hash(payload), payload = payload
                });
            } catch (System.IO.IOException error) {
                throw new BotDecisionProtocolException("Model request payload is not canonical: " + error.Message);
            }
            state.NextSequence = sequence + 1;
            list.Add(request);
            return sequence;
        }

        // 다음 InputBatch에 반드시 싣는 manifest다. 비어 있으면 null을 돌려 v1 직렬화를 그대로 둔다.
        public ModelRequest[] PendingManifest(uint tick) {
            if (!expected.TryGetValue(tick, out var list) || list.Count == 0) return null;
            var manifest = new ModelRequest[list.Count];
            for (int i = 0; i < list.Count; i++) manifest[i] = ModelRequest.Copy(list[i]);
            return manifest;
        }

        // 확정 프레임의 모델 이벤트를 사람 입력 실행 전에 검증하고 적용한다.
        public void Receive(ConfirmedFrame frame) {
            if (!Enabled && frame.modelEvents != null) throw new BotDecisionProtocolException("Model events require the lockstep-v2 protocol.");
            var events = frame.modelEvents ?? Array.Empty<ModelEvent>();
            foreach (var pair in expected)
                if (pair.Key < frame.tick)
                    throw new BotDecisionProtocolException("Confirmed frame dropped model request " + pair.Value[0] + ".");
            expected.TryGetValue(frame.tick, out var pending);
            var remaining = new List<ModelRequest>(pending ?? (IEnumerable<ModelRequest>)Array.Empty<ModelRequest>());
            expected.Remove(frame.tick);
            foreach (var item in events) {
                if (item.kind != ModelProtocol.Accepted && item.kind != ModelProtocol.Rejected) continue;
                int index = remaining.FindIndex(request => new Key(request).Equals(new Key(item)));
                if (index < 0) throw new BotDecisionProtocolException("Confirmed frame added model request " + item + ".");
                if (remaining[index].digest != item.digest || remaining[index].observationTick != item.observationTick)
                    throw new BotDecisionProtocolException("Confirmed model request " + item + " conflicts with the local observation.");
                // 진짜 관측 digest에 다른 provider 입력을 붙인 기록은 여기서 걸린다.
                if (remaining[index].payloadHash != item.payloadHash)
                    throw new BotDecisionProtocolException("Confirmed model request " + item + " used a different provider input payload.");
                remaining.RemoveAt(index);
            }
            if (remaining.Count > 0) throw new BotDecisionProtocolException("Confirmed frame dropped model request " + remaining[0] + ".");
            foreach (var item in events) Apply(item);
            foreach (var pair in accepted)
                if (pair.Value.Live && pair.Value.TerminalTick <= frame.tick)
                    throw new BotDecisionProtocolException("Model request " + pair.Key + " passed its terminal tick " + pair.Value.TerminalTick + ".");
        }

        // 같은 요청에 대한 재전송은 본문이 완전히 같을 때만 멱등하다.
        private static string Signature(ModelEvent item) =>
            item.kind + "|" + item.observationTick + "|" + item.resumeTick + "|" + (item.playKey ?? "") + "|" + (item.aimKey ?? "") + "|" + (item.reason ?? "");

        private void Apply(ModelEvent item) {
            var state = slots[item.slot];
            if (item.generation > state.MaxGeneration) throw new BotDecisionProtocolException("Model event " + item + " names an unknown bot generation.");
            var key = new Key(item);
            bool live = item.generation == state.LiveGeneration && state.Source != null;
            if (item.kind == ModelProtocol.Accepted) {
                if (accepted.ContainsKey(key) || resolved.ContainsKey(key))
                    throw new BotDecisionProtocolException("Model request " + item + " was accepted twice.");
                accepted[key] = new Accepted {
                    ObservationTick = item.observationTick, TerminalTick = item.terminalTick,
                    Digest = item.digest, PayloadHash = item.payloadHash, Live = live
                };
                return;
            }
            if (item.kind == ModelProtocol.Rejected) {
                // 거부는 요청을 즉시 종결한다. 수락 기록이 없으므로 terminal 본문으로 남긴다.
                resolved[key] = Signature(item);
                if (live) {
                    if (item.resumeTick != 0) state.ResumeTick = item.resumeTick;
                    state.Source.Deliver(item);
                }
                return;
            }
            string body = Signature(item);
            if (accepted.TryGetValue(key, out var open)) {
                if (open.ObservationTick != item.observationTick)
                    throw new BotDecisionProtocolException("Model result " + item + " does not match the accepted observation tick " + open.ObservationTick + ".");
                accepted.Remove(key); resolved[key] = body;
                if (!open.Live || !live) return;
                if (item.kind == ModelProtocol.BackedOff) state.ResumeTick = item.resumeTick;
                state.Source.Deliver(item);
                return;
            }
            if (resolved.TryGetValue(key, out var seen)) {
                if (seen != body) throw new BotDecisionProtocolException("Model result " + item + " conflicts with the already recorded result.");
                return;
            }
            // 수락된 적이 없는 신원은 늦은 결과가 아니라 위조다. 교체된 봇의 수락 기록은 위에서 걸린다.
            throw new BotDecisionProtocolException("Model result " + item + " answers no accepted request.");
        }
    }
}
