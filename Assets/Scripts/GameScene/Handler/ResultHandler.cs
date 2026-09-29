using GameScene.Dto;
using Global;

namespace GameScene.Handler
{
    public class ResultHandler : IFrameInfoHandler<ResultInfo>
    {
        public void Handler(ResultInfo data)
        {
            SceneContext.MatchResult = data;
            // 서버는 결과를 보낸 뒤 FrameInfo를 멈춘다. 결과 화면까지의 무음을 끊김으로 보지 않게 한다.
            StompConnector.Instance.NotifyMatchEnded();
            GameEndEventController.Instance.TriggerGameEnd();
        }
    }
}