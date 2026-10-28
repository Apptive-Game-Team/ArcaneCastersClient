using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    // 확정 입력을 좌측/우측, 각 배치의 원래 순서로 실행한다. 표시용 float는 경유하지 않는다.
    public sealed class LockstepBattleDriver {
        private readonly ConfirmedInputStream stream;
        public BattleSessionRuntime Runtime { get; }
        public bool Failed { get; private set; }
        public event Action<long, InputResponse> InputHandled;
        public LockstepBattleDriver(BattleSessionRuntime runtime, bool modelProtocol = false) {
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            if (runtime.Context.FrameNumber != 0) throw new InvalidOperationException("Lockstep requires a fresh battle runtime.");
            if (runtime.Definition.LeftUserId == runtime.Definition.RightUserId) throw new InvalidOperationException("Lockstep participants must be distinct.");
            stream = new ConfirmedInputStream(runtime.Definition.SessionId, modelProtocol);
            if (modelProtocol) runtime.Models.Enable();
        }
        public void Receive(ConfirmedFrame confirmed) {
            if (Failed) throw new InvalidOperationException("Lockstep battle has failed.");
            if (Runtime.IsTerminated && !Runtime.Loop.StopRequested) throw new InvalidOperationException("Lockstep battle has terminated.");
            try {
                stream.Enqueue(confirmed);
                while (!Runtime.Loop.StopRequested && stream.TryTake(out var frame)) {
                    // 사람 입력을 실행하기 전에 이 확정 프레임의 모델 manifest를 재구성해 검증하고 결과를 전달한다.
                    Runtime.Models.Receive(frame);
                    Exception inputFailure = null;
                    if (!Runtime.Context.SubmitAction("lockstepInputs", context => {
                        try {
                            for (int slot = 0; slot < 2; slot++) {
                                long user = slot == 0 ? Runtime.Definition.LeftUserId : Runtime.Definition.RightUserId;
                                for (int i = 0; i < frame.inputs[slot].Length; i++) {
                                    var command = frame.inputs[slot][i];
                                    if (command.kind == "emote") { Runtime.Emotes.ReceivePlayer(user, command.emote); continue; }
                                    if (command.kind == "selectCard") { context.SelectCard(user, command.magicId); continue; }
                                    if (command.kind == "unselectCard") { context.UnselectCard(user, command.magicId); continue; }
                                    int requestId = LockstepLocalInputs.SimulationRequestId(frame.tick, slot, i);
                                    var position = new Vector3(Fix64.FromRaw(command.xRaw), 0, Fix64.FromRaw(command.zRaw));
                                    var response = context.InputHandler.HandleInput(context, user, command.magicId, position, requestId);
                                    InputHandled?.Invoke(user, response);
                                }
                            }
                        } catch (Exception error) { inputFailure = error; }
                    })) throw new InvalidOperationException("Lockstep input queue is full.");
                    Runtime.AdvanceConfirmedFrame(checked((int)frame.tick + 1));
                    if (inputFailure != null) throw new InvalidOperationException("Lockstep input execution failed.", inputFailure);
                }
            } catch (Exception error) { Failed = true; Runtime.Fail(error); throw; }
        }
    }
}
