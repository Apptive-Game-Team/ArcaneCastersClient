using GameScene.Dto;
using GameScene.Object;
using GameScene.UI;

namespace GameScene.Handler
{
    public class DeltaFrameHandler : IFrameInfoHandler<FrameInfoDto>
    {

        public void Handler(FrameInfoDto data)
        {
            if (data == null)
            {
                return;
            }

            // 마나 UI 업데이트
            GameSceneUIController.Instance.UpdateMana(data.updatedMana);

            // 카드 추가
            if (data.cards?.added != null)
            {
                foreach (long magicId in data.cards.added)
                {
                    GameSceneUIController.Instance.AddCard(magicId);
                }
            }

            PresentationFramePlayer.Apply(data.objects, data.events);

            TimerController.Instance.UpdateTimer(data.remainingTime);
        }
    }
}
