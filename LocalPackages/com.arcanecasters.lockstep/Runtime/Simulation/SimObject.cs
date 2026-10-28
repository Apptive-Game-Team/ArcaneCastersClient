using System;
using System.Collections.Generic;
using System.Linq;
using BEPUutilities;
using FixMath.NET;

namespace ArcaneCasters.Simulation {
    public enum Master { LeftPlayer, RightPlayer, None }
    public enum ObjectStatus { Idle, Initializing, Move, Attack, Destroyed, Damaged, Hindered, Dying, Deploying, Deployed, Recovering }
    public enum VisualEffect {
        None, Burn, Wet, Shock, Snared, ShockTrapArming, Knockback, LeafFieldHeal, Sandstorm,
        Frenzy, Panic, Bubble, Inspired, Overcharge, StormStagCharge2, StormStagCharge3, StormStagCharge4,
        CardSelected, FireIdleAura, FireAttackAura, WaterIdleAura, WaterAttackAura, NatureIdleAura,
        NatureAttackAura, LightningIdleAura, LightningAttackAura, RockIdleAura, RockAttackAura, WindIdleAura, WindAttackAura
    }

    public abstract class SimComponent {
        public SimObject Owner { get; }
        public long SourceId { get; }
        public SimulationContext Context => Owner.Context;
        protected SimComponent(SimObject owner) { Owner = owner ?? throw new ArgumentNullException(nameof(owner)); SourceId = owner.Context.NextComponentId(); }
        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void OnDestroy() { }
        public virtual bool ActiveWhileDying => false;
    }

    public readonly struct StatusChange {
        public readonly ObjectStatus Previous, Current;
        public StatusChange(ObjectStatus previous, ObjectStatus current) { Previous = previous; Current = current; }
    }

    public sealed class ElementSet {
        private readonly SortedSet<Element> native = new SortedSet<Element>();
        private readonly SortedDictionary<Element, HashSet<long>> bonuses = new SortedDictionary<Element, HashSet<long>>();
        public Element[] Total {
            get {
                var result = new SortedSet<Element>(native);
                foreach (var entry in bonuses) if (entry.Value.Count > 0) result.Add(entry.Key);
                if (result.Count == 0) result.Add(Element.None);
                return result.ToArray();
            }
        }
        public bool Has(Element element) => native.Contains(element) || bonuses.TryGetValue(element, out var sources) && sources.Count > 0;
        public bool NativeHas(Element element) => native.Contains(element);
        public void AddNative(Element element) { native.Add(element); }
        public void RemoveNative(Element element) { native.Remove(element); }
        public void AddBonus(Element element, long source) {
            if (!bonuses.TryGetValue(element, out var sources)) bonuses.Add(element, sources = new HashSet<long>());
            sources.Add(source);
        }
        public void RemoveBonus(Element element, long source) {
            if (!bonuses.TryGetValue(element, out var sources)) return;
            sources.Remove(source); if (sources.Count == 0) bonuses.Remove(element);
        }
        public void ClearBonusFrom(long source) { foreach (var element in bonuses.Keys.ToArray()) RemoveBonus(element, source); }
    }

    public interface ISimCollider {
        bool IsTrigger { get; }
        Fix64? CircleRadius { get; }
    }

    public sealed class SimObject {
        private readonly List<SimComponent> components = new List<SimComponent>();
        private readonly List<SimComponent> toAdd = new List<SimComponent>();
        private readonly List<SimComponent> toRemove = new List<SimComponent>();
        private readonly List<VisualEffect> effects = new List<VisualEffect>();
        private readonly List<ISimCollider> colliders = new List<ISimCollider>();
        private readonly List<SimGizmo> gizmos = new List<SimGizmo>();
        private readonly FlowPublisher<StatusChange> statusChanges = new FlowPublisher<StatusChange>();
        public int Id { get; }
        public Master Master { get; private set; }
        public string Prefab { get; }
        public ObjectStatus Status { get; private set; } = ObjectStatus.Initializing;
        public Vector3 Position { get; private set; }
        public SimulationContext Context { get; }
        public ElementSet Elements { get; } = new ElementSet();
        public bool IsDestroyed => Status == ObjectStatus.Destroyed;
        public bool IsDying => Status == ObjectStatus.Dying;
        public bool IsInitialized => Status != ObjectStatus.Initializing;
        public bool IsActive => !IsDestroyed && IsInitialized;
        public VisualEffect[] Effects => effects.ToArray();
        public SimComponent[] Components => components.ToArray();
        public SimComponent[] PendingComponents => toAdd.ToArray();
        public ISimCollider[] Colliders => colliders.ToArray();
        public SimGizmo[] Gizmos => gizmos.ToArray();
        public void DrawCircle(Vector3 relativePosition, Fix64 radius, GizmoCategory category) => gizmos.Add(SimGizmo.Circle(relativePosition, radius, category));
        public void DrawBox(Vector3 relativePosition, Vector3 size, GizmoCategory category) => gizmos.Add(SimGizmo.Box(relativePosition, size, category));

