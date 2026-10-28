using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using ArcaneCasters.Simulation;
using FixMath.NET;

namespace ArcaneCasters.Parity {
    // Java 테스트 fixture의 두 INSERT 형태만 읽는다. 게임 실행 중 SQL을 해석하지 않는다.
    internal static class ParameterSqlFixture {
        public static string Read(string name) {
#if UNITY_5_3_OR_NEWER
            var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("LockstepFixtures/" + name);
            if (asset == null) throw new FileNotFoundException("Missing parameter fixture: " + name);
            return asset.text;
#else
            using var stream = typeof(ParameterSqlFixture).Assembly.GetManifestResourceStream("LockstepFixtures." + name);
            if (stream == null) throw new FileNotFoundException("Missing parameter fixture: " + name);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
#endif
        }
        private static string Capture(string text, string pattern) {
            var match = Regex.Match(text, pattern);
            if (!match.Success) throw new FormatException("Unsupported parameter fixture insert: " + text);
            return match.Groups[1].Value;
        }
        public static int Load(BattleParameters target, string sql) {
            int loaded = 0;
            void Insert(string owner, string parameter, string number) {
                var value = (Fix64)decimal.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture);
                string key = owner + "." + parameter;
                if (target.ContainsKey(key)) return;
                target.Add(key, value); loaded++;
            }
            foreach (string statement in sql.Split(';')) {
                if (!statement.Contains("INSERT INTO parameter_values")) continue;
                const string end = ") AS seed(game_object_name, parameter_name, parameter_value)";
                int seedEnd = statement.IndexOf(end, StringComparison.Ordinal);
                if (seedEnd >= 0) {
                    string prefix = statement.Substring(0, seedEnd);
                    int start = prefix.LastIndexOf("VALUES", StringComparison.Ordinal);
                    if (start < 0) throw new FormatException("Missing fixture VALUES");
                    string rows = prefix.Substring(start + 6);
                    const string pattern = @"\(\s*'([^']+)'\s*,\s*'([^']+)'\s*,\s*([+-]?[0-9]+(?:\.[0-9]+)?)\s*\)";
                    var matches = Regex.Matches(rows, pattern);
                    if (matches.Count == 0 || Regex.Replace(rows, pattern, "").Trim(' ', ',', '\r', '\n', '\t').Length != 0)
                        throw new FormatException("Invalid fixture seed rows");
                    foreach (Match row in matches) Insert(row.Groups[1].Value, row.Groups[2].Value, row.Groups[3].Value);
                } else {
                    string number = Capture(statement, @"SELECT go\.id, p\.id,\s*([^\r\n]+)").Trim();
                    string parameter = Capture(statement, @"p\.name = '([^']+)'");
                    var single = Regex.Match(statement, @"go\.name = '([^']+)'");
                    if (single.Success) Insert(single.Groups[1].Value, parameter, number);
                    else {
                        string names = Capture(statement, @"go\.name IN \(([^)]+)\)");
                        foreach (string name in names.Split(',')) Insert(name.Trim().Trim('\''), parameter, number);
                    }
                }
            }
            return loaded;
        }
    }
}
