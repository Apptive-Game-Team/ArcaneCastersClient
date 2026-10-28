using ArcaneCasters.Simulation;

namespace ArcaneCasters.Parity {
    public static class LocalInputChecks {
        public static void Run() {
            var mixed = new LockstepLocalInputs(); Assert.True(mixed.QueueSelection(7, true)); Assert.True(mixed.QueueSelection(7, false)); Assert.True(mixed.Queue(7, 1, 2, 800));
            var mixedBatch = mixed.Prepare(1, 1); Assert.Equal("selectCard", mixedBatch.commands[0].kind); Assert.Equal("unselectCard", mixedBatch.commands[1].kind); Assert.Equal("cast", mixedBatch.commands[2].kind);
            Assert.True(mixed.TryAcknowledge(new InputResponse(true, InputResultCode.Success, "ok", 0, LockstepLocalInputs.SimulationRequestId(1, 1, 2)), out var mixedAck)); Assert.Equal(800, mixedAck.Id);
            var local = new LockstepLocalInputs();
            Assert.True(local.Queue(5, 1, 2, 700)); Assert.True(!local.Queue(6, 1, 2, 700));
            var sent = local.Prepare(3, 1); Assert.Equal(1, sent.commands.Length); Assert.Equal(5L, sent.commands[0].magicId);
            Assert.True(local.Queue(6, 2, 3, 701)); Assert.True(object.ReferenceEquals(sent, local.Prepare(3, 1)));
            Assert.True(local.Prepare(2, 1) == null);
            var response = new InputResponse(true, InputResultCode.Success, "ok", 17, LockstepLocalInputs.SimulationRequestId(3, 1, 0), 5);
            Assert.True(local.TryAcknowledge(response, out var ack)); Assert.Equal(700, ack.Id); Assert.Equal(5L, ack.MagicId); Assert.Equal(17, ack.UpdatedMana);
            Assert.True(!local.TryAcknowledge(response, out _));
            var next = local.Prepare(4, 1); Assert.Equal(1, next.commands.Length); Assert.Equal(6L, next.commands[0].magicId);
            // 이미 보낸 배치를 다시 실행해도 응답은 한 번만 화면으로 전달한다.
            Assert.True(!local.TryAcknowledge(response, out _));
            var rejected = new InputResponse(false, InputResultCode.FailInvalidPlace, "invalid", 17, LockstepLocalInputs.SimulationRequestId(4, 1, 0), -1);
            Assert.True(local.TryAcknowledge(rejected, out ack)); Assert.Equal(701, ack.Id); Assert.Equal(InputResultCode.FailInvalidPlace, ack.ResultCode); Assert.True(!ack.Valid);
            Assert.True(local.Queue(7, 0, 0, 700)); local.Clear(); Assert.Equal(0, local.Prepare(0, 0).commands.Length);
            Assert.True(!local.Queue(0, 0, 0)); Assert.True(!local.Queue(1, -1, 0)); Assert.True(!local.Queue(1, 0, (10L << 32) + 1));
            for (int i = 0; i < 16; i++) Assert.True(local.Queue(1, 0, 0, i));
            Assert.True(!local.Queue(1, 0, 0, 16)); var batch = local.Prepare(1, 0); Assert.Equal(16, batch.commands.Length);
            for (int i = 15; i >= 0; i--) {
                Assert.True(local.TryAcknowledge(new InputResponse(true, InputResultCode.Success, "ok", 0, LockstepLocalInputs.SimulationRequestId(1, 0, i)), out ack));
                Assert.Equal(i, ack.Id);
            }
        }
    }
}
