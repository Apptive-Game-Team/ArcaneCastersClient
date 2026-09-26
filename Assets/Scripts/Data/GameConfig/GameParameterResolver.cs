using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Data.Magic;

namespace Data.GameConfig
{
    public static class GameParameterResolver
    {
        private static readonly string[] DisplayParamOrder =
        {
            "hp",
            "damage",
            "duration",
            "radius",
            "attack_range",
            "attack_interval",
            "speed",
            "count",
            "quantity",
        };

        private static readonly HashSet<string> HiddenParams = new()
        {
            "magic_id",
            "mass"
        };

        // FieldSelector는 필드 선택 중 매 프레임 range/radius를 조회한다. 아래 해석 로직은
        // parameters 전체를 몇 번씩 훑는 LINQ 파이프라인이라 프레임마다 돌리면 비용이 크므로
        // (magic, paramName)별 결과를 메모이즈한다. null 값은 "찾지 못함"을 뜻한다.
        private static readonly Dictionary<MagicParameterKey, float?> MagicParameterCache = new();

        // TryGetObjectParameter도 조준 중 매 프레임 불리는 경로라 같은 이유로 메모이즈한다.
        // (gameObjectName, paramName)별 결과라 MagicParameterCache와는 키가 다르지만 무효화 규칙은 같다.
        private static readonly Dictionary<ObjectParameterKey, float?> ObjectParameterCache = new();
        private static IReadOnlyList<GameParameterData> cachedParameterSource;

        public static string GetMagicDisplayStats(CombinedMagicData magic)
        {
            if (magic == null)
            {
                return string.Empty;
            }

            var parameters = ParametersDataSource.GetCachedParameters();
            if (parameters == null || parameters.Count == 0)
            {
                return string.Empty;
            }

            var objectNames = GetObjectNamesForMagic(parameters, magic);
            if (objectNames.Count == 0)
            {
                return string.Empty;
            }

            var blocks = new List<string>();
            foreach (var objectName in objectNames)
            {
                var stats = parameters
                    .Where(parameter => IsSameName(parameter.gameObjectName, objectName))
                    .Where(parameter => !HiddenParams.Contains(parameter.paramName))
                    .GroupBy(parameter => parameter.paramName)
                    .Select(group => group.First())
                    .OrderBy(parameter => GetDisplayOrder(parameter.paramName))
                    .ThenBy(parameter => parameter.paramName)
                    .ToList();

                if (stats.Count == 0)
                {
                    continue;
                }

                var lines = stats
                    .Select(parameter => $"{ToDisplayName(parameter.paramName)}: {FormatValue(parameter.value)}")
                    .ToList();

                var rows = new List<string>((lines.Count + 1) / 2);
                for (int i = 0; i < lines.Count; i += 2)
                {
                    rows.Add(i + 1 < lines.Count
                        ? $"{lines[i]}\t{lines[i + 1]}"
                        : lines[i]);
                }

                blocks.Add(string.Join("\n", rows));
            }

            return string.Join("\n\n", blocks);
        }

        public static bool TryGetMagicParameter(CombinedMagicData magic, string paramName, out float value)
        {
            value = 0f;
            if (magic == null || string.IsNullOrWhiteSpace(paramName))
            {
                return false;
            }

            var parameters = ParametersDataSource.GetCachedParameters();
            if (parameters == null || parameters.Count == 0)
            {
                return false;
            }

            InvalidateCacheIfSourceChanged(parameters);

            var key = new MagicParameterKey(magic, paramName);
            if (MagicParameterCache.TryGetValue(key, out float? cached))
            {
                if (!cached.HasValue)
                {
                    return false;
                }

                value = cached.Value;
                return true;
            }

            bool resolved = ResolveMagicParameter(parameters, magic, paramName, out value);
            MagicParameterCache[key] = resolved ? value : (float?)null;
            return resolved;
        }

        /// <summary>
        /// ParametersDataSource는 갱신될 때마다 새 List를 배정하므로, 참조가 바뀌면 캐시를 버린다.
        /// </summary>
        private static void InvalidateCacheIfSourceChanged(IReadOnlyList<GameParameterData> parameters)
        {
            if (ReferenceEquals(cachedParameterSource, parameters))
            {
                return;
            }

            cachedParameterSource = parameters;
            MagicParameterCache.Clear();
            ObjectParameterCache.Clear();
        }

        /// <summary>
        /// game object 이름을 직접 적어 그 parameter 를 읽는다. indicator document 의
        /// <c>{"object":...,"parameter":...}</c> 값이 마법 자신이 아닌 다른 game object 를 가리킬 때 쓴다.
        /// </summary>
        public static bool TryGetObjectParameter(string gameObjectName, string paramName, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(gameObjectName) || string.IsNullOrWhiteSpace(paramName))
            {
                return false;
            }

            var parameters = ParametersDataSource.GetCachedParameters();
            if (parameters == null || parameters.Count == 0)
            {
                return false;
            }

            InvalidateCacheIfSourceChanged(parameters);

            var key = new ObjectParameterKey(gameObjectName, paramName);
            if (ObjectParameterCache.TryGetValue(key, out float? cached))
            {
                if (!cached.HasValue)
                {
                    return false;
                }

                value = cached.Value;
                return true;
            }

            var parameter = parameters.FirstOrDefault(p =>
                IsSameName(p.gameObjectName, gameObjectName) &&
                IsSameName(p.paramName, paramName));

