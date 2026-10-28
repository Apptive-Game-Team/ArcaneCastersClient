using System;
using System.Collections.Generic;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class PlayerData {
        public const int MaxCardCount = 6;
        public readonly ManaCharger ManaCharger;
        public int Mana, Hp;
        public readonly List<long> Cards = new List<long>();
        public PlayerData(ManaCharger manaCharger) { ManaCharger = manaCharger ?? throw new ArgumentNullException(nameof(manaCharger)); }
        public void AddMana(int delta, int max) { Mana = Math.Min(checked(Mana + delta), max); }
        public bool SpendMana(int cost) { if (Mana < cost) return false; Mana -= cost; return true; }
        public bool AddCard(long magicId) { if (Cards.Count >= MaxCardCount) return false; Cards.Add(magicId); return true; }
        public bool ValidCardUse(long magicId, int cost) => Cards.Contains(magicId) && cost <= Mana;
        public bool UseCard(long magicId, int cost) {
            if (!ValidCardUse(magicId, cost)) return false;
            Cards.RemoveAt(Cards.IndexOf(magicId)); Mana -= cost; return true;
        }
    }

    public sealed class ManaCharger {
        private static readonly Fix64 Interval = (Fix64)0.25m;
        private readonly IntervalTimer timer = new IntervalTimer();
        private Fix64 pendingMana;
        public readonly Stat ChargeValue = new Stat(Fix64.One);
        public int MaxMana { get; }
        public Fix64 PendingMana => pendingMana;
        public Fix64 ElapsedSeconds => timer.ElapsedSeconds;
        public ManaCharger(int maxMana) { if (maxMana <= 0) throw new ArgumentOutOfRangeException(nameof(maxMana)); MaxMana = maxMana; }
        public void UpdateManaCharge(Fix64 delta) { ChargeValue.ModifierPercent += delta; }
        public void ChargeMana(PlayerData player, Fix64 deltaTime) {
            int intervals = timer.Advance(deltaTime, Interval);
            for (int i = 0; i < intervals; i++) {
                if (player.Mana >= MaxMana) { pendingMana = Fix64.Zero; continue; }
                pendingMana += ChargeValue.Total;
                int amount = (int)Fix64.Floor(pendingMana + IntervalTimer.EpsilonSeconds);
                int capacity = MaxMana - player.Mana;
                if (amount >= capacity) { player.AddMana(capacity, MaxMana); pendingMana = Fix64.Zero; }
                else if (amount > 0) {
                    player.AddMana(amount, MaxMana); pendingMana -= (Fix64)amount;
                    if (pendingMana < IntervalTimer.EpsilonSeconds) pendingMana = Fix64.Zero;
                }
            }
        }
    }

    public sealed class CardDeck {
        private readonly Stat interval = new Stat(Fix64.One);
        private readonly IntervalTimer timer = new IntervalTimer();
        private readonly Queue<long> cards;
        public Fix64 ElapsedSeconds => timer.ElapsedSeconds;
        public long[] RemainingCards => cards.ToArray();
        public CardDeck(long[] source, long shuffleSeed) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var shuffled = (long[])source.Clone(); new JavaRandom(shuffleSeed).Shuffle(shuffled);
            cards = new Queue<long>(shuffled);
        }
        public void ReturnCard(long magicId) { cards.Enqueue(magicId); }
        public void SetCards(IEnumerable<long> source) { cards.Clear(); foreach (long card in source) cards.Enqueue(card); }
        public void Fever() { interval.ModifierPercent -= (Fix64)0.5m; }
        public void DrawCard(PlayerData player, ICollection<long> drawn, Fix64 deltaTime) {
            int draws = timer.Advance(deltaTime, interval.Total);
            for (int i = 0; i < draws; i++) {
                if (cards.Count == 0 || player.Cards.Count >= PlayerData.MaxCardCount) continue;
                long card = cards.Dequeue(); if (player.AddCard(card)) drawn?.Add(card);
            }
        }
    }
}
