using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.GameConfig;
using Data.Localization;

namespace Data.Magic
{
    /// <summary>
    /// 도감과 다른 화면이 동일한 마법 상세 문구를 표시하도록 만든다.
    /// </summary>
    public static class MagicBookDetailText
    {
        public static async Task<string> BuildAsync(CombinedMagicData data, bool compactStats = false)
        {
            if (data == null)
            {
                return string.Empty;
            }

            string stats = GameParameterResolver.GetMagicDisplayStats(data);
            if (compactStats)
            {
                stats = CompactStats(stats);
            }

            string description = await GetDescriptionAsync(data);
            return Combine(stats, description);
        }

        private static string CompactStats(string stats)
        {
            if (string.IsNullOrWhiteSpace(stats))
            {
                return string.Empty;
            }

            string[] blocks = stats.Split(new[] { "\n\n" }, StringSplitOptions.None);
            for (int blockIndex = 0; blockIndex < blocks.Length; blockIndex++)
            {
                string[] lines = blocks[blockIndex].Split('\n');
                var rows = new List<string>((lines.Length + 1) / 2);
                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex += 2)
                {
                    rows.Add(lineIndex + 1 < lines.Length
                        ? $"{lines[lineIndex]}\t{lines[lineIndex + 1]}"
                        : lines[lineIndex]);
                }

                blocks[blockIndex] = string.Join("\n", rows);
            }

            return string.Join("\n\n", blocks);
        }

        public static string Combine(string stats, string description)
        {
            if (string.IsNullOrWhiteSpace(stats))
            {
                return description ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                return stats;
            }

            return $"{stats}\n\n{description}";
        }

        /// <summary>
        /// MagicBook 표는 마법의 snake_case 이름 하나로만 키를 잡는다.
        /// 모든 마법이 설명을 갖는 것은 아니므로 없으면 빈 문자열이다.
        /// </summary>
        private static async Task<string> GetDescriptionAsync(CombinedMagicData data)
        {
            string key = data.textLocalizationKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string description = await LocaleUtils.GetStringAsync("MagicBook", key);
            return !string.IsNullOrWhiteSpace(description) && description != key
                ? description
                : string.Empty;
        }
    }
}
