using System;
using System.Collections.Generic;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum Element { None, Fire, Water, Nature, Lightning, Rock, Wind }

    public static class ElementalChart {
        // 서버 elemental_chart.rs의 공격 행 / 방어 열. 반 배율을 정수로 저장한다.
        private static readonly byte[,] HalfMultipliers = {
            { 2, 2, 2, 2, 2, 2, 2 }, { 2, 2, 1, 4, 2, 2, 2 },
            { 2, 4, 2, 1, 2, 2, 2 }, { 2, 1, 4, 2, 4, 1, 2 },
            { 2, 2, 2, 2, 2, 2, 2 }, { 2, 1, 3, 3, 1, 3, 1 },
            { 2, 2, 2, 2, 4, 4, 2 }
        };

        public static Fix64 Multiplier(Element attacker, Element defender) {
            if ((uint)attacker >= 7 || (uint)defender >= 7) throw new ArgumentOutOfRangeException();
            return (Fix64)HalfMultipliers[(int)attacker, (int)defender] / 2;
        }

        public static Fix64 Average(IReadOnlyList<Element> attack, IReadOnlyList<Element> defense) {
            if (attack == null || defense == null) throw new ArgumentNullException();
            int attacks = Math.Max(1, attack.Count), defenses = Math.Max(1, defense.Count);
            Fix64 sum = Fix64.Zero;
            for (int a = 0; a < attacks; a++)
                for (int d = 0; d < defenses; d++)
                    sum += Multiplier(attack.Count == 0 ? Element.None : attack[a], defense.Count == 0 ? Element.None : defense[d]);
            return sum / checked(attacks * defenses);
        }
    }

    public readonly struct Hit {
        public readonly int Damage;
        public readonly int AttackerId;
        private readonly Element[] elements;
        public IReadOnlyList<Element> Elements => Array.AsReadOnly(elements ?? Array.Empty<Element>());
        public Hit(int damage, int attackerId, params Element[] elements) {
            Damage = damage; AttackerId = attackerId;
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            // 원본 AttackInfo의 BTreeSet처럼 중복 제거 후 enum 순서를 유지한다.
            var distinct = new SortedSet<Element>(elements);
            foreach (var element in distinct)
                if ((uint)element >= 7) throw new ArgumentOutOfRangeException(nameof(elements));
            this.elements = new Element[distinct.Count]; distinct.CopyTo(this.elements);
        }
    }

    public sealed class CombatHealth {
        private readonly Element[] elements;
        public int Hp { get; private set; }
        public int MaxHp { get; }
        public bool Dying { get; private set; }
        // 등록 순서는 시뮬레이션의 컴포넌트 순서다. 면역은 방어막 소비보다 먼저 검사한다.
        public readonly List<Func<Hit, bool>> Immunities = new List<Func<Hit, bool>>();
        public readonly List<Func<Hit, bool>> Interceptors = new List<Func<Hit, bool>>();
        public readonly List<Action<Element>> ElementReactions = new List<Action<Element>>();
        public event Action<int> HitBy;
        public event Action Died;

        public CombatHealth(int maxHp, params Element[] elements) {
            if (maxHp <= 0) throw new ArgumentOutOfRangeException(nameof(maxHp));
            MaxHp = maxHp; Hp = maxHp;
            this.elements = new ElementalSet(elements).Values;
        }

        private bool Immune(Hit hit) {
            foreach (var immunity in Immunities.ToArray()) if (immunity(hit)) return true;
            return false;
        }

        public void OnDamaged(Hit hit) {
            if (Dying || Immune(hit)) return;
            foreach (var interceptor in Interceptors.ToArray()) if (interceptor(hit)) return;
            foreach (var reaction in ElementReactions.ToArray())
                foreach (var element in hit.Elements) reaction(element);
            ApplyDamage(hit);
        }

        // DOT와 낙하 피해는 방어막/속성 반응을 거치지 않지만 면역은 적용한다.
        public void ApplyDamage(Hit hit) {
            if (Dying || Immune(hit)) return;
            if (hit.AttackerId != 0 && hit.Damage > 0) HitBy?.Invoke(hit.AttackerId);
            Fix64 remaining = (Fix64)Hp - (Fix64)hit.Damage * ElementalChart.Average(hit.Elements, elements);
            // 원본의 정수 복합 대입처럼 피해가 아닌 남은 HP를 0 방향으로 절삭한다.
            long whole = remaining.RawValue / (1L << 32);
            Hp = (int)Math.Max(int.MinValue, Math.Min(MaxHp, whole));
            if (Hp <= 0) { Dying = true; Died?.Invoke(); }
        }

        private sealed class ElementalSet {
            public readonly Element[] Values;
            public ElementalSet(Element[] source) {
                var hit = new Hit(0, 0, source);
                Values = new Element[hit.Elements.Count];
                for (int i = 0; i < Values.Length; i++) Values[i] = hit.Elements[i];
            }
        }
    }

    public static class CombatRange {
        public static Fix64 HorizontalEdgeDistance(Vector3 source, Fix64 sourceRadius, Vector3 target, Fix64 targetRadius) {
            Fix64 x = source.X - target.X, z = source.Z - target.Z;
            return MathHelper.Max(Fix64.Zero, Fix64.Sqrt(x * x + z * z) - sourceRadius - targetRadius);
        }
        public static bool Contains(Vector3 source, Fix64 sourceRadius, Vector3 target, Fix64 targetRadius, Fix64 range) {
            if (range < Fix64.Zero || sourceRadius < Fix64.Zero || targetRadius < Fix64.Zero) return false;
            Fix64 vertical = MathHelper.Max(Fix64.Zero, Fix64.Abs(source.Y - target.Y) - sourceRadius - targetRadius);
            return vertical <= range && HorizontalEdgeDistance(source, sourceRadius, target, targetRadius) <= range;
        }
        public static int TargetMask(Vector3 position) => position.Y >= (Fix64)2 ? 2 : 1;
    }
}
