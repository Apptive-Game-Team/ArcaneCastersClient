using System;
using System.IO;

namespace ArcaneCasters.Simulation {
    public sealed class LockstepBattleSession {
        private readonly string sessionId, version;
        private readonly long memberId;
        private readonly bool spectator;
        private readonly Func<SessionStart, BattleSessionRuntime> create;
        private string initialHash;
        private long left, right;
        public LockstepBattleDriver Driver { get; private set; }
        public LockstepBattleSession(string sessionId, long memberId, string version, Func<SessionStart, BattleSessionRuntime> create, bool spectator = false) {
            this.sessionId = sessionId; this.memberId = memberId; this.version = version;
            this.create = create ?? throw new ArgumentNullException(nameof(create));
            this.spectator = spectator;
        }
        public void Initialize(SessionStart initial) {
            if (initial == null) throw new InvalidDataException("Missing lockstep initial state.");
            initial.Validate(sessionId, memberId, version, spectator);
            long nextLeft = initial.participants[0], nextRight = initial.participants[1];
            string nextHash = initial.initialHash;
            if (initialHash != null && (initialHash != nextHash || left != nextLeft || right != nextRight))
                throw new InvalidDataException("Lockstep initial state changed during recovery.");
            var runtime = create(initial);
            if (runtime == null || runtime.Definition.SessionId != sessionId || runtime.Definition.LeftUserId != nextLeft
                || runtime.Definition.RightUserId != nextRight || runtime.Context.TickRate != initial.tickRate)
                throw new InvalidDataException("Battle runtime does not match the lockstep session.");
            var driver = new LockstepBattleDriver(runtime, LockstepVersion.UsesModelProtocol(version));
            if (Driver != null && ReferenceEquals(Driver.Runtime, runtime)) throw new InvalidOperationException("Recovery requires a new battle runtime.");
            Driver?.Runtime.Close();
            Driver = driver; initialHash = nextHash; left = nextLeft; right = nextRight;
        }
        public void Receive(ConfirmedFrame frame) {
            if (Driver == null) throw new InvalidOperationException("Lockstep battle is not initialized.");
            Driver.Receive(frame);
        }
    }
}
