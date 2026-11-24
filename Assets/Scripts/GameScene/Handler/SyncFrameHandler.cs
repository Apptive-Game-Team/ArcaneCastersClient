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

        public void Handler(SyncFrameInfo syncFrameInfo) => Apply(syncFrameInfo, false);

        public bool TryApplyInitial(SyncFrameInfo sync)
        {
            if (sync?.snapshotResponseDto?.objects == null || sync.snapshotResponseDto.myCards == null ||
                GameSceneUIController.Instance == null || ObjectSyncer.Instance == null ||
                ObjectContainer.Instance == null || TimerController.Instance == null) return false;
            try
            {
                Apply(sync, true);
                var actual = GameSceneUIController.Instance.GetAllCards().OrderBy(id => id);
                var expected = sync.snapshotResponseDto.myCards.OrderBy(id => id);
                return actual.SequenceEqual(expected) && sync.snapshotResponseDto.objects.All(obj => ObjectContainer.Instance.IsExist(obj.id));
            }
            catch (System.Exception error)
            {
                WDebug.LogWarning($"[SessionStart] 초기 상태 적용 실패: {error.Message}");
                return false;
            }
        }

        private void Apply(SyncFrameInfo syncFrameInfo, bool strict)
        {
            if (syncFrameInfo?.snapshotResponseDto == null)
            {
                WDebug.LogError("[SyncFrameHandler] syncFrameInfo is null");
                return;
            }

            GameConfig.SetTickRate(syncFrameInfo.tickRate);

            Card.CardUIZoom.Instance?.ApplyServerPause(syncFrameInfo.practicePaused);
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
                if (strict) throw;
                WDebug.Log("[SyncFrameHandler] 카드 추가 중 오류 발생");
            }
 
            try
            {
                if (syncFrameInfo.projectileDtos != null)
                {
                    foreach (var projectile in syncFrameInfo.projectileDtos)
                    {
                        ProjectileSpawner.Instance.Spawn(projectile);
                    }
                }

                // 동기화가 사라진 오브젝트를 지우기 전에 이벤트를 먼저 처리한다.
                gameEventHandler.Handler(syncFrameInfo.events);
                ObjectSyncer.Instance.Sync(syncFrameInfo.snapshotResponseDto.objects ?? Array.Empty<SnapshotObjectDto>());
            }
            finally
            {
                ProjectileSpawner.EndProjectileFrame(syncFrameInfo.projectileDtos);
            }
            
            TimerController.Instance.UpdateTimer(syncFrameInfo.remainingTime);
        }
    }
}
