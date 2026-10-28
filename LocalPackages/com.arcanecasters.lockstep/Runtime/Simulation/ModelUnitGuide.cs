using System;
using System.Collections.Generic;
using System.Text;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    // 원본 unit_guide.rs의 Stats다. DB에 유닛 설명문이 없으므로 hp·damage·공격 간격·사거리·속도가 설명 그 자체다.
    public readonly struct ModelUnitStats {
        public readonly Fix64 Hp, Damage, AttackInterval, AttackRange, Speed, PushForce;
        public readonly bool Flying;
        public ModelUnitStats(Fix64 hp, Fix64 damage, Fix64 attackInterval, Fix64 attackRange, Fix64 speed, bool flying, Fix64 pushForce) {
            Hp = hp; Damage = damage; AttackInterval = attackInterval; AttackRange = attackRange; Speed = speed; Flying = flying; PushForce = pushForce;
        }
        public bool Known => Hp > Fix64.Zero || Damage > Fix64.Zero;
        public Fix64 DamagePerSecond => AttackInterval > Fix64.Zero ? Damage / AttackInterval : Damage;
        // 기지를 때리기 시작하기까지의 초다. 움직이지 않는 유닛은 값이 없다.
        public Fix64? SecondsToReach(Fix64 distance) {
            var gap = MathHelper.Max(Fix64.Zero, distance - AttackRange);
            if (gap == Fix64.Zero) return Fix64.Zero;
            return Speed > Fix64.Zero ? gap / Speed : (Fix64?)null;
        }
    }

    public sealed class ModelUnitGuide {
        private readonly IDictionary<string, Fix64> parameters;
        public ModelUnitGuide(IDictionary<string, Fix64> parameters) {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }
        // prefab의 파라미터는 _prefab 접미사를 뗀 game object 이름 아래에 있다.
        public static string ObjectName(string prefab) => UnitCatalogue.GameObjectName(prefab);
        private Fix64 Value(string prefab, string parameter) =>
            parameters.TryGetValue(ObjectName(prefab) + "." + parameter, out var value) ? value : Fix64.Zero;
        public ModelUnitStats Stats(string prefab) => new ModelUnitStats(
            Value(prefab, "hp"), Value(prefab, "damage"), Value(prefab, "attack_interval"),
            Value(prefab, "attack_range"), Value(prefab, "speed"), Value(prefab, "spawn_height") > Fix64.Zero, Value(prefab, "push_force"));

        // "flying, 50 hp, hits for 300 every 3s (100 dps) at range 1.5, speed 1.5"
        public string Describe(string prefab) {
            var stats = Stats(prefab);
            if (!stats.Known) return "no stats known";
            var text = new StringBuilder(stats.Flying ? "flying" : "ground");
            text.Append(", ").Append(ModelText.Number(stats.Hp)).Append(" hp");
            if (stats.Damage > Fix64.Zero) {
                text.Append(", hits for ").Append(ModelText.Number(stats.Damage));
                if (stats.AttackInterval > Fix64.Zero)
                    text.Append(" every ").Append(ModelText.Number(stats.AttackInterval)).Append("s (").Append(ModelText.Number(stats.DamagePerSecond)).Append(" dps)");
                if (stats.AttackRange > Fix64.Zero) text.Append(" at range ").Append(ModelText.Number(stats.AttackRange));
            }
            if (stats.Speed > Fix64.Zero) text.Append(", speed ").Append(ModelText.Number(stats.Speed));
            else text.Append(", does not move");
            if (stats.PushForce > Fix64.Zero) text.Append(", pushes enemy units back");
            return text.ToString();
        }

        // "flies at speed 1.5; 50 hp; hits for 300 every 3s (100 dps) at range 1.5"
        public string Role(string prefab) {
            var stats = Stats(prefab);
            if (!stats.Known) return "no stats known";
            var text = new StringBuilder();
            if (stats.Speed > Fix64.Zero) text.Append(stats.Flying ? "flies" : "walks").Append(" at speed ").Append(ModelText.Number(stats.Speed));
            else text.Append("stays where it is placed and never moves, so it only acts on enemies that come to it");
            text.Append("; ").Append(ModelText.Number(stats.Hp)).Append(" hp");
            if (stats.Damage > Fix64.Zero) {
                text.Append("; hits for ").Append(ModelText.Number(stats.Damage));
                if (stats.AttackInterval > Fix64.Zero)
                    text.Append(" every ").Append(ModelText.Number(stats.AttackInterval)).Append("s (").Append(ModelText.Number(stats.DamagePerSecond)).Append(" dps)");
                if (stats.AttackRange > Fix64.Zero) text.Append(" at range ").Append(ModelText.Number(stats.AttackRange));
            }
            if (stats.PushForce > Fix64.Zero) text.Append("; blows enemy units back");
            return text.ToString();
        }

        // 서 있는 자리에서 작용하는 거리다. 공격 사거리와 밀어내기 범위 가운데 긴 쪽이다.
        public Fix64 Reach(string prefab) => MathHelper.Max(Value(prefab, "attack_range"), Value(prefab, "push_range_x"));
    }

    // 원본 magic_descriptions.rs다. 플레이어가 읽는 카드 설명문을 마법 이름으로 찾는다.
    // 생산 기본값은 MagicDescriptionResource에 묶인 사본이므로 .NET/Mono/WebGL이 모두 같은 텍스트를 읽는다.
    public sealed class MagicDescriptions {
        public static readonly MagicDescriptions Empty = new MagicDescriptions(new Dictionary<string, string>());
        private static MagicDescriptions bundled;
        // 원본 bundled()의 OnceLock처럼 한 번만 읽는다. 파일 경로나 환경을 보지 않는다.
        public static MagicDescriptions Bundled => bundled ?? (bundled = new MagicDescriptions(MagicDescriptionResource.Parse()));
        private readonly IDictionary<string, string> byName;
        public int Count => byName.Count;
        public MagicDescriptions(IDictionary<string, string> byName) {
            this.byName = byName ?? throw new ArgumentNullException(nameof(byName));
        }
        public string Describe(string magicName) => magicName != null && byName.TryGetValue(magicName, out var text) ? text : null;
        public static MagicDescriptions Parse(string text) {
            var entries = ParseProperties(text);
            var map = new Dictionary<string, string>();
            foreach (var entry in entries) map[entry.Key] = entry.Value;
            return new MagicDescriptions(map);
        }

        // java.util.Properties.load(Reader): #/! 주석, =/:/공백 구분, 역슬래시 escape, 홀수 개의 끝 역슬래시로 이어지는 줄.
        // 같은 키는 마지막 값을 남기고, 잘못된 \u escape는 전체 읽기를 실패시킨다.
        public static List<KeyValuePair<string, string>> ParseProperties(string text) {
            var entries = new List<KeyValuePair<string, string>>();
            foreach (string logical in LogicalLines(text ?? "")) {
                int keyEnd = KeyEnd(logical);
                string rawKey = logical.Substring(0, keyEnd);
                string rest = TrimStartSpaces(logical.Substring(keyEnd));
                if (rest.Length > 0 && (rest[0] == '=' || rest[0] == ':')) rest = TrimStartSpaces(rest.Substring(1));
                string key = Unescape(rawKey), value = Unescape(rest);
                int existing = entries.FindIndex(entry => entry.Key == key);
                if (existing >= 0) entries[existing] = new KeyValuePair<string, string>(key, value);
                else entries.Add(new KeyValuePair<string, string>(key, value));
            }
            return entries;
        }
        private static bool IsPropertiesSpace(char character) => character == ' ' || character == '\t' || character == '';
        private static string TrimStartSpaces(string value) {
            int index = 0;
            while (index < value.Length && IsPropertiesSpace(value[index])) index++;
            return value.Substring(index);
        }
        private static List<string> LogicalLines(string text) {
            var lines = new List<string>();
            string current = null;
            foreach (string natural in text.Split('\n')) {
                string withoutReturn = natural.EndsWith("\r", StringComparison.Ordinal) ? natural.Substring(0, natural.Length - 1) : natural;
                // 홀로 선 '\r'도 한 줄을 끝낸다.
                foreach (string part in withoutReturn.Split('\r')) {
                    string stripped = TrimStartSpaces(part);
                    bool continuing = current != null;
                    if (!continuing && (stripped.Length == 0 || stripped[0] == '#' || stripped[0] == '!')) continue;
                    int trailing = 0;
                    for (int i = stripped.Length - 1; i >= 0 && stripped[i] == '\\'; i--) trailing++;
                    bool continues = trailing % 2 == 1;
                    string body = continues ? stripped.Substring(0, stripped.Length - 1) : stripped;
                    string line = (current ?? "") + body;
                    current = null;
                    if (continues) current = line; else lines.Add(line);
                }
            }
            if (current != null) lines.Add(current);
            return lines;
        }
        private static int KeyEnd(string line) {
            bool escaped = false;
            for (int index = 0; index < line.Length; index++) {
                char character = line[index];
                if (escaped) { escaped = false; continue; }
                if (character == '\\') { escaped = true; continue; }
                if (character == '=' || character == ':' || IsPropertiesSpace(character)) return index;
            }
            return line.Length;
        }
        private static string Unescape(string raw) {
            var text = new StringBuilder(raw.Length);
            for (int index = 0; index < raw.Length; index++) {
                char character = raw[index];
                if (character != '\\') { text.Append(character); continue; }
                if (++index >= raw.Length) break;
                switch (raw[index]) {
                    case 't': text.Append('\t'); break;
                    case 'n': text.Append('\n'); break;
                    case 'r': text.Append('\r'); break;
                    case 'f': text.Append(''); break;
                    case 'u': {
                        if (index + 4 >= raw.Length) throw new System.IO.InvalidDataException("Malformed \\uxxxx encoding.");
                        string hex = raw.Substring(index + 1, 4);
                        if (!ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ushort unit))
                            throw new System.IO.InvalidDataException("Malformed \\uxxxx encoding.");
                        index += 4; text.Append((char)unit);
                        break;
                    }
                    default: text.Append(raw[index]); break;
                }
            }
            return text.ToString();
        }
    }
}
