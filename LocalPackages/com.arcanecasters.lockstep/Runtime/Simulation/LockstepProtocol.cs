using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ArcaneCasters.Simulation {
    public static class LockstepVersion {
        public const string Supported = "lockstep-v1";
        // 모델 봇 경기만 v2를 쓴다. v1 경기의 규칙 봇·PVE·PVP 계약은 그대로 둔다.
        public const string SupportedModel = "lockstep-v2";
        public static bool IsSupported(string value) => value == Supported || value == SupportedModel;
        public static bool UsesModelProtocol(string value) => value == SupportedModel;
        public static bool ValidSessionId(string value) {
            if (string.IsNullOrEmpty(value) || value.Length > 128) return false;
            foreach (char c in value) {
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_')) return false;
            }
            return true;
        }
    }
    [Serializable]
    public sealed class CastCommand {
        public string kind = "cast";
        public string emote;
        public long magicId;
        public long xRaw;
        public long zRaw;
    }
    [Serializable]
    public sealed class InputBatch {
        public uint tick;
        public CastCommand[] commands;
        // 관측 다음 배치에 반드시 실리는 모델 요청 manifest다. v1 경기에서는 null로 남는다.
        public ModelRequest[] modelRequests;
    }
    [Serializable]
    public sealed class ConfirmedFrame {
        public string sessionId;
        public uint tick;
        public CastCommand[][] inputs;
        // 서버가 확정한 모델 이벤트다. 사람 입력과 섞지 않고 같은 프레임에 따로 기록한다.
        public ModelEvent[] modelEvents;
    }
    [Serializable]
    public sealed class SessionStart {
        public string sessionId;
        public long[] participants;
        public string simulationVersion;
        public string initialStateJson;
        public string initialHash;
        public int tickRate;

        public int Validate(string expectedSession, long memberId, string expectedVersion, bool spectator = false) {
            if (!LockstepVersion.IsSupported(expectedVersion) || sessionId != expectedSession || simulationVersion != expectedVersion || tickRate != 20
                || participants == null || participants.Length != 2 || participants[0] == 0
                || participants[1] == 0 || participants[0] == participants[1] || memberId <= 0
                || (participants[0] < 0 && participants[1] < 0)
                || initialStateJson == null || Encoding.UTF8.GetByteCount(initialStateJson) > 128 * 1024)
                throw new InvalidDataException("Invalid lockstep initial state");
            if (initialHash != Digest(Encoding.UTF8.GetBytes(initialStateJson)))
                throw new InvalidDataException("Initial state hash mismatch");
            int slot = Array.IndexOf(participants, memberId);
            if (slot < 0 && !spectator) throw new InvalidDataException("Not a lockstep participant");
            return slot;
        }
        internal static string Digest(byte[] bytes) {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
    [Serializable]
    public sealed class HistoryPage {
        public string sessionId;
        public uint fromTick;
        public uint nextTick;
        public uint confirmedCount;
        public ConfirmedFrame[] frames;
        public string ended;
    }

    public sealed class ConfirmedInputStream {
        private readonly string sessionId;
        private readonly bool modelProtocol;
        private readonly SortedDictionary<uint, ConfirmedFrame> pending = new SortedDictionary<uint, ConfirmedFrame>();
        private readonly List<string> processed = new List<string>();
        public uint NextTick => (uint)processed.Count;
        public ConfirmedInputStream(string sessionId, bool modelProtocol = false) {
            if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException(nameof(sessionId));
            this.sessionId = sessionId; this.modelProtocol = modelProtocol;
        }

        public void Enqueue(ConfirmedFrame frame) {
            if (frame == null || frame.sessionId != sessionId || frame.tick >= 12000
                || frame.tick > NextTick + 256 || frame.inputs == null || frame.inputs.Length != 2)
                throw new InvalidDataException("Invalid confirmed frame");
            var copy = new ConfirmedFrame { sessionId = sessionId, tick = frame.tick, inputs = new CastCommand[2][], modelEvents = CopyModelEvents(frame) };
            for (int slot = 0; slot < 2; slot++) {
                if (frame.inputs[slot] == null || frame.inputs[slot].Length > 16) throw new InvalidDataException("Invalid input count");
                copy.inputs[slot] = new CastCommand[frame.inputs[slot].Length];
                for (int i = 0; i < frame.inputs[slot].Length; i++) {
                    var c = frame.inputs[slot][i];
                    if (c == null || (c.kind == "emote"
                        ? c.magicId != 0 || c.xRaw != 0 || c.zRaw != 0 || !SessionEmotes.Parse(c.emote).HasValue
                        : c.emote != null || c.magicId <= 0 || c.xRaw < 0 || c.xRaw > (18L << 32) || c.zRaw < 0 || c.zRaw > (10L << 32)
                            || c.kind != "cast" && c.kind != "selectCard" && c.kind != "unselectCard"
                            || c.kind != "cast" && (c.xRaw != 0 || c.zRaw != 0)))
                        throw new InvalidDataException("Invalid command");
                    copy.inputs[slot][i] = new CastCommand { kind = c.kind, emote = c.emote, magicId = c.magicId, xRaw = c.xRaw, zRaw = c.zRaw };
                }
            }
            string hash = Fingerprint(copy);
            if (frame.tick < NextTick) {
                if (processed[(int)frame.tick] != hash) throw new InvalidDataException("Conflicting confirmed frame");
                return;
            }
            if (pending.TryGetValue(frame.tick, out var prior) && Fingerprint(prior) != hash)
                throw new InvalidDataException("Conflicting pending frame");
            pending[frame.tick] = copy;
        }

        public bool TryTake(out ConfirmedFrame frame) {
            if (!pending.TryGetValue(NextTick, out frame)) return false;
            pending.Remove(NextTick);
            processed.Add(Fingerprint(frame));
            return true;
        }

        // v1 경기는 모델 필드가 붙은 프레임 자체를 거부한다. 빈 배열도 필드가 있는 것이므로 거부하고, 없는 필드만 허용한다.
        private ModelEvent[] CopyModelEvents(ConfirmedFrame frame) {
            if (!modelProtocol) {
                if (frame.modelEvents != null) throw new InvalidDataException("Model events require the lockstep-v2 protocol");
                return null;
            }
            if (frame.modelEvents == null || frame.modelEvents.Length == 0) return Array.Empty<ModelEvent>();
            if (frame.modelEvents.Length > ModelProtocol.MaxEventsPerFrame) throw new InvalidDataException("Invalid model event count");
            var copy = new ModelEvent[frame.modelEvents.Length];
            var seen = new HashSet<string>();
            for (int i = 0; i < copy.Length; i++) {
                copy[i] = ModelEvent.Copy(frame.modelEvents[i]);
                if (!seen.Add(copy[i].kind + "/" + copy[i].slot + "/" + copy[i].generation + "/" + copy[i].sequence))
                    throw new InvalidDataException("Duplicate model event in one confirmed frame");
            }
            return copy;
        }

        private static string Fingerprint(ConfirmedFrame frame) {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) {
                writer.Write(frame.sessionId); writer.Write(frame.tick);
                foreach (var commands in frame.inputs) {
                    writer.Write(commands.Length);
                    foreach (var command in commands) { writer.Write(command.kind); writer.Write(command.emote ?? ""); writer.Write(command.magicId); writer.Write(command.xRaw); writer.Write(command.zRaw); }
                }
                var events = frame.modelEvents ?? Array.Empty<ModelEvent>();
                writer.Write(events.Length);
                foreach (var item in events) item.Write(writer);
            }
            return SessionStart.Digest(stream.ToArray());
        }
    }
}
