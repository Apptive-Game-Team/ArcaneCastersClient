using System;
using UnityEngine;

namespace GameScene.Emote
{
    /// <summary>
    /// 서버와 주고받는 emote 5종. 멤버 이름이 곧 wire 값이지만, 직렬화는
    /// <see cref="EmoteTypes.WireName"/> 를 거친다. prefab 이 이 enum 의 정수를 저장하므로
    /// 선언 순서를 바꾸면 이미 저장된 행이 다른 멤버를 가리킨다.
    /// </summary>
    public enum EmoteType
    {
        Laugh = 0,
        Greet = 1,
        Taunt = 2,
        Cry = 3,
        Surprised = 4
    }

    public static class EmoteTypes
    {
        public static readonly EmoteType[] All =
        {
            EmoteType.Laugh,
            EmoteType.Greet,
            EmoteType.Taunt,
            EmoteType.Cry,
            EmoteType.Surprised
        };

        /// <summary>서버가 읽는 이름. enum 의 <c>ToString</c> 에 기대지 않고 여기서 한 번만 정한다.</summary>
        public static string WireName(EmoteType emote)
        {
            switch (emote)
            {
                case EmoteType.Laugh:
                    return "Laugh";
                case EmoteType.Greet:
                    return "Greet";
                case EmoteType.Taunt:
                    return "Taunt";
                case EmoteType.Cry:
                    return "Cry";
                case EmoteType.Surprised:
                    return "Surprised";
                default:
                    return null;
            }
        }

        /// <summary>
        /// 서버가 보낸 이름을 읽는다. 모르는 이름은 false 를 돌려주고 호출자가 그 한 건만 버린다.
        /// </summary>
        public static bool TryParse(string name, out EmoteType emote)
        {
            emote = EmoteType.Laugh;

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string trimmedName = name.Trim();
            foreach (EmoteType candidate in All)
            {
                if (string.Equals(WireName(candidate), trimmedName, StringComparison.OrdinalIgnoreCase))
                {
                    emote = candidate;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>emote 한 종류와 그 아이콘. prefab 에 배열로 실려 있고 순서에 기대지 않는다.</summary>
    [Serializable]
    public class EmoteSpriteEntry
    {
        public EmoteType emote;
        public Sprite sprite;
    }

    public static class EmoteSpriteEntries
    {
        public static Sprite Find(EmoteSpriteEntry[] entries, EmoteType emote)
        {
            if (entries == null)
            {
                return null;
            }

            foreach (EmoteSpriteEntry entry in entries)
            {
                if (entry != null && entry.emote == emote)
                {
                    return entry.sprite;
                }
            }

            return null;
        }
    }
}
