using UnityEngine;

namespace Global.Sound.BGM
{
    public class BGMClipContainer : MonoBehaviour
    {
        [SerializeField] private AudioClip bgmClip;
        // Boss fight track. Left unassigned until a track is supplied; while null, a boss spawn keeps the current BGM.
        [SerializeField] private AudioClip bossClip;

        public AudioClip GetBGMClip()
        {
            return bgmClip;
        }

        public AudioClip GetBossClip()
        {
            return bossClip;
        }
    }
}