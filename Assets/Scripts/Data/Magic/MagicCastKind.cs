using System;

namespace Data.Magic
{
    /// <summary>
    /// 카드가 필드에 무엇을 남기는지. 서버 <c>magics.cast_kind</c> 를 카드 목록에서 나눠 보는 단위로 옮긴 것이다.
    /// </summary>
    public enum MagicCastKind
    {
        Unknown,
        Unit,
        Building,
        Drop,
        Explosion,
        Shot,
    }

    public static class MagicCastKinds
    {
        /// <summary>종류 필터 드롭다운에 보이는 순서. null 은 전체다.</summary>
        public static readonly MagicCastKind?[] FilterOptions =
        {
            null,
            MagicCastKind.Unit,
            MagicCastKind.Drop,
            MagicCastKind.Explosion,
            MagicCastKind.Shot,
            MagicCastKind.Building,
        };

        public static MagicCastKind Parse(string castKind)
        {
            if (string.IsNullOrWhiteSpace(castKind))
            {
                return MagicCastKind.Unknown;
            }

            switch (castKind.Trim().ToLowerInvariant())
            {
                case "spawn": return MagicCastKind.Unit;
                case "summon": return MagicCastKind.Building;
                case "drop": return MagicCastKind.Drop;
                case "explosion": return MagicCastKind.Explosion;
                case "shot": return MagicCastKind.Shot;
                default: return MagicCastKind.Unknown;
            }
        }

        /// <summary>필드에 몸을 남기는 카드. 이런 카드는 누른 유닛 위치로 끌려가지 않고 바닥에 놓인다.</summary>
        public static bool LeavesBody(MagicCastKind kind)
        {
            return kind == MagicCastKind.Unit || kind == MagicCastKind.Building;
        }

        public static string LocalizationKey(MagicCastKind kind)
        {
            return "filter.kind." + kind.ToString().ToLowerInvariant();
        }

        public static string FallbackLabel(MagicCastKind kind)
        {
            return kind switch
            {
                MagicCastKind.Unit => "유닛",
                MagicCastKind.Building => "건물",
                MagicCastKind.Drop => "드랍",
                MagicCastKind.Explosion => "폭발",
                MagicCastKind.Shot => "슛",
                _ => kind.ToString(),
            };
        }
    }

    /// <summary>
    /// 카드 목록을 마나 구간으로 나눠 보는 단위. 구간 경계는 카드 등급별 마나 배치를 따른다
    /// (2장 10~15, 3장 20~40, 4장 40~55, 5장 70~80).
    /// </summary>
    public enum ManaBand
    {
        Low,
        Mid,
        High,
        Top,
    }

    public static class ManaBands
    {
        public static readonly ManaBand?[] FilterOptions =
        {
            null,
            ManaBand.Low,
            ManaBand.Mid,
            ManaBand.High,
            ManaBand.Top,
        };

        public static ManaBand Of(int manaCost)
        {
            if (manaCost <= 15) return ManaBand.Low;
            if (manaCost < 40) return ManaBand.Mid;
            if (manaCost < 60) return ManaBand.High;
            return ManaBand.Top;
        }

        public static bool Contains(ManaBand? band, int manaCost)
        {
            return !band.HasValue || Of(manaCost) == band.Value;
        }

        public static string Label(ManaBand band)
        {
            return band switch
            {
                ManaBand.Low => "~15",
                ManaBand.Mid => "16~39",
                ManaBand.High => "40~59",
                ManaBand.Top => "60~",
                _ => throw new ArgumentOutOfRangeException(nameof(band), band, null),
            };
        }
    }
}
