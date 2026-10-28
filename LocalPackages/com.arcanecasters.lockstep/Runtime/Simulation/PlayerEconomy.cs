using System;
using System.Collections.Generic;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class JavaRandom {
        private const long Mask = (1L << 48) - 1;
        public long State { get; private set; }
        public bool HasGaussianSpare { get; private set; }
        public Fix64 GaussianSpare { get; private set; }
        public JavaRandom(long seed) { State = (seed ^ 0x5DEECE66DL) & Mask; }
        private int Next(int bits) {
            State = unchecked(State * 0x5DEECE66DL + 11) & Mask;
            return unchecked((int)((ulong)State >> (48 - bits)));
        }
        public int NextInt() => Next(32);
        // 하위 32비트도 Java의 signed int로 더해야 같은 nextLong 수열이 된다.
        public long NextLong() => unchecked(((long)Next(32) << 32) + Next(32));
        public bool NextBoolean() => Next(1) != 0;
        public int NextRange(int origin, int bound) {
            if (origin >= bound) throw new ArgumentOutOfRangeException(nameof(bound));
            int value = NextInt(), width = unchecked(bound - origin), mask = unchecked(width - 1);
            if ((width & mask) == 0) return unchecked((value & mask) + origin);
            if (width > 0) {
                int bits = (int)((uint)value >> 1), result = bits % width;
                while (unchecked(bits + mask - result) < 0) { bits = (int)((uint)NextInt() >> 1); result = bits % width; }
                return unchecked(result + origin);
            }
            while (value < origin || value >= bound) value = NextInt();
            return value;
        }
        public long NextLongBound(long bound) {
            if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound));
            long value = NextLong(), mask = bound - 1;
            if ((bound & mask) == 0) return value & mask;
            long bits = (long)((ulong)value >> 1), result = bits % bound;
            while (unchecked(bits + mask - result) < 0) { bits = (long)((ulong)NextLong() >> 1); result = bits % bound; }
            return result;
        }
        private Fix64 NextFixedRange(Fix64 origin, Fix64 bound, bool singlePrecisionSource) {
            if (origin >= bound) throw new ArgumentOutOfRangeException(nameof(bound));
            var width = Fix64.FromRaw(checked(bound.RawValue - origin.RawValue));
            var result = (singlePrecisionSource ? NextFloatFraction() : NextFraction()) * width + origin;
            return result < bound ? result : Fix64.FromRaw(bound.RawValue - 1);
        }
        public Fix64 NextFractionRange(Fix64 origin, Fix64 bound) => NextFixedRange(origin, bound, false);
        public Fix64 NextFloatRange(Fix64 origin, Fix64 bound) => NextFixedRange(origin, bound, true);
        public Fix64 NextFloatBound(Fix64 bound) => NextFloatRange(Fix64.Zero, bound);
        public Fix64 NextGaussian() {
            if (HasGaussianSpare) { HasGaussianSpare = false; return GaussianSpare; }
            while (true) {
                var first = 2 * NextFraction() - Fix64.One; var second = 2 * NextFraction() - Fix64.One;
                var square = first * first + second * second;
                if (square >= Fix64.One || square <= Fix64.Zero) continue;
                // 작은 square에서 나눗셈 중간값이 Q32.32 범위를 넘지 않도록 제곱근을 먼저 구한다.
                var scale = Fix64.Sqrt(-2 * Fix64.Ln(square)) / Fix64.Sqrt(square);
                GaussianSpare = second * scale; HasGaussianSpare = true; return first * scale;
            }
        }
        public int NextBound(int bound) {
            if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound));
            int value = Next(31), mask = bound - 1;
            if ((bound & mask) == 0) return (int)(((long)bound * value) >> 31);
            int result = value % bound;
            while (unchecked(value - result + mask) < 0) { value = Next(31); result = value % bound; }
            return result;
        }
        public void Shuffle<T>(IList<T> values) {
            for (int count = values.Count; count > 1; count--) {
                int index = NextBound(count);
                T previous = values[count - 1]; values[count - 1] = values[index]; values[index] = previous;
            }
        }
        // Java nextDouble의 두 상태 전이를 유지하고 53비트 분수를 Q32.32로 내린다.
        public Fix64 NextFraction() => Fix64.FromRaw((((long)Next(26) << 27) + Next(27)) >> 21);
        // Java nextFloat는 한 번 전이한 상위 24비트를 사용한다.
        public Fix64 NextFloatFraction() => Fix64.FromRaw((long)Next(24) << 8);
    }

    public sealed class Stat {
        public readonly Fix64 Original;
        public Fix64 ModifierPercent;
        private readonly List<KeyValuePair<int, Fix64>> multipliers = new List<KeyValuePair<int, Fix64>>();
        public Stat(Fix64 original) { Original = original; }
        public void SetMultiplier(int source, Fix64 multiplier) {
            int index = multipliers.FindIndex(pair => pair.Key == source);
            var entry = new KeyValuePair<int, Fix64>(source, multiplier);
            if (index < 0) multipliers.Add(entry); else multipliers[index] = entry;
        }
        public void RemoveMultiplier(int source) { multipliers.RemoveAll(pair => pair.Key == source); }
        public Fix64 Total {
            get {
                Fix64 value = Original * (Fix64.One + ModifierPercent);
                foreach (var multiplier in multipliers) value *= multiplier.Value;
                return value;
            }
        }
    }

    public sealed class PlayerEconomy {
        public const int TickRate = 20;
        public const int MaxHand = 6;
        private readonly CardDeck deck;
        public PlayerData Data { get; }
        private bool fever;
        public Stat ManaCharge => Data.ManaCharger.ChargeValue;
        public int Mana => Data.Mana;
        public int MaxMana => Data.ManaCharger.MaxMana;
        public IReadOnlyList<long> Hand => Data.Cards;
        public int ChargeTicks => (int)(Data.ManaCharger.ElapsedSeconds * TickRate);
        public int DrawTicks => (int)(deck.ElapsedSeconds * TickRate);
        public long PendingManaRaw => Data.ManaCharger.PendingMana.RawValue;
        public long[] RemainingDeck => deck.RemainingCards;

        public PlayerEconomy(long[] cards, long shuffleSeed, int maxMana) {
            if (cards == null || cards.Length > 1024 || maxMana <= 0) throw new ArgumentException("Invalid economy configuration");
            deck = new CardDeck(cards, shuffleSeed);
            Data = new PlayerData(new ManaCharger(maxMana));
        }

        public void BeginFever() {
            if (fever) return;
            fever = true;
            ManaCharge.ModifierPercent += Fix64.One;
            deck.Fever();
        }

        public void Tick() {
            Data.ManaCharger.ChargeMana(Data, PhysicsWorld.StepDuration);
            deck.DrawCard(Data, null, PhysicsWorld.StepDuration);
        }

        public bool UseCard(long magicId, int cost) {
            if (cost < 0 || !Data.UseCard(magicId, cost)) return false;
            deck.ReturnCard(magicId);
            return true;
        }
    }
}
