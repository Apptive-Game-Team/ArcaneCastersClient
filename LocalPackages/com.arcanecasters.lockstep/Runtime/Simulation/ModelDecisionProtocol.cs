using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    // 모델 봇의 확정 요청과 결과는 lockstep-v2 경기에만 존재한다. v1 경기는 이 필드를 담은 프레임을 거부한다.
    public static class ModelProtocol {
        public const string Accepted = "accepted", Rejected = "rejected", Completed = "completed", Failed = "failed", BackedOff = "backoff";
        public const string WaitPlayKey = "wait";
        public const int MaxEventsPerFrame = 8, MaxRequestsPerBatch = 2, MaxKeyLength = 64, MaxReasonLength = 200;
        // provider 입력 payload의 상한이다. 기존 입력/이력 용량 제한을 넘기지 않도록 확정 기록에는 hash만 남긴다.
        public const int MaxOptions = 17, MaxOptionTextLength = 1024, MaxStateEntries = 16, MaxStateJsonLength = 8192;
        public const int MaxAimGroups = 16, MaxAimsPerGroup = 8, MaxPayloadBytes = 48 * 1024;
        public const string CastPolicy = "cast", StancePolicy = "stance";
        // 원본 llm_answer의 MAX_REASON_LENGTH다. 확정 기록은 더 긴 사유를 담을 수 있고 화면용 사유만 여기서 자른다.
        public const int MaxDecisionReasonLength = 120;
        public const uint MaxTick = 12000;
        public static string TruncateReason(string value) =>
            value == null || value.Length <= MaxDecisionReasonLength ? value : value.Substring(0, MaxDecisionReasonLength);
        public static bool ValidKey(string value) {
            if (string.IsNullOrEmpty(value) || value.Length > MaxKeyLength) return false;
            foreach (char c in value)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '.' || c == '-')) return false;
            return true;
        }
        public static bool ValidDigest(string value) {
            if (value == null || value.Length != 64) return false;
            foreach (char c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return true;
        }
        public static bool ResolvesRequest(string kind) => kind == Completed || kind == Failed || kind == BackedOff;
    }

    // provider가 고를 선택지 하나다. client는 provider/model/URL/system prompt를 정하지 않고 선택지 본문만 싣는다.
    [Serializable]
    public sealed class ModelOption {
        public string key;
        public string text;
        public static ModelOption Copy(ModelOption option) {
            if (option == null || !ModelProtocol.ValidKey(option.key) || option.text == null
                || option.text.Length == 0 || option.text.Length > ModelProtocol.MaxOptionTextLength)
                throw new InvalidDataException("Invalid model option");
            return new ModelOption { key = option.key, text = option.text };
        }
    }
    // board 상태 한 항목이다. 값은 정규 JSON 조각으로 실어 넣은 순서를 지킨다.
    [Serializable]
    public sealed class ModelStateEntry {
        public string key;
        public string json;
        public static ModelStateEntry Copy(ModelStateEntry entry) {
            if (entry == null || string.IsNullOrEmpty(entry.key) || entry.key.Length > ModelProtocol.MaxKeyLength
                || entry.json == null || entry.json.Length == 0 || entry.json.Length > ModelProtocol.MaxStateJsonLength)
                throw new InvalidDataException("Invalid model state entry");
            return new ModelStateEntry { key = entry.key, json = entry.json };
        }
    }
    [Serializable]
    public sealed class ModelAimGroup {
        public string playKey;
        public ModelOption[] aims;
        public static ModelAimGroup Copy(ModelAimGroup group) {
            if (group == null || !ModelProtocol.ValidKey(group.playKey) || group.aims == null
                || group.aims.Length < 2 || group.aims.Length > ModelProtocol.MaxAimsPerGroup)
                throw new InvalidDataException("Invalid model aim group");
            var copy = new ModelOption[group.aims.Length];
            var seen = new HashSet<string>();
            for (int i = 0; i < copy.Length; i++) {
                copy[i] = ModelOption.Copy(group.aims[i]);
                if (!seen.Add(copy[i].key)) throw new InvalidDataException("Duplicate model aim option");
            }
            return new ModelAimGroup { playKey = group.playKey, aims = copy };
        }
    }
    // 서버가 provider IO에 그대로 쓰는 정규 입력이다. 확정 기록에는 이 payload의 hash만 남는다.
    [Serializable]
    public sealed class ModelRequestPayload {
        public string policy;
        public ModelOption[] criteria;
        public ModelStateEntry[] state;
        public ModelAimGroup[] aims;

        public static ModelRequestPayload Copy(ModelRequestPayload payload) {
            if (payload == null) throw new InvalidDataException("Missing model request payload");
            if (payload.policy != ModelProtocol.CastPolicy && payload.policy != ModelProtocol.StancePolicy)
                throw new InvalidDataException("Unknown model request policy");
            if (payload.criteria == null || payload.criteria.Length == 0 || payload.criteria.Length > ModelProtocol.MaxOptions)
                throw new InvalidDataException("Invalid model request criteria");
            if (payload.state == null || payload.state.Length == 0 || payload.state.Length > ModelProtocol.MaxStateEntries)
                throw new InvalidDataException("Invalid model request state");
            var aims = payload.aims ?? Array.Empty<ModelAimGroup>();
            if (aims.Length > ModelProtocol.MaxAimGroups) throw new InvalidDataException("Invalid model request aims");
            var copy = new ModelRequestPayload {
                policy = payload.policy,
                criteria = new ModelOption[payload.criteria.Length],
                state = new ModelStateEntry[payload.state.Length],
                aims = new ModelAimGroup[aims.Length]
            };
            var keys = new HashSet<string>();
            for (int i = 0; i < copy.criteria.Length; i++) {
                copy.criteria[i] = ModelOption.Copy(payload.criteria[i]);
                if (!keys.Add(copy.criteria[i].key)) throw new InvalidDataException("Duplicate model request option");
            }
            var stateKeys = new HashSet<string>();
            for (int i = 0; i < copy.state.Length; i++) {
                copy.state[i] = ModelStateEntry.Copy(payload.state[i]);
                if (!stateKeys.Add(copy.state[i].key)) throw new InvalidDataException("Duplicate model state entry");
            }
            var groups = new HashSet<string>();
            for (int i = 0; i < copy.aims.Length; i++) {
                copy.aims[i] = ModelAimGroup.Copy(aims[i]);
                if (!groups.Add(copy.aims[i].playKey)) throw new InvalidDataException("Duplicate model aim group");
                if (!keys.Contains(copy.aims[i].playKey)) throw new InvalidDataException("Model aim group names no option");
            }
            if (Canonical(copy).Length > ModelProtocol.MaxPayloadBytes) throw new InvalidDataException("Model request payload too large");
            return copy;
        }

        // 서버와 verifier가 같은 바이트를 얻는 정규 직렬화다.
        // 틀은 언어에 의존하지 않게 고정한다: 각 수는 4바이트 big endian, 각 문자열은 UTF-8 길이 4바이트 big endian 뒤에 그 바이트다.
        public static byte[] Canonical(ModelRequestPayload payload) {
            using var stream = new MemoryStream();
            Text(stream, "lockstep-model-payload-v2");
            Text(stream, payload.policy);
            Count(stream, payload.criteria.Length);
            foreach (var option in payload.criteria) { Text(stream, option.key); Text(stream, option.text); }
            Count(stream, payload.state.Length);
            foreach (var entry in payload.state) { Text(stream, entry.key); Text(stream, entry.json); }
            var aims = payload.aims ?? Array.Empty<ModelAimGroup>();
            Count(stream, aims.Length);
            foreach (var group in aims) {
                Text(stream, group.playKey); Count(stream, group.aims.Length);
                foreach (var option in group.aims) { Text(stream, option.key); Text(stream, option.text); }
            }
            return stream.ToArray();
        }
        private static void Count(Stream stream, int value) {
            stream.WriteByte((byte)(value >> 24)); stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8)); stream.WriteByte((byte)value);
        }
        private static void Text(Stream stream, string value) {
            var bytes = Encoding.UTF8.GetBytes(value);
            Count(stream, bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }
        public static string Hash(ModelRequestPayload payload) => SessionStart.Digest(Canonical(payload));

        // 정확한 Think 시점의 메뉴와 criteria에서 만든다. client는 어떤 provider 설정도 더하지 않는다.
        public static ModelRequestPayload Of(string policy, ModelCandidateMenu menu, ModelJsonMap criteria) {
            if (menu == null) throw new ArgumentNullException(nameof(menu));
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));
            var options = new List<ModelOption>();
            foreach (var entry in criteria.Entries)
                options.Add(new ModelOption { key = entry.Key, text = entry.Value.AsText ?? entry.Value.Write() });
            var state = new List<ModelStateEntry>();
            foreach (var entry in menu.State.Entries)
                state.Add(new ModelStateEntry { key = entry.Key, json = entry.Value.Write() });
            var groups = new List<ModelAimGroup>();
            if (policy == ModelProtocol.CastPolicy) {
                foreach (var play in menu.Plays) {
                    if (play.Aims.Count < 2) continue;
                    var aims = new ModelOption[play.Aims.Count];
                    for (int i = 0; i < aims.Length; i++) aims[i] = new ModelOption { key = play.Aims[i].Key, text = play.Aims[i].Description };
                    groups.Add(new ModelAimGroup { playKey = play.Key, aims = aims });
                }
            }
            return Copy(new ModelRequestPayload {
                policy = policy, criteria = options.ToArray(), state = state.ToArray(), aims = groups.ToArray()
            });
        }
    }

    // 사람 입력과 분리된 모델 요청 manifest다. 관측한 다음 InputBatch에 반드시 실린다.
    [Serializable]
    public sealed class ModelRequest {
        public int slot;
        public long generation;
        public long sequence;
        public uint observationTick;
        public string digest;
        public string payloadHash;
        public ModelRequestPayload payload;

        public static ModelRequest Copy(ModelRequest request) {
            if (request == null) throw new InvalidDataException("Missing model request");
            Validate(request.slot, request.generation, request.sequence, request.observationTick);
            if (!ModelProtocol.ValidDigest(request.digest)) throw new InvalidDataException("Invalid model request digest");
            if (!ModelProtocol.ValidDigest(request.payloadHash)) throw new InvalidDataException("Invalid model request payload hash");
            var payload = ModelRequestPayload.Copy(request.payload);
            // 진짜 관측 digest에 다른 provider 입력을 붙일 수 없다.
            if (ModelRequestPayload.Hash(payload) != request.payloadHash)
                throw new InvalidDataException("Model request payload does not match its hash");
            return new ModelRequest {
                slot = request.slot, generation = request.generation, sequence = request.sequence,
                observationTick = request.observationTick, digest = request.digest,
                payloadHash = request.payloadHash, payload = payload
            };
        }
        internal static void Validate(int slot, long generation, long sequence, uint observationTick) {
            if (slot < 0 || slot > 1 || generation < 0 || sequence < 0 || observationTick == 0 || observationTick >= ModelProtocol.MaxTick)
                throw new InvalidDataException("Invalid model request identity");
        }
        public override string ToString() => slot + ":" + generation + ":" + sequence + "@" + observationTick;
    }

    // 서버가 확정 프레임에 기록한 모델 이벤트다. 수락·거부·완료·실패·backoff를 모두 포함한다.
    [Serializable]
    public sealed class ModelEvent {
        public string kind;
        public int slot;
        public long generation;
        public long sequence;
        public uint observationTick;
        public string digest;
        // 서버가 실제 provider IO에 사용한 payload의 hash다. replica와 verifier가 재구성해 비교한다.
        public string payloadHash;
        public uint terminalTick;
        public uint resumeTick;
        public string playKey;
        public string aimKey;
        public string reason;

        public static ModelEvent Copy(ModelEvent source) {
            if (source == null) throw new InvalidDataException("Missing model event");
            ModelRequest.Validate(source.slot, source.generation, source.sequence, source.observationTick);
            bool manifest = source.kind == ModelProtocol.Accepted || source.kind == ModelProtocol.Rejected;
            if (!manifest && !ModelProtocol.ResolvesRequest(source.kind)) throw new InvalidDataException("Unknown model event kind");
            // manifest 이벤트만 정규 digest와 payload hash를 싣는다. 나머지는 그 자리가 반드시 비어 있어야 한다.
            if (manifest ? !ModelProtocol.ValidDigest(source.digest) : source.digest != null)
                throw new InvalidDataException("Invalid model event digest");
            if (manifest ? !ModelProtocol.ValidDigest(source.payloadHash) : source.payloadHash != null)
                throw new InvalidDataException("Invalid model event payload hash");
            if (source.terminalTick != 0 && (source.kind != ModelProtocol.Accepted || source.terminalTick <= source.observationTick || source.terminalTick >= ModelProtocol.MaxTick))
                throw new InvalidDataException("Invalid model terminal deadline");
            if (source.kind == ModelProtocol.Accepted && source.terminalTick == 0) throw new InvalidDataException("Accepted model request needs a terminal deadline");
            if (source.resumeTick != 0 && ((source.kind != ModelProtocol.BackedOff && source.kind != ModelProtocol.Rejected)
                || source.resumeTick <= source.observationTick || source.resumeTick >= ModelProtocol.MaxTick))
                throw new InvalidDataException("Invalid model backoff resume tick");
            if (source.kind == ModelProtocol.BackedOff && source.resumeTick == 0) throw new InvalidDataException("Backed off model request needs a resume tick");
            if ((source.kind == ModelProtocol.Completed) != (source.playKey != null)) throw new InvalidDataException("Invalid model play key");
            if (source.playKey != null && !ModelProtocol.ValidKey(source.playKey)) throw new InvalidDataException("Invalid model play key");
            if (source.aimKey != null && (source.kind != ModelProtocol.Completed || !ModelProtocol.ValidKey(source.aimKey))) throw new InvalidDataException("Invalid model aim key");
            // provider가 고른 사유는 완료 기록에도 남긴다. 수락 통지만 사유를 담지 않는다.
            if (source.reason != null && (source.reason.Length == 0 || source.reason.Length > ModelProtocol.MaxReasonLength
                || source.kind == ModelProtocol.Accepted)) throw new InvalidDataException("Invalid model event reason");
            return new ModelEvent {
                kind = source.kind, slot = source.slot, generation = source.generation, sequence = source.sequence,
                observationTick = source.observationTick, digest = source.digest, payloadHash = source.payloadHash,
                terminalTick = source.terminalTick, resumeTick = source.resumeTick,
                playKey = source.playKey, aimKey = source.aimKey, reason = source.reason
            };
        }
        internal void Write(BinaryWriter writer) {
            writer.Write(kind); writer.Write(slot); writer.Write(generation); writer.Write(sequence);
            writer.Write(observationTick); writer.Write(digest ?? ""); writer.Write(payloadHash ?? "");
            writer.Write(terminalTick); writer.Write(resumeTick);
            writer.Write(playKey ?? ""); writer.Write(aimKey ?? ""); writer.Write(reason ?? "");
        }
        public override string ToString() => kind + " " + slot + ":" + generation + ":" + sequence + "@" + observationTick;
    }

    // (session, side, generation, sequence, observationTick, 관측·후보 digest)를 정확한 Think 시점에 고정한다.
    public static class ModelDigest {
        public static string Compute(string sessionId, int slot, long generation, long sequence, uint observationTick,
            BotEye eye, ModelCandidateMenu menu, ModelRequestPayload payload = null) {
            if (sessionId == null) throw new ArgumentNullException(nameof(sessionId));
            if (eye == null) throw new ArgumentNullException(nameof(eye));
            if (menu == null) throw new ArgumentNullException(nameof(menu));
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) {
                writer.Write("lockstep-model-v2");
                writer.Write(sessionId); writer.Write(slot); writer.Write(generation); writer.Write(sequence); writer.Write(observationTick);
                writer.Write(eye.Mana); writer.Write(eye.EnemyPlayerHp);
                // 관측 시각은 봇의 이동 추정을 바꾸므로 digest에 포함한다.
                writer.Write(eye.ObservedAtSeconds.HasValue);
                writer.Write(eye.ObservedAtSeconds.HasValue ? eye.ObservedAtSeconds.Value.RawValue : 0L);
                writer.Write(eye.Cards.Count);
                foreach (long card in eye.Cards) writer.Write(card);
                writer.Write(eye.Objects.Count);
                foreach (var item in eye.Objects) {
                    writer.Write(item.Id); writer.Write((int)item.Master); writer.Write(item.Prefab ?? "");
                    writer.Write(item.Hp); writer.Write(item.Mob); writer.Write(item.Targetable);
                    Write(writer, item.Position);
                }
                // 모델에게 제시한 본문 전체와 board 상태를 묶는다. stance 정책의 판단을 바꾸는 attack/save 분류도 포함한다.
                writer.Write(menu.WaitDescription ?? "");
                writer.Write(menu.State.Write());
                writer.Write(menu.DearestManaCost);
                writer.Write(menu.AttackPlay == null ? "" : menu.AttackPlay.Key);
                writer.Write(menu.SavePlay == null ? "" : menu.SavePlay.Key);
                writer.Write(menu.Plays.Count);
                foreach (var play in menu.Plays) {
                    writer.Write(play.Key); writer.Write(play.MagicId); writer.Write(play.MagicName);
                    writer.Write(play.Summon); writer.Write(play.Standard); writer.Write(play.ManaCost);
                    writer.Write(play.BaseDamage.RawValue); writer.Write(play.UnitDamage.RawValue);
                    writer.Write(play.Description);
                    Write(writer, play.Target);
                    writer.Write(play.Aims.Count);
                    foreach (var aim in play.Aims) { writer.Write(aim.Key); writer.Write(aim.Description); Write(writer, aim.Target); }
                }
                // provider 입력도 같은 관측에 묶는다.
                writer.Write(payload == null ? "" : ModelRequestPayload.Hash(payload));
            }
            return SessionStart.Digest(stream.ToArray());
        }
        private static void Write(BinaryWriter writer, Vector3 value) {
            writer.Write(value.X.RawValue); writer.Write(value.Y.RawValue); writer.Write(value.Z.RawValue);
        }
    }

    // 원본 llm_candidates.rs의 Aim이다. 좌표가 아니라 그 자리에 무엇이 있는지로 설명한다.
    public sealed class ModelCandidateAim {
        public string Key { get; }
        public string Description { get; }
        public Vector3 Target { get; }
        public ModelCandidateAim(string key, string description, Vector3 target) {
            if (!ModelProtocol.ValidKey(key)) throw new ArgumentException("Invalid aim key", nameof(key));
            Key = key; Description = description ?? ""; Target = target;
        }
    }
    // 원본 llm_candidates.rs의 Play다. 마법 하나와 그것이 떨어지는 지점이며, 모델은 키만 고른다.
    public sealed class ModelCandidatePlay {
        public string Key { get; }
        public long MagicId { get; }
        public string MagicName { get; }
        public int ManaCost { get; }
        public Vector3 Target { get; }
        public string Description { get; }
        public bool Summon { get; }
        public Fix64 BaseDamage { get; }
        public Fix64 UnitDamage { get; }
        public IReadOnlyList<ModelCandidateAim> Aims { get; }
        // 규칙 brain의 판단으로 제시한 선택지는 설명문이 STANDARD TACTIC으로 시작한다.
        public bool Standard => Description.StartsWith(ModelCandidateBuilder.StandardTactic, StringComparison.Ordinal);
        // 후보는 digest로 고정되므로 바깥 목록의 변경이 그 뒤에 반영되면 안 된다. 받은 목록을 복사해 얼린다.
        public ModelCandidatePlay(string key, long magicId, string magicName, int manaCost, Vector3 target, string description,
            bool summon, Fix64 baseDamage, Fix64 unitDamage, IReadOnlyList<ModelCandidateAim> aims = null) {
            if (!ModelProtocol.ValidKey(key) || key == ModelProtocol.WaitPlayKey) throw new ArgumentException("Invalid play key", nameof(key));
            if (magicId <= 0) throw new ArgumentOutOfRangeException(nameof(magicId));
            Key = key; MagicId = magicId; MagicName = magicName ?? ""; ManaCost = manaCost; Target = target;
            Description = description ?? ""; Summon = summon; BaseDamage = baseDamage; UnitDamage = unitDamage;
            Aims = Array.AsReadOnly(Freeze(aims));
        }
        private static ModelCandidateAim[] Freeze(IReadOnlyList<ModelCandidateAim> aims) {
            if (aims == null || aims.Count == 0) return Array.Empty<ModelCandidateAim>();
            var copy = new ModelCandidateAim[aims.Count];
            var seen = new HashSet<string>();
            for (int i = 0; i < copy.Length; i++) {
                copy[i] = aims[i] ?? throw new ArgumentException("A candidate aim is missing", nameof(aims));
                if (!seen.Add(copy[i].Key)) throw new ArgumentException("Duplicate candidate aim key " + copy[i].Key, nameof(aims));
            }
            return copy;
        }
        public ModelCandidateAim Aim(string key) {
            foreach (var aim in Aims) if (aim.Key == key) return aim;
            return null;
        }
        // 고른 조준이 가리키는 지점이고, 조준이 없거나 모르는 키면 play의 지점이다.
        public Vector3 TargetFor(string aimKey) => (aimKey == null ? null : Aim(aimKey))?.Target ?? Target;
    }
    // 원본 llm_candidates.rs의 LlmCandidates다. 한 요청이 고르는 닫힌 선택지와 board 상태다.
    public sealed class ModelCandidateMenu {
        public IReadOnlyList<ModelCandidatePlay> Plays { get; }
        public string WaitDescription { get; }
        public ModelJsonMap State { get; }
        public ModelCandidateMenu(IReadOnlyList<ModelCandidatePlay> plays, string waitDescription, ModelJsonMap state) {
            if (plays == null) throw new ArgumentNullException(nameof(plays));
            var copy = new ModelCandidatePlay[plays.Count];
            var seen = new HashSet<string>();
            for (int i = 0; i < copy.Length; i++) {
                copy[i] = plays[i] ?? throw new ArgumentException("A candidate play is missing", nameof(plays));
                if (!seen.Add(copy[i].Key)) throw new ArgumentException("Duplicate candidate play key " + copy[i].Key, nameof(plays));
            }
            Plays = Array.AsReadOnly(copy);
            WaitDescription = waitDescription;
            // 바깥 caller가 같은 map이나 중첩된 값을 바꿔도 이 관측은 그대로 남는다.
            State = (state ?? throw new ArgumentNullException(nameof(state))).Freeze();
        }
        public bool OffersWait => WaitDescription != null;
        public bool HasPlays => Plays.Count > 0;
        // 적어도 한 선택지가 어디에 떨어질지 되묻는다.
        public bool AsksAims { get { foreach (var play in Plays) if (play.Aims.Count > 1) return true; return false; } }
        public bool Urgent => State.Get("enemyNearOurBase")?.AsBool == true;
        public ModelCandidatePlay Play(string key) {
            foreach (var play in Plays) if (play.Key == key) return play;
            return null;
        }
        // 선택지 키에서 설명문으로. wait는 마지막에 둔다.
        public ModelJsonMap Criteria() {
            var criteria = new ModelJsonMap();
            foreach (var play in Plays) criteria.Put(play.Key, play.Description);
            if (WaitDescription != null) criteria.Put(ModelProtocol.WaitPlayKey, WaitDescription);
            return criteria;
        }
        public string Describe(string key) {
            if (key == null) return null;
            if (key == ModelProtocol.WaitPlayKey) return WaitDescription;
            return Play(key)?.Description;
        }
        // 원본 stance_decision_source의 dearest_cost다. state의 손패에서 값이 붙은 가장 비싼 카드다.
        public int DearestManaCost {
            get {
                var hand = State.Get("hand")?.AsList;
                if (hand == null) return int.MaxValue;
                int dearest = int.MaxValue;
                foreach (var row in hand) {
                    int? cost = row.AsMap?.Get("cost")?.AsNumberInt;
                    if (!cost.HasValue || cost.Value >= ModelCandidateBuilder.UnpricedCost) continue;
                    if (dearest == int.MaxValue || cost.Value > dearest) dearest = cost.Value;
                }
                return dearest;
            }
        }
        // 원본 stance_decision_source의 attack_play다. 마나당 기지 피해가 가장 큰 선택지, 없으면 가장 비싼 소환이다.
        public ModelCandidatePlay AttackPlay {
            get {
                ModelCandidatePlay best = null; Fix64 bestKey = Fix64.Zero;
                foreach (var play in Plays) {
                    if (play.BaseDamage <= Fix64.Zero) continue;
                    var key = play.BaseDamage / Math.Max(1, play.ManaCost);
                    if (best != null && key <= bestKey) continue;
                    best = play; bestKey = key;
                }
                if (best != null) return best;
                foreach (var play in Plays) {
                    if (!play.Summon) continue;
                    if (best != null && play.ManaCost <= best.ManaCost) continue;
                    best = play;
                }
                return best;
            }
        }
        // 원본 stance_decision_source의 save_play다. 가장 비싼 카드를 낼 수 있게 된 뒤의 최선이다.
        public ModelCandidatePlay SavePlay {
            get {
                int dearest = DearestManaCost;
                ModelCandidatePlay best = null; Fix64 bestKey = Fix64.Zero;
                foreach (var play in Plays) {
                    if (play.ManaCost < dearest) continue;
                    var key = play.BaseDamage + play.UnitDamage;
                    if (best != null && key <= bestKey) continue;
                    best = play; bestKey = key;
                }
                return best;
            }
        }
        // 원본의 min_by처럼 가장 가까운 첫 후보를 고른다.
        public ModelCandidatePlay Nearest(long magicId, Vector3 point) {
            ModelCandidatePlay best = null; Fix64 bestDistance = Fix64.Zero;
            foreach (var play in Plays) {
                if (play.MagicId != magicId) continue;
                var distance = Vector3.DistanceSquared(play.Target, point);
                if (best != null && distance >= bestDistance) continue;
                best = play; bestDistance = distance;
            }
            return best;
        }
    }
    public interface IModelCandidateBuilder {
        ModelCandidateMenu Build(BotEye eye, IDictionary<string, Fix64> parameters, Master side, InputDecision standard);
    }
}
