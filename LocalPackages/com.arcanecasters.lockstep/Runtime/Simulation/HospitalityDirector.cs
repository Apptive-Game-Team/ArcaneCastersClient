using System;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public sealed class HospitalityDirector {
        public static readonly Fix64 Full = -Fix64.One, Eased = (Fix64)(-0.2m), Partial = (Fix64)(-0.6m);
        public static readonly Fix64 ComfortablePressure = (Fix64)0.6m, DefaultBoardShare = (Fix64)0.5m;
        public const int ComfortableLead = 2;
        private readonly Fix64 boardShare;
        public HospitalityDirector(Fix64 boardShare) { this.boardShare = MathHelper.Clamp(boardShare, DefaultBoardShare, Fix64.One); }
        public int SummonAllowance(int enemyBoardMana, int ownBoardMana) {
            var scaled = enemyBoardMana * boardShare;
            const long unit = 1L << 32;
            long whole = scaled.RawValue / unit, remainder = scaled.RawValue % unit;
            // 비율의 Q32.32 양자화 오차 때문에 100*0.7이 69로 잘리지 않도록 정수 경계만 보정한다.
            long tolerance = Math.Abs((long)enemyBoardMana);
            if (remainder > 0 && unit - remainder <= tolerance) whole++;
            else if (remainder < 0 && unit + remainder <= tolerance) whole--;
            return checked((int)whole - ownBoardMana);
        }
        public Fix64 Aggression(int playerUnitCount, int botUnitCount, Fix64 pressure) {
            int lead = playerUnitCount - botUnitCount;
            if (lead <= 0) return Full;
            return lead >= ComfortableLead || pressure >= ComfortablePressure ? Eased : Partial;
        }
    }
}
