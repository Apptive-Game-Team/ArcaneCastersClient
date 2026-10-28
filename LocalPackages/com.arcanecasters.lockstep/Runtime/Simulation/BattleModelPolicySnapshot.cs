using System;
using System.IO;

namespace ArcaneCasters.Simulation {
    // 서버 설정 중 재생에 필요한 값만 고정한다. provider 주소와 인증 정보는 포함하지 않는다.
    [Serializable] public sealed class BattleModelPolicySnapshot {
        public int slot;
        public string brainType;
        public string mode;
        public uint minIntervalTicks;
        public uint terminalTicks;
        public bool fallbackToRule;

        internal BattleModelPolicySnapshot Copy() => (BattleModelPolicySnapshot)MemberwiseClone();
        internal void Validate(BotPersona persona) {
            if (persona == null || !BotPersona.UsesLlm(persona.BrainType)
                || brainType != BotPersona.BrainTypeName(persona.BrainType)
                || (mode != "PLAY" && mode != "STANCE" && mode != "CARD")
                || (persona.BrainType == BotBrainType.Openrouter && mode != "PLAY")
                || terminalTicks == 0)
                throw new InvalidDataException("Invalid frozen model policy.");
        }
    }
}