        public SimObject(SimulationContext context, Master master, string prefab, Vector3 position) {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Id = context.NextObjectId(); Master = master; Prefab = prefab; Position = position;
        }
        public T GetComponent<T>() where T : class => components.OfType<T>().FirstOrDefault();
        public T[] GetComponents<T>() where T : class => components.OfType<T>().ToArray();
        public T[] GetPendingComponents<T>() where T : class => toAdd.OfType<T>().ToArray();
        public bool HasComponent<T>() where T : class => GetComponent<T>() != null;
        public void AddComponent(SimComponent component) { CheckOwner(component); toAdd.Add(component); }
        public void AddComponentImmediately(SimComponent component) { CheckOwner(component); components.Add(component); }
        private void CheckOwner(SimComponent component) {
            if (component == null || !ReferenceEquals(component.Owner, this)) throw new ArgumentException("Component owner mismatch");
        }
        public void RemoveComponent(SimComponent component) {
            if (components.Any(c => ReferenceEquals(c, component)) || toAdd.Any(c => ReferenceEquals(c, component))) toRemove.Add(component);
        }
        public bool CancelComponentRemoval(SimComponent component) => RemoveReference(toRemove, component);
        public bool RemoveComponentImmediately(SimComponent component) => RemoveReference(components, component);
        private static bool RemoveReference(List<SimComponent> list, SimComponent component) {
            int index = list.FindIndex(c => ReferenceEquals(c, component)); if (index < 0) return false;
            list.RemoveAt(index); return true;
        }
        public void AddCollider(ISimCollider collider) { colliders.Add(collider); }
        public Fix64? FirstCircleRadius(bool trigger) => colliders.FirstOrDefault(c => c.IsTrigger == trigger && c.CircleRadius.HasValue)?.CircleRadius;
        public FlowSubscription SubscribeStatusChange(Action<StatusChange> callback) => statusChanges.Subscribe(callback);
        public void SetStatus(ObjectStatus status) {
            var previous = Status;
            if (previous == ObjectStatus.Destroyed || previous == ObjectStatus.Dying && status != ObjectStatus.Destroyed) return;
            if (previous == status && status != ObjectStatus.Attack && status != ObjectStatus.Hindered) return;
            Status = status; statusChanges.Publish(new StatusChange(previous, status)); ApplyUpdate();
        }
        public void SetMaster(Master master) { if (Master != master) { Master = master; ApplyUpdate(); } }
        public void SetPositionInPlace(Vector3 position) { Position = position; }
        public void SetPosition(Vector3 position) {
            Position = position;
            if (!CastPlacement.InsideMap(position)) { Destroy(); return; }
            ApplyUpdate();
        }
        public bool HasEffect(VisualEffect effect) => effects.Contains(effect);
        public void AddEffect(VisualEffect effect) {
            if (effect == VisualEffect.None || effects.Contains(effect)) return;
            effects.Add(effect); ApplyUpdate();
        }
        public void RemoveEffect(VisualEffect effect) { if (effects.Remove(effect)) ApplyUpdate(); }
        public void RemoveEffects(Predicate<VisualEffect> predicate) { if (effects.RemoveAll(predicate) > 0) ApplyUpdate(); }
        public void ClearEffects() { if (effects.Count > 0) { effects.Clear(); ApplyUpdate(); } }
        public void Start() {
            Context.InitializePrefab(this);
            components.AddRange(toAdd); toAdd.Clear();
            foreach (var component in Components) component.Start();
            SetStatus(ObjectStatus.Idle);
        }
        public void Update() {
            bool dying = IsDying;
            foreach (var component in Components) if (!dying || component.ActiveWhileDying) component.Update();
        }
        public void Destroy() { SetStatus(ObjectStatus.Destroyed); ApplyUpdate(); OnDestroy(); }
        public void OnDestroy() { foreach (var component in Components) component.OnDestroy(); statusChanges.Close(); }
        public void Discard() {
            Status = ObjectStatus.Destroyed; statusChanges.Close(); ApplyUpdate();
            components.Clear(); toAdd.Clear(); toRemove.Clear(); colliders.Clear();
        }
        public void FlushComponents() {
            var added = toAdd.ToArray(); toAdd.Clear(); components.AddRange(added);
            foreach (var component in added) component.Start();
            var removed = toRemove.ToArray(); toRemove.Clear();
            foreach (var component in removed) component.OnDestroy();
            components.RemoveAll(c => removed.Any(removedComponent => ReferenceEquals(c, removedComponent)));
        }
        public void ApplyUpdate() { Context.UpdateObject(this); }
    }

