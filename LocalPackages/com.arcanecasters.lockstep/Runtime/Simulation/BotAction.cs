using System;
using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    // 같은 슬롯의 봇이 교체된 뒤 이전 모델 응답을 적용하지 않도록 세대를 전달한다.
    public delegate IBotDecisionSource BotDecisionSourceFactory(Master side, long generation, BotPersona persona, IBotDecisionSource fallback);
    public sealed class BotDecisionProtocolException : Exception {
        public BotDecisionProtocolException(string message) : base(message) { }
    }
    public interface IBotDecisionSource {
        InputDecision Think(BotEye eye, IDictionary<string, Fix64> parameters, Master side, bool overdue);
    }
    public sealed class RuleDecisionSource : IBotDecisionSource {
        public BotBrain Brain { get; }
        public RuleDecisionSource(BotBrain brain) { Brain = brain ?? throw new ArgumentNullException(nameof(brain)); }
        public InputDecision Think(BotEye eye, IDictionary<string, Fix64> parameters, Master side, bool overdue) => Brain.Think(eye, parameters, side, overdue);
    }
    public readonly struct BotInputRequest {
        public readonly int Id;
        public readonly long MagicId;
        public readonly Vector3? Position;
        public BotInputRequest(int id, long magicId, Vector3? position) { Id = id; MagicId = magicId; Position = position; }
    }
    public interface IBotInputHandler {
        void HandleBotPlayerInput(SimulationContext context, Master side, BotInputRequest request);
    }
    public sealed class BotInputHandlerService {
        public IBotInputHandler Handler { get; }
        public BotInputHandlerService(IBotInputHandler handler) { Handler = handler ?? throw new ArgumentNullException(nameof(handler)); }
    }
    public sealed class BotMagicInputAdapter : IBotInputHandler {
        private readonly MagicInputHandler handler;
        public BotMagicInputAdapter(MagicInputHandler handler) { this.handler = handler ?? throw new ArgumentNullException(nameof(handler)); }
        public void HandleBotPlayerInput(SimulationContext context, Master side, BotInputRequest request) => handler.HandleBotPlayerInput(context, side, request.MagicId, request.Position, request.Id);
    }
    public sealed class BotAction {
        public event Action<Master> MissingHandler;
        public bool UseCard(GameActionSender<SimulationContext> sender, BotInputRequest request, Master side) => sender.Submit("botUseMagic", context => {
            var service = context.Service<BotInputHandlerService>();
            if (service == null) MissingHandler?.Invoke(side);
            else service.Handler.HandleBotPlayerInput(context, side, request);
        });
    }
}
