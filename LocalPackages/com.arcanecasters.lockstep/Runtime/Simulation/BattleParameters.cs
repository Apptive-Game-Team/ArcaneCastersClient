using System;
using System.Collections;
using System.Collections.Generic;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    // 원본 ParameterService처럼 객체 이름만 소문자로 정규화한다. 파라미터 이름은 구분한다.
    public sealed class BattleParameters : IDictionary<string, Fix64> {
        private readonly Dictionary<string, Fix64> values = new Dictionary<string, Fix64>(StringComparer.Ordinal);
        private static string Key(string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            int separator = key.IndexOf('.');
            return separator < 0 ? key : key.Substring(0, separator).ToLowerInvariant() + key.Substring(separator);
        }
        public Fix64 this[string key] {
            get {
                if (TryGetValue(key, out var value)) return value;
                int separator = key.IndexOf('.');
                throw new KeyNotFoundException("Parameter not found: " + (separator < 0 ? key : key.Substring(0, separator) + ", " + key.Substring(separator + 1)));
            }
            set => values[Key(key)] = value;
        }
        public Fix64 GetValue(string objectName, string parameter) => this[objectName + "." + parameter];
        public Fix64 GetValueOrDefault(string objectName, string parameter, Fix64 fallback) => TryGetValue(objectName + "." + parameter, out var value) ? value : fallback;
        public bool TryGetValue(string key, out Fix64 value) => values.TryGetValue(Key(key), out value);
        public void Add(string key, Fix64 value) => values.Add(Key(key), value);
        public bool ContainsKey(string key) => values.ContainsKey(Key(key));
        public bool Remove(string key) => values.Remove(Key(key));
        public ICollection<string> Keys => values.Keys;
        public ICollection<Fix64> Values => values.Values;
        public int Count => values.Count;
        public bool IsReadOnly => false;
        public void Clear() => values.Clear();
        public void Add(KeyValuePair<string, Fix64> item) => Add(item.Key, item.Value);
        public bool Contains(KeyValuePair<string, Fix64> item) => TryGetValue(item.Key, out var value) && value == item.Value;
        public bool Remove(KeyValuePair<string, Fix64> item) => Contains(item) && Remove(item.Key);
        public void CopyTo(KeyValuePair<string, Fix64>[] array, int index) => ((ICollection<KeyValuePair<string, Fix64>>)values).CopyTo(array, index);
        public IEnumerator<KeyValuePair<string, Fix64>> GetEnumerator() => values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