            bool resolved = parameter != null;
            if (resolved)
            {
                value = parameter.value;
            }

            ObjectParameterCache[key] = resolved ? value : (float?)null;
            return resolved;
        }

        private static bool ResolveMagicParameter(
            IReadOnlyList<GameParameterData> parameters,
            CombinedMagicData magic,
            string paramName,
            out float value)
        {
            value = 0f;
            var objectNames = GetObjectNamesForParameter(parameters, magic, paramName);
            foreach (var objectName in objectNames)
            {
                var parameter = parameters.FirstOrDefault(p =>
                    IsSameName(p.gameObjectName, objectName) &&
                    IsSameName(p.paramName, paramName));
                if (parameter != null)
                {
                    value = parameter.value;
                    return true;
                }
            }

            return false;
        }

        private static List<string> GetObjectNamesForParameter(
            IReadOnlyList<GameParameterData> parameters,
            CombinedMagicData magic,
            string paramName)
        {
            // 시전 종류 축이 없어졌으므로 range 도 마법 이름으로만 찾는다.
            // game_objects.name 이 magics.name 과 같아서 아래 후보만으로 닿는다.
            var result = new List<string>(GetObjectNamesForMagic(parameters, magic));
            return result
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Where(name => parameters.Any(parameter => IsSameName(parameter.gameObjectName, name)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> GetObjectNamesForMagic(
            IReadOnlyList<GameParameterData> parameters,
            CombinedMagicData magic)
        {
            var result = parameters
                .Where(parameter => IsSameName(parameter.paramName, "magic_id"))
                .Where(parameter => Math.Abs(parameter.value - magic.id) < 0.01f)
                .Select(parameter => parameter.gameObjectName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            if (result.Count > 0)
            {
                return result;
            }

            var candidates = new[]
            {
                magic.serverName,
                ToSnakeCase(magic.resourceName),
                magic.localizationKey
            };

            return candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                .Where(candidate => parameters.Any(parameter => IsSameName(parameter.gameObjectName, candidate)))
                .Distinct()
                .ToList();
        }

        private static int GetDisplayOrder(string paramName)
        {
            var index = Array.IndexOf(DisplayParamOrder, paramName);
            return index >= 0 ? index : DisplayParamOrder.Length;
        }

        private static bool IsSameName(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string ToDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.Replace("_", " "));
        }

        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var chars = new List<char>();
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (char.IsUpper(current) && i > 0)
                {
                    chars.Add('_');
                }

                chars.Add(char.ToLowerInvariant(current));
            }

            return new string(chars.ToArray());
        }

        private static string FormatValue(float value)
        {
            return Math.Abs(value - Math.Round(value)) < 0.001f
                ? ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <see cref="ResolveMagicParameter"/>가 실제로 읽는 입력만 담는 캐시 키.
        /// 여기 없는 필드가 결과에 영향을 주지 않으므로 잘못된 캐시 적중이 생기지 않는다.
        /// </summary>
        private readonly struct MagicParameterKey : IEquatable<MagicParameterKey>
        {
            private readonly long magicId;
            private readonly string serverName;
            private readonly string resourceName;
            private readonly string localizationKey;
            private readonly string paramName;

            public MagicParameterKey(CombinedMagicData magic, string paramName)
            {
                magicId = magic.id;
                serverName = magic.serverName;
                resourceName = magic.resourceName;
                localizationKey = magic.localizationKey;
                this.paramName = paramName;
            }

            public bool Equals(MagicParameterKey other)
            {
                return magicId == other.magicId &&
                       string.Equals(serverName, other.serverName, StringComparison.Ordinal) &&
                       string.Equals(resourceName, other.resourceName, StringComparison.Ordinal) &&
                       string.Equals(localizationKey, other.localizationKey, StringComparison.Ordinal) &&
                       string.Equals(paramName, other.paramName, StringComparison.Ordinal);
            }

            public override bool Equals(object obj) => obj is MagicParameterKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = magicId.GetHashCode();
                    hash = hash * 397 ^ (serverName?.GetHashCode() ?? 0);
                    hash = hash * 397 ^ (resourceName?.GetHashCode() ?? 0);
                    hash = hash * 397 ^ (localizationKey?.GetHashCode() ?? 0);
                    hash = hash * 397 ^ (paramName?.GetHashCode() ?? 0);
                    return hash;
                }
            }
        }

        /// <summary>
        /// <see cref="TryGetObjectParameter"/>가 읽는 입력만 담는 캐시 키.
        /// 이름 비교가 <see cref="IsSameName"/> 처럼 대소문자를 구분하지 않으므로 키도 그렇게 비교한다.
        /// </summary>
        private readonly struct ObjectParameterKey : IEquatable<ObjectParameterKey>
        {
            private readonly string gameObjectName;
            private readonly string paramName;

            public ObjectParameterKey(string gameObjectName, string paramName)
            {
                this.gameObjectName = gameObjectName;
                this.paramName = paramName;
            }

            public bool Equals(ObjectParameterKey other)
            {
                return string.Equals(gameObjectName, other.gameObjectName, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(paramName, other.paramName, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj) => obj is ObjectParameterKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = gameObjectName != null
                        ? StringComparer.OrdinalIgnoreCase.GetHashCode(gameObjectName)
                        : 0;
                    hash = hash * 397 ^ (paramName != null
                        ? StringComparer.OrdinalIgnoreCase.GetHashCode(paramName)
                        : 0);
                    return hash;
                }
            }
        }
    }
}
