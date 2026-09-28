using GameScene.Dto;
using Global.Serialization;

namespace GameScene.Handler
{
    public class GeneralHandler : IFrameInfoHandler<string>
    {
        private readonly DeltaFrameHandler deltaFrameHandler = new DeltaFrameHandler();
        private readonly ResultHandler resultHandler = new ResultHandler();
        private readonly SyncFrameHandler syncFrameHandler = new SyncFrameHandler();
        private readonly MagicValidHandler magicValidHandler = new MagicValidHandler();
        private readonly BotThoughtHandler botThoughtHandler = new BotThoughtHandler();
        private readonly PveScriptEventHandler pveScriptEventHandler = new PveScriptEventHandler();
        private readonly EmoteHandler emoteHandler = new EmoteHandler();

        public void Handler(string json)
        {
            if (!JsonCodec.TryDeserialize(json, out ServerMessage message))
            {
                HandleUntypedPveScriptEvent(json);
                return;
            }

            switch (message)
            {
                case FrameInfoDto frame:
                    deltaFrameHandler.Handler(frame);
                    break;
                case SyncFrameInfo sync:
                    syncFrameHandler.Handler(sync);
                    break;
                case MagicValidInfo magicValid:
                    magicValidHandler.Handler(magicValid);
                    break;
                case ResultInfo result:
                    resultHandler.Handler(result);
                    break;
                case BotThoughtInfo botThought:
                    botThoughtHandler.Handler(botThought);
                    break;
                case PveScriptEventInfo pveScriptEvent:
                    pveScriptEventHandler.Handler(pveScriptEvent);
                    break;
                case EmoteInfo emote:
                    emoteHandler.Handler(emote);
                    break;
            }
        }

        /// <summary>
        /// PVE 스크립트 이벤트는 <c>type</c> 없이 오는 경우가 있어 마지막 수단으로 형태만 보고 처리한다.
        /// </summary>
        private void HandleUntypedPveScriptEvent(string json)
        {
            if (!JsonCodec.TryDeserialize(json, out PveScriptEventInfo payload))
            {
                return;
            }

            bool hasKey = !string.IsNullOrWhiteSpace(payload.key);
            bool hasLines = payload.lines != null && payload.lines.Count > 0;

            if (!hasKey && !hasLines)
            {
                return;
            }

            pveScriptEventHandler.Handler(payload);
        }
    }
}
