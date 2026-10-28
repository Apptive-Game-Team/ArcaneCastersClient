using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum PlayerResult { Draw, Win, Lose }
    public sealed class BattleResult {
        public Master? Loser { get; private set; }
        public bool Ended { get; private set; }
        public void SetEnd(bool value = true) { Ended = value; }
        public void SetLoser(Master loser) { Ended = true; if (!Loser.HasValue) Loser = loser; }
        public long WinnerId(long leftId, long rightId) => Loser == Master.LeftPlayer ? rightId : leftId;
        public PlayerResult ForPlayer(Master side) => !Loser.HasValue ? PlayerResult.Draw : Loser == side ? PlayerResult.Lose : PlayerResult.Win;
        public void Broadcast(long leftId, long rightId, BattleResultMmr mmr, System.Action<long, BattleResultMessage> send) {
            if (send == null) throw new System.ArgumentNullException(nameof(send));
            var result = new BattleResultMessage(Loser, mmr);
            send(rightId, result); send(leftId, result);
        }
    }
    public sealed class PlayerHealthComponent : MobComponent, IPlayerHealth, IGaugeComponent, IFrenzyImmune, IMindControlImmune {
        private readonly Master originalMaster;
        public PlayerHealthComponent(SimObject owner, int hp) : base(owner, hp, 0) { originalMaster = owner.Master; WritePlayerHp(hp); }
        private void WritePlayerHp(int hp) {
            if (originalMaster == Master.None) return;
            Context.Players[originalMaster].Hp = hp;
        }
        public override void ApplyDamage(Hit hit) { base.ApplyDamage(hit); WritePlayerHp(Hp); }
        public override void OnDeath() { Context.Result.SetLoser(Owner.Master); }
        public Gauge GetGauge() => new Gauge(Hp, MaxHp, GaugeCategory.Hp);
        public static void Register(SimulationContext context) {
            context.RegisterPrefab("Player", owner => { owner.AddCollider(new CircleCollider(owner, 1)); owner.Elements.AddNative(Element.None);
                owner.AddComponent(new PlayerHealthComponent(owner, (int)(context.GameParameters["player.hp"].RawValue / (1L << 32)))); owner.AddComponent(new PlayerStatusSetter(owner)); owner.AddComponent(new CommonEffectReceiver(owner)); });
        }
    }
}
