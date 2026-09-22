using GameScene.Dto;
using GameScene.Emote;
using Global;

namespace GameScene.Handler
{
    public class EmoteHandler : IFrameInfoHandler<EmoteInfo>
    {
        public void Handler(EmoteInfo emote)
        {
            if (emote == null)
            {
                return;
            }

            if (!EmoteTypes.TryParse(emote.emote, out EmoteType parsedEmote))
            {
                WDebug.LogWarning($"[Emote] 모르는 emote 라 버린다. value: {emote.emote}");
                return;
            }

            PlayerEmoteBubblePresenter.ShowForSide(emote.side, parsedEmote);
        }
    }
}
