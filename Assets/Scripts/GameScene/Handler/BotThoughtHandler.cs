using GameScene.Dto;
using Global;
using Global.Util;

namespace GameScene.Handler
{
    public class BotThoughtHandler : IFrameInfoHandler<BotThoughtInfo>
    {
        public void Handler(BotThoughtInfo thought)
        {
            if (thought == null || StompConnector.Instance == null ||
                StompConnector.Instance.HasMatchEnded ||
                !JwksService.IsFetched || !JwtHelper.IsAdmin(SceneContext.JwtToken))
            {
                return;
            }

            string summary = $"[BotThought:{thought.botSide}] {thought.reason} ({thought.ruleId})";
            string cards = thought.cards == null ? "" : string.Join(",", thought.cards);
            WDebug.Log($"{summary} cards=[{cards}] target={thought.target}");

            BotThoughtLog.Append(thought);
        }
    }
}
