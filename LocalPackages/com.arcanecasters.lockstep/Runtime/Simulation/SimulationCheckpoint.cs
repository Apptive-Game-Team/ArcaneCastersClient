using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    [Serializable]
    public sealed class StateCheckpoint {
        public uint tick;
        public string hash;
    }

    // 복구 snapshot이 아니다. 규칙 객체의 모든 instance field를 읽고 물리는 별도 typed writer에 위임한다.
    // 알 수 없는 외부 상태는 조용히 생략하지 않고 경기를 실패시킨다. 신규 규칙 field는 자동 포함된다.
    public static class SimulationCheckpoint {
        public const int Version = 1;
        private static readonly Dictionary<string, string> Exclusions = new Dictionary<string, string>(StringComparer.Ordinal) {
            { "SimulationContext.events", "전장 표시용 BattleEvent 큐이며 규칙은 이 큐를 읽지 않는다." },
            { "SimulationContext.Created", "화면 객체 생성 알림이다." },
            { "SimulationContext.Updated", "화면 상태 갱신 알림이다." },
            { "SimulationContext.InitializationFailed", "오류 보고이며 실패한 재생은 저장하지 않는다." },
            { "SimulationContext.MagicUsed", "통계 구독자는 별도 verifier가 재구성한다." },
            { "BattleFrameSystems.<Output>k__BackingField", "플랫폼별 화면 출력이며 규칙으로 역입력하지 않는다." },
            { "BattleFrameLoop.HandleGameEnd", "결과 전달 hook이다. 승패는 Context.Result에 있다." },
            { "BattleFrameLoop.GameEnded", "종료 출력 hook이며 종료 상태는 loop field에 있다." },
            { "BattleSessionRuntime.Terminated", "실행 host의 종료 통지다." },
            { "BattleSessionRuntime.PveResultDecided", "결과 저장 통지다." },
            { "BattleSessionRuntime.PveResultReady", "PVE 결과 출력이다." },
            { "PveScriptSystem.<SendScriptEvent>k__BackingField", "대사 전송 출력이다." },
            { "PveActionRunner.<SendState>k__BackingField", "PVE 화면 상태 전송 출력이다." },
            { "PveObjectiveSystem.send", "PVE 목표 전송 출력이다." },
            { "BotAgent.Thought", "봇 판단 설명 출력이다." },
            { "SessionBotControl.Thought", "봇 판단 설명 출력이다." },
            { "BotAgent.gate", "동기 봇 실행의 monitor다." },
            { "GameActionQueue`1.gate", "입력 큐 monitor다." },
            { "GameActionQueue`1.Failed", "실패한 입력의 오류 통지다." },
            { "ComponentUpdateSystem.Failed", "실패한 컴포넌트의 오류 통지다." },
            { "PveScriptSystem.Failed", "실패한 script의 오류 통지다." }
        };
        private static bool Excluded(FieldInfo field, out string reason) {
            if (Exclusions.TryGetValue(field.DeclaringType.Name + "." + field.Name, out reason)) return true;
            reason = null; return false;
        }
        private static readonly ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new ConcurrentDictionary<Type, FieldInfo[]>();
        private static readonly ConcurrentDictionary<FieldInfo, bool> EventCache = new ConcurrentDictionary<FieldInfo, bool>();
        private static IEnumerable<FieldInfo> Fields(Type type) => FieldCache.GetOrAdd(type, DiscoverFields);
        private static FieldInfo[] DiscoverFields(Type type) {
            var fields = new List<FieldInfo>();
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
                fields.AddRange(current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
            return fields.OrderBy(field => field.DeclaringType.FullName + "." + field.Name, StringComparer.Ordinal).ToArray();
        }
        public static string[] FieldManifest() => typeof(SimulationContext).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(SimulationContext).Namespace && !type.IsEnum && !typeof(Delegate).IsAssignableFrom(type))
            .SelectMany(type => Fields(type).Where(field => field.DeclaringType == type))
            .Select(field => field.DeclaringType.FullName + "." + field.Name + ":" + field.FieldType.FullName + (Excluded(field, out var reason) ? " excluded=" + reason : " included"))
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        public static string Hash(BattleSessionRuntime runtime) {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Bytes(runtime))).Replace("-", "").ToLowerInvariant();
        }
        public static byte[] Bytes(BattleSessionRuntime runtime) {
            if (runtime == null || runtime.Failure != null) throw new InvalidOperationException("A failed battle has no valid checkpoint.");
            using (var bytes = new MemoryStream()) {
                using (var writer = new BinaryWriter(bytes, new UTF8Encoding(false), true)) {
                    writer.Write("ArcaneCasters.state"); writer.Write(Version);
                    var state = new RuleWriter(writer);
                    state.Write(runtime, "battle");
                }
                return bytes.ToArray();
            }
        }
        private sealed class References : IEqualityComparer<object> {
            public new bool Equals(object a, object b) => ReferenceEquals(a, b);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
        private sealed class RuleWriter {
            private readonly BinaryWriter writer;
            private readonly Dictionary<object, int> references = new Dictionary<object, int>(new References());
            private int nodes;
            public RuleWriter(BinaryWriter writer) { this.writer = writer; }
            private static bool RuleType(Type type) => type != null && type.Namespace == typeof(SimulationContext).Namespace;
            private static bool LogicCallback(Delegate callback) {
                for (var type = callback.Method.DeclaringType; type != null; type = type.DeclaringType)
                    if (typeof(IBattleFrameOutput).IsAssignableFrom(type)) return false;
                return RuleType(callback.Method.DeclaringType);
            }
            private static readonly ConcurrentDictionary<Type, string> TypeNames = new ConcurrentDictionary<Type, string>();
            private static string TypeName(Type type) => TypeNames.GetOrAdd(type, BuildTypeName);
            private static string BuildTypeName(Type type) {
                if (type.IsArray) return TypeName(type.GetElementType()) + "[]";
                if (type.IsGenericType) return (type.GetGenericTypeDefinition().FullName ?? type.Name) + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">";
                return type.FullName ?? type.Name;
            }
            private static string Key(object key) {
                if (key == null) return "null";
                if (key is string text) return "s:" + text;
                if (key is Type type) return "t:" + TypeName(type);
                if (key is SimObject owner) return "o:" + owner.Id.ToString("D10", CultureInfo.InvariantCulture);
                if (key is SimComponent component) return "c:" + component.SourceId.ToString("D20", CultureInfo.InvariantCulture);
                if (key is Fix64 raw) return "fixed:" + raw.RawValue.ToString(CultureInfo.InvariantCulture);
                if (key.GetType().IsValueType && !key.GetType().IsPrimitive && !key.GetType().IsEnum)
                    return TypeName(key.GetType()) + "{" + string.Join(";", Fields(key.GetType()).Select(field => field.Name + "=" + Key(field.GetValue(key)))) + "}";
                if (key is Enum || key is IConvertible) return TypeName(key.GetType()) + ":" + Convert.ToString(key, CultureInfo.InvariantCulture);
                throw new InvalidDataException("Checkpoint requires a stable collection key: " + TypeName(key.GetType()));
            }
            public void Write(object value, string path, bool outputSubscription = false) {
                if (++nodes > 2000000) throw new InvalidDataException("Checkpoint graph exceeds its limit: " + path);
                if (value is Delegate onlyOutput && !onlyOutput.GetInvocationList().Any(LogicCallback)) {
                    if (!outputSubscription) throw new InvalidDataException("External logic callback entered checkpoint: " + path);
                    value = null;
                }
                if (value == null) { writer.Write((byte)0); return; }
                if (value is Type metadata) { writer.Write((byte)1); writer.Write("System.Type"); writer.Write(TypeName(metadata)); return; }
                Type type = value.GetType();
                if (value is IBattleFrameOutput || value is IPveSessionSink) { writer.Write((byte)0); return; }
                writer.Write((byte)1); writer.Write(TypeName(type));
                if (value is string text) { writer.Write(text); return; }
                if (value is Fix64 fixedValue) { writer.Write(fixedValue.RawValue); return; }
                if (type.IsEnum) { writer.Write(Convert.ToInt64(value, CultureInfo.InvariantCulture)); return; }
                if (type.IsPrimitive || value is decimal) {
                    if (value is float || value is double) throw new InvalidDataException("Floating point entered the rule checkpoint: " + path);
                    writer.Write(Convert.ToString(value, CultureInfo.InvariantCulture)); return;
                }
                // .NET은 빈 read-only snapshot을 공유하고 Mono는 새 wrapper를 만들 수 있다.
                // 읽기 전용 컬렉션과 길이 0 배열은 참조 공유가 아닌 내용으로 기록한다.
                bool valueCollection = type.IsArray && ((Array)value).Length == 0
                    || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.ObjectModel.ReadOnlyCollection<>);
                if (valueCollection) writer.Write((byte)4);
                else if (!type.IsValueType) {
                    if (references.TryGetValue(value, out int previous)) { writer.Write((byte)2); writer.Write(previous); return; }
                    writer.Write((byte)3); references.Add(value, references.Count);
                }
                if (value is PhysicsWorld physics) { physics.WriteCheckpoint(writer); return; }
                if (value is BattlePhysicsSystem system) { system.WriteCheckpoint(writer); return; }
                if (value is Delegate callback) {
                    // 화면·통계 구독자의 수나 closure는 replica 규칙 상태에 넣지 않는다.
                    var logic = callback.GetInvocationList().Where(item => LogicCallback(item)).ToArray();
                    writer.Write(logic.Length);
                    foreach (var item in logic) {
                        writer.Write(TypeName(item.Method.DeclaringType)); writer.Write(item.Method.Name);
                        Write(item.Target, path + ".callback");
                    }
                    return;
                }
                if (value is IDictionary dictionary) {
                    var entries = dictionary.Keys.Cast<object>().OrderBy(Key, StringComparer.Ordinal).ToArray();
                    writer.Write(entries.Length);
                    foreach (var entry in entries) { Write(entry, path + ".key"); Write(dictionary[entry], path + "[" + Key(entry) + "]"); }
                    return;
                }
                var dictionaryContract = type.GetInterfaces().FirstOrDefault(contract => contract.IsGenericType
                    && (contract.GetGenericTypeDefinition() == typeof(IDictionary<,>) || contract.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
                if (dictionaryContract != null) {
                    var entries = ((IEnumerable)value).Cast<object>().Select(entry => new {
                        Key = entry.GetType().GetProperty("Key").GetValue(entry, null),
                        Value = entry.GetType().GetProperty("Value").GetValue(entry, null)
                    }).OrderBy(entry => Key(entry.Key), StringComparer.Ordinal).ToArray();
                    writer.Write(entries.Length);
                    foreach (var entry in entries) { Write(entry.Key, path + ".key"); Write(entry.Value, path + "[" + Key(entry.Key) + "]"); }
                    return;
                }
                if (value is IEnumerable sequence) {
                    var entries = sequence.Cast<object>().ToArray();
                    bool isSet = type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ISet<>));
                    if (isSet) entries = entries.OrderBy(Key, StringComparer.Ordinal).ToArray();
                    writer.Write(entries.Length);
                    for (int i = 0; i < entries.Length; i++) Write(entries[i], path + "[" + i + "]");
                    return;
                }
                if (!RuleType(type) && type.Namespace != "BEPUutilities" && !(type.IsValueType && (type.Namespace == "System" || type.Namespace == "System.Collections.Generic")))
                    throw new InvalidDataException("Unowned checkpoint state: " + TypeName(type) + " at " + path);
                foreach (var field in Fields(type)) {
                    if (Excluded(field, out _)) continue;
                    writer.Write(TypeName(field.DeclaringType) + "." + field.Name);
                    bool isEvent = EventCache.GetOrAdd(field, item => item.DeclaringType.GetEvent(item.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null);
                    Write(field.GetValue(value), path + "." + field.Name, isEvent);
                }
                writer.Write((byte)255);
            }
        }
    }
}

