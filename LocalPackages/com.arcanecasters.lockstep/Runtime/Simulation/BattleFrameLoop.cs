using System;

namespace ArcaneCasters.Simulation {
    public interface IBattleFrameOutput {
        void BeginFrame(SimulationContext context);
        void BuildSnapshot(SimulationContext context);
        void EndFrame(SimulationContext context);
    }
    public sealed class BattleFrameSystems {
        public IGameSystem Actions { get; set; } = new GameActionSystem();
        public IGameSystem Economy { get; set; } = new BattleEconomySystem();
        public IGameSystem Bots { get; set; }
        public IGameSystem Fever { get; set; }
        public IGameSystem ResetStatus { get; set; } = new GameObjectStateInitialSystem();
        public IGameSystem Components { get; set; } = new ComponentUpdateSystem();
        public IGameSystem Physics { get; set; } = new BattlePhysicsSystem();
        public IGameSystem FlushObjects { get; set; } = new GameObjectAddRemoveSystem();
        public IBattleFrameOutput Output { get; set; }
    }
    public sealed class BattleFrameLoop {
        private readonly SimulationContext context;
        private readonly BattleFrameSystems systems;
        private readonly Func<bool> timedOut;
        private int? endDetectedFrame;
        private bool gameEndHandled;
        public bool HasTimeLimit { get; set; } = true;
        public bool StopRequested { get; private set; }
        public Action<SimulationContext> BeforeResultCheck { get; set; }
        // 영속 결과 저장은 서버 경로가 담당하며, 여기서는 결정된 전투 결과만 전달한다.
        public Action<SimulationContext> HandleGameEnd { get; set; }
        public event Action<SimulationContext> GameEnded;
        public BattleFrameLoop(SimulationContext context, BattleFrameSystems systems, Func<bool> timedOut) {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.systems = systems ?? throw new ArgumentNullException(nameof(systems));
            this.timedOut = timedOut ?? throw new ArgumentNullException(nameof(timedOut));
        }
        public static int SyncIntervalFrames(int tickRate) {
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));
            return Math.Max(1, tickRate / 2 + tickRate % 2);
        }
        public static bool IsSyncFrame(int frame, int tickRate) {
            int interval = SyncIntervalFrames(tickRate);
            return frame == 1 || frame % interval == 0;
        }
        // 네트워크가 확정한 프레임마다 호출한다. 실시간 경과 시간으로 보충 tick을 만들지 않는다.
        public void Update() {
            systems.Actions.Update(context);
            systems.Economy?.Update(context);
            systems.Output?.BeginFrame(context);
            systems.Fever?.Update(context);
            if (!endDetectedFrame.HasValue && !context.Result.Ended) systems.Bots?.Update(context);
            BeforeResultCheck?.Invoke(context);
            if (HasTimeLimit && !endDetectedFrame.HasValue && timedOut()) {
                if (!context.Result.Loser.HasValue) {
                    int left = context.Players[Master.LeftPlayer].Hp, right = context.Players[Master.RightPlayer].Hp;
                    if (left != right) context.Result.SetLoser(left < right ? Master.LeftPlayer : Master.RightPlayer);
                }
                context.Result.SetEnd();
            }
            if (context.Result.Ended) {
                if (!endDetectedFrame.HasValue) endDetectedFrame = context.FrameNumber;
                if (!gameEndHandled && context.FrameNumber - endDetectedFrame.Value >= context.TickRate) {
                    gameEndHandled = true;
                    GameEnded?.Invoke(context);
                    HandleGameEnd?.Invoke(context);
                    StopRequested = true;
                }
            }
            systems.ResetStatus.Update(context);
            systems.Components.Update(context);
            systems.Physics.Update(context);
            systems.FlushObjects.Update(context);
            if (IsSyncFrame(context.FrameNumber, context.TickRate)) systems.Output?.BuildSnapshot(context);
            systems.Output?.EndFrame(context);
        }
    }
}