    public sealed class SimulationContext {
        internal VineHitTracker ActiveVineTracker { get; set; }
        private int nextObjectId;
        private long nextComponentId = 1;
        private readonly Dictionary<Type, object> events = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        public int TickRate { get; private set; } = 20;
        public void SetTickRate(int tickRate) {
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate), tickRate, "tickRate must be positive");
            TickRate = tickRate; DeltaTime = Fix64.One / tickRate;
        }
        public void InsertService<T>(T service) where T : class => services[typeof(T)] = service;
        public T Service<T>() where T : class => services.TryGetValue(typeof(T), out var service) ? (T)service : null;
        public Fix64 DeltaTime { get; set; } = PhysicsWorld.StepDuration;
        public JavaRandom Random { get; set; } = new JavaRandom(1);
        public int FrameNumber { get; set; }
        public Terrain Terrain { get; set; } = Terrain.None;
        private FlowFieldNavigation navigation;
        public FlowFieldNavigation Navigation => navigation ?? (navigation = new FlowFieldNavigation(() => FrameNumber, () => Objects, () => Terrain));
        public bool SuppressDeathFields { get; set; }
        public IDictionary<string, Fix64> GameParameters { get; } = new BattleParameters();
        public IDictionary<Master, PlayerData> Players { get; } = new Dictionary<Master, PlayerData>();
        public BattleResult Result { get; } = new BattleResult();
        public IDictionary<Master, long> UserIds { get; } = new Dictionary<Master, long>();
        public IDictionary<Master, CardDeck> CardDecks { get; } = new Dictionary<Master, CardDeck>();
        public MagicInputHandler InputHandler { get; set; }
        public GameActionQueue<SimulationContext> Actions { get; } = new GameActionQueue<SimulationContext>();
        public bool SubmitAction(string name, Action<SimulationContext> action) => Actions.Submit(name, action);
        public void DrainActions() => Actions.Drain(this);
        public int PendingActionCount => Actions.Count;
        public GameActionSender<SimulationContext> ActionSender() => new GameActionSender<SimulationContext>(Actions);
        public CardSelectVisualizer CardSelectVisualizer { get; set; }
        public void SelectCard(long userId, long magicId) => CardSelectVisualizer?.SelectCard(this, userId, magicId);
        public void UnselectCard(long userId, long magicId) => CardSelectVisualizer?.UnselectCard(this, userId, magicId);
        public IBossMagicParser BossMagicParser { get; set; }
        public Action<Master, long, long> MagicUsed;
        private readonly Dictionary<string, Action<SimObject>> initializers = new Dictionary<string, Action<SimObject>>(StringComparer.Ordinal);
        private readonly List<SimObject> objects = new List<SimObject>();
        private readonly List<SimObject> pending = new List<SimObject>();
        public SimObject[] Objects => objects.ToArray();
        public SimObject[] PendingObjects => pending.ToArray();
        public event Action<SimObject> Created, Updated;
        public event Action<SimObject, Exception> InitializationFailed;
        public Action<SimObject> ObjectInitializer;
        public Action BeforeCreate;
        public SimulationContext(int firstObjectId = 1) { nextObjectId = firstObjectId; }
        internal int NextObjectId() => checked(nextObjectId++);
        internal long NextComponentId() => checked(nextComponentId++);
        public void AddEvent<T>(T value) {
            if (!events.TryGetValue(typeof(T), out var list)) events[typeof(T)] = list = new List<T>();
            ((List<T>)list).Add(value);
        }
        public T[] DrainEvents<T>() {
            if (!events.TryGetValue(typeof(T), out var list)) return Array.Empty<T>();
            events.Remove(typeof(T)); return ((List<T>)list).ToArray();
        }
        public BattleEvent[] DrainEvents() => DrainEvents<BattleEvent>();
        public SimObject FindPlayer(Master master) => Objects.FirstOrDefault(owner => owner.IsActive && owner.Master == master && owner.Prefab == "Player");
        public SimObject FindPlayerByUser(long userId) {
            foreach (var pair in UserIds) if (pair.Value == userId) return FindPlayer(pair.Key);
            return null;
        }
        public void RegisterPrefab(string name, Action<SimObject> initializer) { initializers.Add(name, initializer); }
        public bool HasPrefab(string name) => initializers.ContainsKey(name) || (PrefabCatalog.CanonicalName(name) is string canonical && initializers.ContainsKey(canonical));
        internal void InitializePrefab(SimObject owner) {
            if (!initializers.TryGetValue(owner.Prefab, out var initializer)) {
                var canonical = PrefabCatalog.CanonicalName(owner.Prefab);
                if (canonical == null || !initializers.TryGetValue(canonical, out initializer)) throw new InvalidOperationException("Unsupported prefab: " + owner.Prefab);
            }
            initializer(owner);
        }
        public SimObject Spawn(Master master, string prefab, Vector3 position) {
            var owner = new SimObject(this, master, prefab, position);
            BeforeCreate?.Invoke(); pending.Add(owner);
            try { owner.Start(); ObjectInitializer?.Invoke(owner); }
            catch (Exception exception) { owner.SetStatus(ObjectStatus.Destroyed); InitializationFailed?.Invoke(owner, exception); return owner; }
            Created?.Invoke(owner); return owner;
        }
        internal void UpdateObject(SimObject owner) { Updated?.Invoke(owner); }
        public bool RemoveObject(SimObject owner) => objects.Remove(owner);
        public void FlushObjects() {
            objects.RemoveAll(owner => owner.IsDestroyed);
            foreach (var owner in Objects) owner.FlushComponents();
            objects.AddRange(pending); pending.Clear();
        }
    }
}
