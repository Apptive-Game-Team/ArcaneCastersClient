using GameScene.Dto;
using Global;
using Global.Sound.BGM;
using UnityEngine;

namespace GameScene.Handler
{
    public class PveStateHandler : IFrameInfoHandler<PveStateInfo>
    {
        private const string BgmChannel = "bgm";

        private readonly PveSyncState syncState;

        public PveStateHandler(PveSyncState syncState)
        {
            this.syncState = syncState ?? new PveSyncState();
        }

        public void Handler(PveStateInfo info)
        {
            if (info == null)
            {
                return;
            }

            Apply(info.channel, info.value, info.seq);
        }

        private void Apply(string channel, string value, int seq)
        {
            // Unknown channels are ignored without recording them.
            if (channel != BgmChannel)
            {
                return;
            }

            if (BGMPlayer.Instance == null || !syncState.ShouldApplyState(channel, seq))
            {
                return;
            }

            if (string.IsNullOrEmpty(value))
            {
                BGMPlayer.Instance.RestoreSceneDefault();
                return;
            }

            BGMClipContainer container = UnityEngine.Object.FindObjectOfType<BGMClipContainer>();
            AudioClip clip = container != null ? container.GetClip(value) : null;
            if (clip == null)
            {
                WDebug.LogWarning($"[PveState] bgm key '{value}' has no clip in BGMClipContainer; keeping the current track.");
                return;
            }

            BGMPlayer.Instance.PlayOverride(clip);
        }
    }
}
