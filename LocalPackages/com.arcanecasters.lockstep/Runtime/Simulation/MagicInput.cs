using System;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum InputResultCode { Success, FailInsufficientMana, FailLackOfCard, FailInvalidPlace, FailInvalidMagic, FailGameEnded }
    public readonly struct InputHandleEvent {
        public readonly Master Master; public readonly InputResultCode ResultCode; public readonly long MagicId;
        public InputHandleEvent(Master master, InputResultCode code, long magicId = -1) { Master = master; ResultCode = code; MagicId = magicId; }
    }
    public readonly struct InputResponse {
        public readonly bool Valid; public readonly InputResultCode ResultCode; public readonly string Message; public readonly int UpdatedMana, Id; public readonly long MagicId;
        public InputResponse(bool valid, InputResultCode code, string message, int mana, int id, long magicId = -1) { Valid = valid; ResultCode = code; Message = message; UpdatedMana = mana; Id = id; MagicId = magicId; }
    }
    public sealed class BoundMagic : IVineFan, IBossMagic {
        public long Id { get; } public string Name { get; } public Element Element { get; }
        public string SummonedParameterKey { get; } public bool Airborne { get; } public string SummonedPrefab { get; } public bool IsSpawnMagic { get; } public bool IsVineFan { get; } public bool IsExplosionMagic { get; }
        private readonly Action<SimulationContext, Master, Vector3?, Vector3> run;
        public BoundMagic(long id, string name, Action<SimulationContext, Master, Vector3?, Vector3> run, string summonedParameterKey = null, bool airborne = false, Element element = Element.None, string summonedPrefab = null, bool isSpawnMagic = false, bool isVineFan = false, bool isExplosionMagic = false) { Id = id; Name = name; this.run = run; SummonedParameterKey = summonedParameterKey; Airborne = airborne; Element = element; SummonedPrefab = summonedPrefab; IsSpawnMagic = isSpawnMagic; IsVineFan = isVineFan; IsExplosionMagic = isExplosionMagic; }
        public void Run(SimulationContext context, Master master, Vector3? origin, Vector3 position) { run(context, master, origin, position); }
        public bool Cast(SimulationContext context, Master master, Vector3 target, Vector3 origin) {
            if (context.InputHandler == null) throw new InvalidOperationException("PVE boss requires a MagicInputHandler");
            return context.InputHandler.HandleBotMagicInput(context, master, this, target, origin).Valid;
        }
        public void Run(SimulationContext context, Master master, Vector3 position) {
            var caster = context.Objects.FirstOrDefault(owner => owner.IsActive && owner.Master == master && owner.Prefab == "Player");
            run(context, master, caster?.Position, position);
        }
        void IVineFan.Run(SimulationContext context, Master master, Vector3 origin, Vector3 position) { Run(context, master, origin, position); }
        public int SummonedQuantity(SimulationContext context) => IsSpawnMagic ? SpawnMagics.Quantity(context, SummonedParameterKey) : 1;
    }
    public interface IMagicLookup { BoundMagic ParseMagic(long userId, long magicId); BoundMagic GetMagic(long magicId); }
    public sealed class MagicInputHandler {
        private readonly IMagicLookup lookup;
        private readonly FlowPublisher<InputHandleEvent> publisher = new FlowPublisher<InputHandleEvent>();
        public MagicInputHandler(IMagicLookup lookup) { this.lookup = lookup; }
        public FlowSubscription Subscribe(Action<InputHandleEvent> listener) => publisher.Subscribe(listener);
        private InputResponse Fail(SimulationContext c, Master side, InputResultCode code, string message, bool valid, int id) {
            publisher.Publish(new InputHandleEvent(side, code)); return new InputResponse(valid, code, message, c.Players[side].Mana, id);
        }
        private InputResponse Ended(SimulationContext c, Master side, int id) => Fail(c, side, InputResultCode.FailGameEnded, "Game has ended.", false, id);
        public InputResponse HandleInput(SimulationContext c, long userId, long magicId, Vector3? position, int id) {
            var sides = c.UserIds.Where(pair => pair.Value == userId).ToArray(); if (sides.Length == 0) throw new InvalidOperationException("User does not play in this session"); var side = sides[0].Key;
            if (c.Result.Ended) return Ended(c, side, id);
            if (magicId <= 0 || !position.HasValue) return Fail(c, side, InputResultCode.FailInvalidMagic, "Invalid magic.", false, id);
            return HandleCard(c, side, magicId, position, id, () => lookup.ParseMagic(userId, magicId));
        }
        public InputResponse HandleBotPlayerInput(SimulationContext c, Master side, long magicId, Vector3? position, int id) {
            if (c.Result.Ended) return Ended(c, side, id);
            return HandleCard(c, side, magicId, position, id, () => lookup.GetMagic(magicId));
        }
        private void ReturnCard(SimulationContext c, Master side, long card) { if (c.CardDecks.TryGetValue(side, out var deck)) deck.ReturnCard(card); }
        private InputResponse HandleCard(SimulationContext c, Master side, long card, Vector3? position, int id, Func<BoundMagic> resolve) {
            var player = c.Players[side]; if (!player.Cards.Contains(card)) return Fail(c, side, InputResultCode.FailLackOfCard, "Cannot use cards.", false, id);
            var magic = resolve(); if (magic == null) { player.Cards.Remove(card); ReturnCard(c, side, card); return Fail(c, side, InputResultCode.FailInvalidMagic, "Invalid magic.", true, id); }
            if (!position.HasValue) throw new InvalidOperationException("Bot cast carries no position");
            var caster = FindCaster(c, side); if (caster == null) return Fail(c, side, InputResultCode.FailInvalidPlace, "Caster is not found.", false, id);
            var range = c.GameParameters[magic.Name + ".range"]; var aim = Clamp(caster.Position, position.Value, range);
            var spot = ResolvePlacement(c, magic, aim, caster.Position, range); if (!spot.HasValue) return Fail(c, side, InputResultCode.FailInvalidPlace, "Cannot place here.", false, id);
            int cost = (int)(c.GameParameters[magic.Name + ".mana_cost"].RawValue / (1L << 32));
            if (!player.UseCard(card, cost)) return Fail(c, side, InputResultCode.FailInsufficientMana, "Insufficient mana.", false, id);
            magic.Run(c, side, caster.Position, spot.Value); ReturnCard(c, side, card);
            var result = Success(c, side, magic, id); if (c.UserIds.TryGetValue(side, out var userId)) c.MagicUsed?.Invoke(side, userId, magic.Id); return result;
        }
        public InputResponse HandleBotMagicInput(SimulationContext c, Master side, BoundMagic magic, Vector3 position, Vector3? origin) {
            if (c.Result.Ended) return Ended(c, side, -1);
            if (magic == null) return Fail(c, side, InputResultCode.FailInvalidMagic, "invalid bot magic", false, -1);
            var castOrigin = origin ?? FindCaster(c, side)?.Position; if (!castOrigin.HasValue) return Fail(c, side, InputResultCode.FailInvalidPlace, "invalid place", false, -1);
            var aim = Clamp(castOrigin.Value, position, c.GameParameters[magic.Name + ".range"]);
            if (!c.Players[side].SpendMana((int)(c.GameParameters[magic.Name + ".mana_cost"].RawValue / (1L << 32)))) return Fail(c, side, InputResultCode.FailInsufficientMana, "insufficient mana", false, -1);
            magic.Run(c, side, castOrigin, aim); return Success(c, side, magic, -1);
        }
        private InputResponse Success(SimulationContext c, Master side, BoundMagic magic, int id) { publisher.Publish(new InputHandleEvent(side, InputResultCode.Success, magic.Id)); return new InputResponse(true, InputResultCode.Success, "magic use is valid", c.Players[side].Mana, id, magic.Id); }
        private static SimObject FindCaster(SimulationContext c, Master side) => c.Objects.FirstOrDefault(owner => owner.IsActive && owner.Master == side && owner.Prefab == "Player");
        private static Vector3 Clamp(Vector3 origin, Vector3 position, Fix64 range) {
            position.X = MathHelper.Clamp(position.X, 0, 18); position.Z = MathHelper.Clamp(position.Z, 0, 10); var offset = position - origin; var distance = offset.Length(); return distance <= range || distance == Fix64.Zero ? position : origin + Vector3.Normalize(offset) * range;
        }
        private static Vector3? ResolvePlacement(SimulationContext c, BoundMagic magic, Vector3 aim, Vector3 origin, Fix64 range) {
            if (magic.SummonedParameterKey == null || magic.Airborne) return aim;
            var radius = c.GameParameters.TryGetValue(magic.SummonedParameterKey + ".radius", out var configured) ? configured : (Fix64)0.3m;
            var bodies = c.Objects.Select(owner => new PlacementBody(owner.Position, owner.FirstCircleRadius(false) ?? Fix64.Zero, owner.IsActive, owner.IsDestroyed, owner.HasComponent<IDamageable>(), owner.FirstCircleRadius(false).HasValue)).ToArray(); var water = c.Terrain.CopyWaterGrid();
            if (!CastPlacement.Blocked(aim, radius, false, bodies, water)) return aim;
            return CastPlacement.Resolve(aim, origin, range, radius, false, bodies, water, out var result) ? result : (Vector3?)null;
        }
    }
    public sealed class PlayerStatusSetter : SimComponent {
        private FlowSubscription subscription;
        public PlayerStatusSetter(SimObject owner) : base(owner) { }
        public override void Start() {
            if (Context.InputHandler == null) throw new InvalidOperationException("Game context has no MagicInputHandler");
            subscription = Context.InputHandler.Subscribe(value => { if (value.Master != Owner.Master) return; if (value.ResultCode == InputResultCode.Success) Owner.SetStatus(ObjectStatus.Attack); else if (value.ResultCode == InputResultCode.FailInvalidMagic) Owner.SetStatus(ObjectStatus.Hindered); });
        }
        public override void OnDestroy() { subscription?.Cancel(); }
    }
}

