using System;
using System.Linq;
using GameScene.Dto;
using GameScene.Object;
using GameScene.UI;
using Global;

namespace GameScene.Handler
{
    public class SyncFrameHandler : IFrameInfoHandler<SyncFrameInfo>
    {
        private readonly GameEventHandler gameEventHandler = new GameEventHandler();

        public void Handler(SyncFrameInfo syncFrameInfo)
        {
            if (syncFrameInfo?.snapshotResponseDto == null)
            {
                WDebug.LogError("[SyncFrameHandler] syncFrameInfo is null");
                return;
            }

            // 마나 UI 업데이트
            GameSceneUIController.Instance.UpdateMana(syncFrameInfo.updatedMana);
            
            // // 카드 추가
            try
            {
                var currentCounts = GameSceneUIController.Instance.GetAllCards()
                    .GroupBy(x => x)
                    .ToDictionary(g => g.Key, g => g.Count());

                // myCards 도 added 와 같은 마법 id 목록이다.
                var targetCounts = (syncFrameInfo.snapshotResponseDto.myCards ?? Array.Empty<long>())
                    .GroupBy(x => x)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 모든 마법 id 추출 (현재 + 목표)
                var allMagicIds = currentCounts.Keys.Union(targetCounts.Keys);

                foreach (var magicId in allMagicIds)
                {
                    currentCounts.TryGetValue(magicId, out int currentCount);
                    targetCounts.TryGetValue(magicId, out int targetCount);

                    int diff = targetCount - currentCount;

                    if (diff > 0) // 추가 필요
                    {
                        for (int i = 0; i < diff; i++) GameSceneUIController.Instance.AddCard(magicId);
                    }
                    else if (diff < 0) // 삭제 필요
                    {
                        for (int i = 0; i < Math.Abs(diff); i++) GameSceneUIController.Instance.RemoveCard(magicId);
                    }
                }
            }
            catch
            {
                WDebug.Log("[SyncFrameHandler] 카드 추가 중 오류 발생");
            }
 
            if (syncFrameInfo.projectileDtos != null)
            {
                foreach (var projectile in syncFrameInfo.projectileDtos)
                {
                    ProjectileSpawner.Instance.Spawn(projectile);
                }
            }

            // 동기화가 사라진 오브젝트를 지우기 전에 이벤트를 먼저 처리한다.
            gameEventHandler.Handler(syncFrameInfo.events);

            // 동기화
            ObjectSyncer.Instance.Sync(syncFrameInfo.snapshotResponseDto.objects ?? Array.Empty<SnapshotObjectDto>());
            
            TimerController.Instance.UpdateTimer(syncFrameInfo.remainingTime);
        }
    }
}