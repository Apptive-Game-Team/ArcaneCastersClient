using System;
using System.Collections.Generic;
using System.Text;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    // 원본 unit_guide의 java_round/round1/round2/number와 llm_answer의 truncate/collapse_whitespace를 고정 소수점으로 옮긴다.
    public static class ModelText {
        public const int MaxReasonLength = 120;
        private const long Half = 1L << 31;

        // Math.round(double): 가장 가까운 정수, 0.5는 양의 무한대 쪽이다.
        public static long JavaRound(Fix64 value) {
            long raw = value.RawValue;
            long floor = raw >> 32;
            long fraction = raw - (floor << 32);
            return fraction >= Half ? floor + 1 : floor;
        }
        public static long Round1Tenths(Fix64 value) => JavaRound(value * 10);
        public static long Round2Hundredths(Fix64 value) => JavaRound(value * 100);

        // 소수점 한 자리까지만 적는다: 5.1, 13, -1.5, 0.
        public static string Number(Fix64 value) => Scaled(Round1Tenths(value), 1, true);

        // Java의 String.valueOf(double)이 이 크기에서 적는 모양이다: 13.0, 2.7, 0.69, 1.0, 0.7.
        public static string Decimal(long scaled, int places) => Scaled(scaled, places, false);

        private static string Scaled(long scaled, int places, bool dropZeroFraction) {
            long unit = 1; for (int i = 0; i < places; i++) unit *= 10;
            bool negative = scaled < 0;
            long magnitude = negative ? -scaled : scaled;
            long whole = magnitude / unit, fraction = magnitude % unit;
            if (dropZeroFraction && fraction == 0) return (negative && whole != 0 ? "-" : "") + whole;
            var digits = new StringBuilder(fraction.ToString().PadLeft(places, '0'));
            if (!dropZeroFraction) {
                while (digits.Length > 1 && digits[digits.Length - 1] == '0') digits.Length--;
            }
            return (negative && (whole != 0 || fraction != 0) ? "-" : "") + whole + "." + digits;
        }

        // text.replaceAll("\\s+", " ")에서 Java의 \s는 [ \t\n\x0B\f\r]다.
        public static string CollapseWhitespace(string text) {
            if (text == null) return null;
            var collapsed = new StringBuilder(text.Length);
            bool inRun = false;
            foreach (char character in text) {
                if (character == ' ' || character == '\t' || character == '\n' || character == '' || character == '' || character == '\r') {
                    if (!inRun) collapsed.Append(' ');
                    inRun = true;
                } else { collapsed.Append(character); inRun = false; }
            }
            return collapsed.ToString();
        }
        public static string Truncate(string text) {
            string line = CollapseWhitespace(text);
            return line == null || line.Length <= MaxReasonLength ? line : line.Substring(0, MaxReasonLength);
        }
    }

    public enum ModelJsonKind { Null, Bool, Int, Decimal, Text, List, Map }

    // 원본 llm_json의 LlmJson이다. 숫자는 Java의 타입을 유지하고 double 자리는 고정 소수점에서 만든 소수 문자열로 적는다.
    public sealed class ModelJson {
        public ModelJsonKind Kind { get; }
        private readonly bool flag;
        private readonly long integer;
        private readonly int places;
        private readonly string text;
        private readonly IReadOnlyList<ModelJson> list;
        private readonly ModelJsonMap map;
        public static readonly ModelJson Null = new ModelJson(ModelJsonKind.Null, false, 0, 0, null, null, null);
        private ModelJson(ModelJsonKind kind, bool flag, long integer, int places, string text, IReadOnlyList<ModelJson> list, ModelJsonMap map) {
            Kind = kind; this.flag = flag; this.integer = integer; this.places = places; this.text = text; this.list = list; this.map = map;
        }
        public static ModelJson Of(bool value) => new ModelJson(ModelJsonKind.Bool, value, 0, 0, null, null, null);
        public static ModelJson Of(long value) => new ModelJson(ModelJsonKind.Int, false, value, 0, null, null, null);
        public static ModelJson Of(string value) => value == null ? Null : new ModelJson(ModelJsonKind.Text, false, 0, 0, value, null, null);
        public static ModelJson Of(IReadOnlyList<ModelJson> value) => new ModelJson(ModelJsonKind.List, false, 0, 0, null, value, null);
        public static ModelJson Of(ModelJsonMap value) => new ModelJson(ModelJsonKind.Map, false, 0, 0, null, null, value);
        // Java의 double 자리다. scaled/10^places를 그 소수 문자열로 적는다.
        public static ModelJson Decimal(long scaled, int places) {
            if (places < 1) throw new ArgumentOutOfRangeException(nameof(places));
            return new ModelJson(ModelJsonKind.Decimal, false, scaled, places, null, null, null);
        }
        public static ModelJson Round1(Fix64 value) => Decimal(ModelText.Round1Tenths(value), 1);
        public static ModelJson Round2(Fix64 value) => Decimal(ModelText.Round2Hundredths(value), 2);
        public string AsText => Kind == ModelJsonKind.Text ? text : null;
        public bool? AsBool => Kind == ModelJsonKind.Bool ? flag : (bool?)null;
        // Number.intValue(): 정수는 그대로, 소수는 0 방향으로 버린다.
        public int? AsNumberInt {
            get {
                if (Kind == ModelJsonKind.Int) return (int)integer;
                if (Kind != ModelJsonKind.Decimal) return null;
                long unit = 1; for (int i = 0; i < places; i++) unit *= 10;
                return (int)(integer / unit);
            }
        }
        public IReadOnlyList<ModelJson> AsList => Kind == ModelJsonKind.List ? list : null;
        public ModelJsonMap AsMap => Kind == ModelJsonKind.Map ? map : null;
        // 관측과 요청은 digest로 고정되므로, 중첩된 목록과 map까지 복사해 얼린 스냅샷만 보관한다.
        public ModelJson Frozen() {
            switch (Kind) {
                case ModelJsonKind.List: {
                    var copy = new ModelJson[list.Count];
                    for (int i = 0; i < copy.Length; i++) copy[i] = (list[i] ?? Null).Frozen();
                    return Of(Array.AsReadOnly(copy));
                }
                case ModelJsonKind.Map: return Of(map.Freeze());
                default: return this;
            }
        }
        public string Write() {
            switch (Kind) {
                case ModelJsonKind.Null: return "null";
                case ModelJsonKind.Bool: return flag ? "true" : "false";
                case ModelJsonKind.Int: return integer.ToString();
                case ModelJsonKind.Decimal: return ModelText.Decimal(integer, places);
                case ModelJsonKind.Text: return Quote(text);
                case ModelJsonKind.Map: return map.Write();
                default: {
                    var json = new StringBuilder("[");
                    for (int i = 0; i < list.Count; i++) { if (i > 0) json.Append(','); json.Append(list[i].Write()); }
                    return json.Append(']').ToString();
                }
            }
        }
        public override string ToString() => Kind == ModelJsonKind.Text ? text : Write();
        internal static string Quote(string value) {
            var json = new StringBuilder("\"");
            foreach (char character in value) {
                switch (character) {
                    case '"': json.Append("\\\""); break;
                    case '\\': json.Append("\\\\"); break;
                    case '\n': json.Append("\\n"); break;
                    case '\r': json.Append("\\r"); break;
                    case '\t': json.Append("\\t"); break;
                    default:
                        if (character < ' ') json.Append("\\u").Append(((int)character).ToString("x4"));
                        else json.Append(character);
                        break;
                }
            }
            return json.Append('"').ToString();
        }
    }

    // LinkedHashMap<String, Object>다. 키는 넣은 순서를 지키고 같은 키를 다시 넣으면 제자리에서 값만 바뀐다.
    public sealed class ModelJsonMap {
        private readonly List<string> order = new List<string>();
        private readonly Dictionary<string, ModelJson> values = new Dictionary<string, ModelJson>();
        private bool frozen;
        public bool IsFrozen => frozen;
        public int Count => order.Count;
        public IReadOnlyList<string> Keys => order.AsReadOnly();
        // 얼린 뒤에는 바깥에서 같은 map을 들고 있어도 과거 관측·요청·digest가 바뀌지 않는다.
        public ModelJsonMap Freeze() {
            var copy = new ModelJsonMap();
            foreach (string key in order) copy.Put(key, values[key].Frozen());
            copy.frozen = true;
            return copy;
        }
        public void Put(string key, ModelJson value) {
            if (frozen) throw new InvalidOperationException("A frozen model state map does not change.");
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (!values.ContainsKey(key)) order.Add(key);
            values[key] = value ?? ModelJson.Null;
        }
        public void Put(string key, string value) => Put(key, ModelJson.Of(value));
        public void Put(string key, long value) => Put(key, ModelJson.Of(value));
        public void Put(string key, bool value) => Put(key, ModelJson.Of(value));
        public ModelJson Get(string key) => key != null && values.TryGetValue(key, out var value) ? value : null;
        public string GetText(string key) => Get(key)?.AsText;
        public bool ContainsKey(string key) => Get(key) != null;
        public IEnumerable<KeyValuePair<string, ModelJson>> Entries {
            get { foreach (string key in order) yield return new KeyValuePair<string, ModelJson>(key, values[key]); }
        }
        public string Write() {
            var json = new StringBuilder("{");
            for (int i = 0; i < order.Count; i++) {
                if (i > 0) json.Append(',');
                json.Append(ModelJson.Quote(order[i])).Append(':').Append(values[order[i]].Write());
            }
            return json.Append('}').ToString();
        }
        public override string ToString() => Write();
    }
}
