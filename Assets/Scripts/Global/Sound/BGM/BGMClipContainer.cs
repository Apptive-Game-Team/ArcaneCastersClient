using System.Collections.Generic;
using UnityEngine;

namespace Global.Sound.BGM
{
    public class BGMClipContainer : MonoBehaviour
    {
        [SerializeField] private AudioClip bgmClip;
        // Tracks the server can ask for by key (the "bgm" PVE state channel). Ships empty until tracks exist.
        [SerializeField] private List<BgmEntry> bgmEntries = new List<BgmEntry>();

        [System.Serializable]
        public struct BgmEntry
        {
            public string key;
            public AudioClip clip;
        }

        public const string BossKey = "boss";

        // Set for the current scene only, by BattleThemeApplier on an adventure match.
        private AudioClip themeBattleClip;
        private AudioClip themeBossClip;

        /// <summary>
        /// Replaces the scene default track and the <see cref="BossKey"/> track with the battle
        /// theme's own. A null argument keeps what this container already holds for that slot.
        /// </summary>
        public void SetThemeClips(AudioClip battleClip, AudioClip bossClip)
        {
            themeBattleClip = battleClip;
            themeBossClip = bossClip;
        }

        public AudioClip GetBGMClip()
        {
            return themeBattleClip != null ? themeBattleClip : bgmClip;
        }

        /// <summary>The clip registered under <paramref name="key"/>, or null when the key is not in the list.</summary>
        public AudioClip GetClip(string key)
        {
            if (key == BossKey && themeBossClip != null)
            {
                return themeBossClip;
            }

            if (bgmEntries == null)
            {
                return null;
            }

            foreach (BgmEntry entry in bgmEntries)
            {
                if (entry.key == key && entry.clip != null)
                {
                    return entry.clip;
                }
            }
            return null;
        }
    }
}