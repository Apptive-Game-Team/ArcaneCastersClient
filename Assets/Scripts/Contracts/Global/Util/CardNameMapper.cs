using System;
using Data;
using Data.Magic;

namespace Global.Util
{
    /// <summary>
    /// 서버가 보내는 이름 문자열에서 마법 카드와 원소를 찾는다.
    /// 카드 종류 enum 이 없어졌으므로 이름은 마법 목록에 대고 확인한다.
    /// </summary>
    public static class CardNameMapper
    {
        /// <summary>마법 이름으로 마법 카드를 찾는다. 목록이 아직 안 왔으면 false 다.</summary>
        public static bool TryMapToMagic(string name, out CombinedMagicData magic)
        {
            return LocalCombinedMagicData.TryGetByName(name, out magic);
        }

        /// <summary>원소 이름을 enum 으로 바꾼다. Fire ~ Wind 와 None 만 받는다.</summary>
        public static bool TryMapToElement(string name, out ElementType element)
        {
            return Enum.TryParse(name, true, out element) && Enum.IsDefined(typeof(ElementType), element);
        }
    }
}
