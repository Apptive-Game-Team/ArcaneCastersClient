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

        public AudioClip GetBGMClip()
        {
            return bgmClip;
        }

        /// <summary>The clip registered under <paramref name="key"/>, or null when the key is not in the list.</summary>
        public AudioClip GetClip(string key)
        {
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