using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Localization;

namespace Data.Magic
{
    /// <summary>
    /// 도감과 덱 화면이 함께 쓰는 종류·마나 필터 드롭다운 문구. 순서는
    /// <see cref="MagicCastKinds.FilterOptions"/>, <see cref="ManaBands.FilterOptions"/> 와 같다.
    /// </summary>
    public static class MagicFilterLabels
    {
        private const string MagicBookTable = "MagicBook";

        public static async Task<List<string>> CastKindLabels()
        {
            var labels = new List<string>(MagicCastKinds.FilterOptions.Length);
            foreach (MagicCastKind? kind in MagicCastKinds.FilterOptions)
            {
                labels.Add(kind.HasValue
                    ? await GetText(MagicCastKinds.LocalizationKey(kind.Value), MagicCastKinds.FallbackLabel(kind.Value))
                    : await GetText("filter.kind.all", "모든 종류"));
            }

            return labels;
        }

        public static async Task<List<string>> ManaBandLabels()
        {
            string mana = await GetText("filter.manaCost", "마나");
            var labels = new List<string>(ManaBands.FilterOptions.Length);
            foreach (ManaBand? band in ManaBands.FilterOptions)
            {
                labels.Add(band.HasValue
                    ? $"{mana} {ManaBands.Label(band.Value)}"
                    : await GetText("filter.mana.all", "모든 마나"));
            }

            return labels;
        }

        private static async Task<string> GetText(string key, string fallback)
        {
            string text = await LocaleUtils.GetStringAsync(MagicBookTable, key);
            return string.IsNullOrWhiteSpace(text) || text == key ? fallback : text;
        }
    }
}
