using System;
using System.Linq;

namespace ArcaneCasters.Simulation {
    public sealed class CardSelectVisualizer {
        private readonly IMagicLookup parser;
        private readonly bool effectsEnabled;
        public CardSelectVisualizer(IMagicLookup parser, bool effectsEnabled = true) { this.parser = parser; this.effectsEnabled = effectsEnabled; }
        private static SimObject Player(SimulationContext context, long userId) {
            return context.FindPlayerByUser(userId) ?? throw new InvalidOperationException("Player not found: " + userId);
        }
        private Element ElementOf(long magicId) => parser?.GetMagic(magicId)?.Element ?? Element.None;
        private static CardSelectedStatusEffect Marker(SimObject player) => player.GetComponents<CardSelectedStatusEffect>().Concat(player.GetPendingComponents<CardSelectedStatusEffect>()).FirstOrDefault(e => !e.IsExpired);
        private static IdleAuraStatusEffect[] Auras(SimObject player) => player.GetComponents<IdleAuraStatusEffect>().Concat(player.GetPendingComponents<IdleAuraStatusEffect>()).ToArray();
        public void SelectCard(SimulationContext context, long userId, long magicId) {
            var player = Player(context, userId); if (!effectsEnabled) return; var element = ElementOf(magicId);
            if (element != Element.None && !Auras(player).Any(e => e.Element == element)) player.AddComponent(new IdleAuraStatusEffect(player, element));
            var marker = Marker(player); if (marker != null) marker.Select(); else player.AddComponent(new CardSelectedStatusEffect(player));
        }
        public void UnselectCard(SimulationContext context, long userId, long magicId) {
            var player = Player(context, userId); var element = ElementOf(magicId);
            if (element != Element.None) foreach (var aura in Auras(player).Where(e => e.Element == element)) aura.Cancel();
            Marker(player)?.Unselect();
        }
    }
}
