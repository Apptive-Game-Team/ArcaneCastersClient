using System;
using System.Collections.Generic;

namespace ArcaneCasters.Simulation {
    public sealed class PlayerEconomyFrame {
        public int Mana { get; }
        public IReadOnlyList<long> DrawnCards { get; }
        public PlayerEconomyFrame(int mana, long[] drawnCards) { Mana = mana; DrawnCards = Array.AsReadOnly(drawnCards); }
    }
    public sealed class BattleEconomySystem : IGameSystem {
        public PlayerEconomyFrame Left { get; private set; }
        public PlayerEconomyFrame Right { get; private set; }
        public void Update(SimulationContext context) {
            // 원본 FrameDataSystem 순서: 양쪽 마나 충전 후 양쪽 카드를 뽑는다.
            foreach (var side in new[] { Master.LeftPlayer, Master.RightPlayer }) {
                if (context.Players.TryGetValue(side, out var player)) player.ManaCharger.ChargeMana(player, context.DeltaTime);
            }
            Left = Draw(context, Master.LeftPlayer); Right = Draw(context, Master.RightPlayer);
        }
        private static PlayerEconomyFrame Draw(SimulationContext context, Master side) {
            if (!context.Players.TryGetValue(side, out var player)) return null;
            var drawn = new List<long>();
            if (context.CardDecks.TryGetValue(side, out var deck)) deck.DrawCard(player, drawn, context.DeltaTime);
            return new PlayerEconomyFrame(player.Mana, drawn.ToArray());
        }
    }
}
