using System;
using System.Collections.Generic;

namespace ArcaneCasters.Simulation {
    // 요청 ID는 화면 응답에만 쓰며 확정 입력이나 전투 상태에는 포함하지 않는다.
    public sealed class LockstepLocalInputs {
        private readonly List<CastCommand> queued = new List<CastCommand>();
        private readonly List<int?> queuedIds = new List<int?>();
        private readonly HashSet<int> pendingIds = new HashSet<int>();
        private readonly Dictionary<int, int> acknowledgements = new Dictionary<int, int>();
        private readonly bool modelProtocol;
        private InputBatch inFlight;
        public LockstepLocalInputs(bool modelProtocol = false) { this.modelProtocol = modelProtocol; }
        public bool HasCapacity(int count) => count >= 0 && count <= 16 - queued.Count;
        public static int SimulationRequestId(uint tick, int slot, int index) {
            if (tick >= 12000 || slot < 0 || slot > 1 || index < 0 || index >= 16) throw new ArgumentOutOfRangeException();
            return checked((int)tick * 32 + slot * 16 + index);
        }
        public bool Queue(long magicId, long xRaw, long zRaw, int? requestId = null) {
            return QueueCommand("cast", magicId, xRaw, zRaw, requestId);
        }
        public bool QueueSelection(long magicId, bool selected) => QueueCommand(selected ? "selectCard" : "unselectCard", magicId, 0, 0, null);
        public bool QueueEmote(string name) {
            if (!HasCapacity(1) || !SessionEmotes.Parse(name).HasValue) return false;
            queued.Add(new CastCommand { kind = "emote", emote = name }); queuedIds.Add(null);
            return true;
        }
        private bool QueueCommand(string kind, long magicId, long xRaw, long zRaw, int? requestId) {
            if (queued.Count >= 16 || magicId <= 0 || xRaw < 0 || xRaw > (18L << 32) || zRaw < 0 || zRaw > (10L << 32)
                || requestId.HasValue && pendingIds.Contains(requestId.Value)) return false;
            queued.Add(new CastCommand { kind = kind, magicId = magicId, xRaw = xRaw, zRaw = zRaw }); queuedIds.Add(requestId);
            if (requestId.HasValue) pendingIds.Add(requestId.Value);
            return true;
        }
        // modelRequests는 그 tick을 관측한 Think가 만든 manifest다. 배치가 이미 만들어진 tick에서는 바꾸지 않는다.
        public InputBatch Prepare(uint tick, int slot, ModelRequest[] modelRequests = null) {
            SimulationRequestId(tick, slot, 0);
            // v1 배치는 모델 필드를 전혀 싣지 않는다. 빈 manifest도 v1에서는 거부한다.
            if (modelRequests != null && (!modelProtocol || modelRequests.Length == 0 || modelRequests.Length > ModelProtocol.MaxRequestsPerBatch))
                throw new ArgumentOutOfRangeException(nameof(modelRequests));
            if (inFlight != null && tick < inFlight.tick) return null;
            if (inFlight == null || inFlight.tick < tick) {
                inFlight = new InputBatch { tick = tick, commands = queued.ToArray(), modelRequests = modelRequests };
                for (int i = 0; i < queuedIds.Count; i++) {
                    if (queuedIds[i].HasValue) acknowledgements.Add(SimulationRequestId(tick, slot, i), queuedIds[i].Value);
                }
                queued.Clear(); queuedIds.Clear();
            }
            return inFlight;
        }
        public bool TryAcknowledge(InputResponse response, out InputResponse local) {
            local = default;
            if (!acknowledgements.TryGetValue(response.Id, out var id)) return false;
            acknowledgements.Remove(response.Id); pendingIds.Remove(id);
            local = new InputResponse(response.Valid, response.ResultCode, response.Message, response.UpdatedMana, id, response.MagicId);
            return true;
        }
        public void Clear() { queued.Clear(); queuedIds.Clear(); pendingIds.Clear(); acknowledgements.Clear(); inFlight = null; }
    }
}
