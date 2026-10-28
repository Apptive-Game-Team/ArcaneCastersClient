using System;
using System.IO;
using System.Linq;
using ArcaneCasters.Simulation;
using FixMath.NET;

internal static class CoreTests {
    private static void CheckProjectileCombat() {
        var context = new SimulationContext(0) { DeltaTime = (Fix64)0.1m }; context.RegisterPrefab("Shot", _ => { }); context.RegisterPrefab("Target", _ => { });
        SimObject Target(Master master, decimal x, decimal z) {
            var owner = context.Spawn(master, "Target", new BEPUutilities.Vector3((Fix64)x, 0, (Fix64)z));
            owner.AddComponentImmediately(new DummyMob(owner, 20)); owner.AddComponentImmediately(new CommonEffectReceiver(owner)); owner.AddCollider(new CircleCollider(owner, (Fix64)0.5m)); return owner;
        }
        var enemy = Target(Master.RightPlayer, 5.6m, 5); var nearby = Target(Master.RightPlayer, 5.2m, 6);
        var ally = Target(Master.LeftPlayer, 5.2m, 5); var distant = Target(Master.RightPlayer, 8, 5);
        var projectile = context.Spawn(Master.LeftPlayer, "Shot", new BEPUutilities.Vector3(5, 0, 5)); var shot = new Shot(projectile, 4, 2);
        projectile.AddComponentImmediately(shot); projectile.AddCollider(new CircleCollider(projectile, (Fix64)0.25m, true)); shot.SetTarget(new BEPUutilities.Vector3(9, 0, 5)); context.FlushObjects();
        new ComponentUpdateSystem().Update(context); new BattlePhysicsSystem().Update(context);
        Check(projectile.IsDestroyed && enemy.GetComponent<DummyMob>().Hp == 16 && nearby.GetComponent<DummyMob>().Hp == 16, "Moving shot explodes once and hits nearby enemies through battle physics");
        Check(ally.GetComponent<DummyMob>().Hp == 20 && distant.GetComponent<DummyMob>().Hp == 20, "Shot explosion excludes allies and out-of-range bodies");
        shot.OnCollisionWithEnemy(enemy); Check(enemy.GetComponent<DummyMob>().Hp == 16, "Shot ignores subsequent collision callbacks");
        var ranged = new ProjectileRangeAttackMob(ally, 20, 1, 1, 3, 1, 4, "FireShot", (Fix64)0.2m);
        ranged.Behavior(enemy); Check(enemy.GetComponent<DummyMob>().Hp == 13, "Ranged projection damage is immediate despite visual travel duration");
        Check(context.DrainEvents().Any(item => item.Type == "FireShot" && item.Duration == (Fix64)0.2m && item.SourceId == ally.Id && item.TargetId == enemy.Id), "Ranged projection retains its source, target and duration");
    }
    private sealed class CollisionVelocityProbe : SimComponent, ICollidable {
        public BEPUutilities.Vector3 Observed;
        public bool DestroyOnContact;
        public CollisionVelocityProbe(SimObject owner) : base(owner) { }
        public void OnCollision(SimObject other) {
            var body = Owner.GetComponent<RigidBody>(); Observed = body.Velocity;
            body.AddVelocity(new BEPUutilities.Vector3(0, 0, 2));
            if (DestroyOnContact) Owner.Destroy();
        }
        public void OnCollisionWithEnemy(SimObject other) { }
    }
    private static void CheckCollisionStages() {
        foreach (bool destroy in new[] { false, true }) {
            var context = new SimulationContext(0) { DeltaTime = (Fix64)0.05m }; context.RegisterPrefab("Body", _ => { });
            var left = context.Spawn(Master.LeftPlayer, "Body", new BEPUutilities.Vector3(5, 0, 5));
            var right = context.Spawn(Master.LeftPlayer, "Body", new BEPUutilities.Vector3((Fix64)5.5m, 0, 5));
            foreach (var owner in new[] { left, right }) { owner.AddComponentImmediately(new RigidBody(owner, 1)); owner.AddCollider(new CircleCollider(owner, (Fix64)0.5m)); }
            var probe = new CollisionVelocityProbe(left) { DestroyOnContact = destroy }; left.AddComponentImmediately(probe); right.AddComponentImmediately(new WallCollision(right)); context.FlushObjects();
            left.GetComponent<RigidBody>().AddVelocity(new BEPUutilities.Vector3(1, 0, 0)); new BattlePhysicsSystem().Update(context);
            Check(probe.Observed == BEPUutilities.Vector3.Zero, "Collision callback observes solved velocity before position integration");
            Check(left.Position.X == 5 && left.Position.Z == (destroy ? (Fix64)5 : (Fix64)5 + 2 * context.DeltaTime), "Callback velocity applies this tick unless callback destroys the object");
            Check(right.Position.X == (Fix64)5.5m + context.DeltaTime, "Impulse transfer precedes destruction callback");
        }
    }
    private static void CheckCowardTransitions() {
        var context = new SimulationContext(7) { DeltaTime = (Fix64)0.1m };
        context.RegisterPrefab("ZapMouse", _ => { }); context.RegisterPrefab("Tower", _ => { }); context.RegisterPrefab("Threat", _ => { });
        var owner = context.Spawn(Master.LeftPlayer, "ZapMouse", new BEPUutilities.Vector3(5, 0, 5));
        var body = new RigidBody(owner, 2); owner.AddComponentImmediately(body);
        var mob = new CowardMob(owner, 10, 2, 1, 1, 1, 3, (Fix64)0.5m); owner.AddComponentImmediately(mob); mob.Start();
        var tower = context.Spawn(Master.RightPlayer, "Tower", new BEPUutilities.Vector3(9, 0, 5)); tower.AddComponentImmediately(new DummyMob(tower, 10));
        var threat = context.Spawn(Master.RightPlayer, "Threat", new BEPUutilities.Vector3(6, 0, 5)); threat.AddComponentImmediately(new AttackMob(threat, 10, 1, 1, 1, 1, 1));
        context.FlushObjects();
        Check(ReferenceEquals(mob.ObjectiveDetector.Detect(owner), tower), "Coward objective excludes nearby units");
        Check(ReferenceEquals(mob.ThreatDetector.Detect(owner), threat), "Coward threat excludes buildings");
        mob.Update(); Check(mob.CurrentState is CowardMob.PanicState, "Nearby unit starts panic");
        owner.FlushComponents();
        for (int i = 0; i < 6; i++) { mob.Update(); var velocity = body.ConsumeVelocity(); if (i == 0) Check(velocity.X == -4 && velocity.Z == Fix64.Zero, "Panic flees at twice movement speed"); }
        Check(mob.PanicCooldownRemaining > 9 && mob.CurrentState is CowardMob.CowardMoveState && ReferenceEquals(mob.Target, tower), "Panic reacquires objective during cooldown");
        owner.FlushComponents(); Check(!owner.HasEffect(VisualEffect.Panic), "Leaving panic removes its visual effect");
        mob.PanicCooldownRemaining = 10; owner.SetPosition(new BEPUutilities.Vector3((Fix64)8.75m, 0, 5)); mob.SetCowardAttack();
        for (int i = 0; i < 11; i++) mob.Update();
        Check(tower.GetComponent<DummyMob>().Hp == 9, "Coward attacks its objective on the original strict interval");
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Reject(Action action) {
        try { action(); } catch (InvalidDataException) { return; }
        throw new Exception("Expected invalid input rejection");
    }
    public static void Run() {
        ArcaneCasters.Parity.NatureUnitParityTests.CheckEvolvedUnitCombat();
        ArcaneCasters.Parity.SpawnerParityTests.CheckRandomAreaReplay();
        ArcaneCasters.Parity.HealingParityTests.CheckLifeTreeAndPveHealth();
        ArcaneCasters.Parity.HealingParityTests.CheckScenarioComponents();
        ArcaneCasters.Parity.RepairAuraParityTests.CheckPrefabAndRangeExit();
        ArcaneCasters.Parity.WindPushParityTests.CheckPushBoundaryAndRefresh();
        ArcaneCasters.Parity.LightningCloudParityTests.CheckPrefabCatchUpAndNeutral();
        ArcaneCasters.Parity.ShockTrapParityTests.CheckPrefabAndConcurrentStuns();
        ArcaneCasters.Parity.ExplosionParityTests.CheckPrefabDamageAndElements();
        ArcaneCasters.Parity.DropParityTests.CheckLeafairRecoveryAndDropDeath();
        ArcaneCasters.Parity.DropMagicParityTests.CheckRegisteredPrefabs();
        ArcaneCasters.Parity.RallyParityTests.CheckDirectiveLifecycle();
        ArcaneCasters.Parity.ChickenCommandoChecks.Run();
        ArcaneCasters.Parity.BuildMagicParityTests.CheckCatalogInputPlacement();
        ArcaneCasters.Parity.HealingTotemChecks.Run();
        ArcaneCasters.Parity.BubbleGeneratorChecks.Run();
        ArcaneCasters.Parity.RockTowerParityTests.CheckAttacks();
        ArcaneCasters.Parity.RockRemnantParityTests.CheckTowerDeathAndCollision();
        ArcaneCasters.Parity.RockUnitParityTests.CheckActualDeaths();
        ArcaneCasters.Parity.TitanParityTests.CheckActualAttackAndDeath();
        ArcaneCasters.Parity.CannonChecks.Run();
        ArcaneCasters.Parity.FireworkParityTests.CheckTower();
        ArcaneCasters.Parity.CraterParityTests.CheckMagicAndRandom();
        ArcaneCasters.Parity.FlameParityTests.CheckMagic();
        ArcaneCasters.Parity.LightningTowerChecks.Run();
        ArcaneCasters.Parity.ElementalNestChecks.Run();
        ArcaneCasters.Parity.ElectricDeathParityTests.CheckNestDeath();
        ArcaneCasters.Parity.TowerbackParityTests.CheckAttacksAndPrefab();
        ArcaneCasters.Parity.SpawnMagicParityTests.CheckRealSwarm();
        ArcaneCasters.Parity.TornadoParityTests.CheckActualPrefabs();
        ArcaneCasters.Parity.CraterCarrierParityTests.CheckActualCarrier();
        ArcaneCasters.Parity.RangedUnitChecks.Run();
        ArcaneCasters.Parity.BeanParityTests.CheckActualShooter();
        ArcaneCasters.Parity.FireSpiritChecks.Run();
        ArcaneCasters.Parity.FlyingSpiritChecks.Run();
        ArcaneCasters.Parity.SelfDestructParityTests.CheckActualWindSpirit();
        ArcaneCasters.Parity.BombSpriteParityTests.CheckActualBomber();
        ArcaneCasters.Parity.ThunderBirdParityTests.CheckActualSwarm();
        ArcaneCasters.Parity.CloudDragonParityTests.CheckActualDragon();
        ArcaneCasters.Parity.ShotMagicParityTests.CheckShotOriginsAndPrefabs();
        ArcaneCasters.Parity.RollingRockParityTests.CheckSpecialPrefabs();
        ArcaneCasters.Parity.VineMagicParityTests.CheckRealVinesAndWisp();
        ArcaneCasters.Parity.RayParityTests.CheckBoundaries();
        ArcaneCasters.Parity.SpiritBombParityTests.CheckChannelEdges();
        ArcaneCasters.Parity.TidalWarheadParityTests.CheckActualCast();
        ArcaneCasters.Parity.RemnantVineMagicParityTests.CheckActualGrowth();
        ArcaneCasters.Parity.AreaMagicParityTests.CheckActualAreas();
        ArcaneCasters.Parity.PveBossParityTests.CheckBoundBossCasting();
        ArcaneCasters.Parity.VineWitchParityTests.CheckRageEdges();
        ArcaneCasters.Parity.SimpleBossPrefabParityTests.CheckSpawnSelection();
        ArcaneCasters.Parity.DimensionToadParityTests.CheckTadpoleCombat();
        ArcaneCasters.Parity.SeaSerpentParityTests.CheckActualSerpent();
        ArcaneCasters.Parity.SummonerParityTests.CheckActualMagmaSpirit();
        ArcaneCasters.Parity.WaterSlimeParityTests.CheckActualSlime();
        ArcaneCasters.Parity.BrickGolemParityTests.CheckActualBoss();
        ArcaneCasters.Parity.WallGolemParityTests.CheckCombatAndRemnant();
        ArcaneCasters.Parity.VineColonyParityTests.CheckActualSummons();
        ArcaneCasters.Parity.NaturePrefabParityTests.CheckNestAndSlimeParameters();
        ArcaneCasters.Parity.AuraParityTests.CheckLifecycle();
        ArcaneCasters.Parity.CorruptedSpiritParityTests.CheckCombatVariants();
        ArcaneCasters.Parity.ManaWellParityTests.CheckResourceLifecycle();
        ArcaneCasters.Parity.PlayerHealthChecks.Run();
        ArcaneCasters.Parity.MagicInputParityTests.CheckActualCastAndStatusUnsubscribe();
        ArcaneCasters.Parity.MagicInputParityTests.CheckCardSelectionThroughInput();
        ArcaneCasters.Parity.BattleFrameChecks.Run();
        ArcaneCasters.Parity.BotLifecycleParityTests.CheckQueuedLifecycleAndFever();
        ArcaneCasters.Parity.CastDeadlineParityTests.CheckFrameClock();
        ArcaneCasters.Parity.BotEyeParityTests.CheckVisibleHealthAndStatus();
        ArcaneCasters.Parity.BotPersonaParityTests.CheckNormalization();
        ArcaneCasters.Parity.UnitCatalogueParityTests.CheckClampsAndQuantities();
        ArcaneCasters.Parity.BoardValueChecks.Run();
        ArcaneCasters.Parity.HospitalityParityTests.CheckFractionalAllowance();
        ArcaneCasters.Parity.PlacementPlannerParityTests.CheckRandomConsumption();
        ArcaneCasters.Parity.BlastTargetParityTests.CheckTiesAndPrediction();
        ArcaneCasters.Parity.PriorityRuleChecks.Run();
        ArcaneCasters.Parity.GamePlannerChecks.Run();
        ArcaneCasters.Parity.BotActionParityTests.CheckMissingHandlerAndOverflow();
        ArcaneCasters.Parity.BotAgentParityTests.CheckFrameClockDispatch();
        ArcaneCasters.Parity.BotAgentSystemParityTests.CheckOrderAndReplacement();
        ArcaneCasters.Parity.SessionBotChecks.Run();
        ArcaneCasters.Parity.SessionEmoteParityTests.CheckFrameBoundariesAndParsing();
        ArcaneCasters.Parity.SessionDefinitionParityTests.CheckInitialization();
        ArcaneCasters.Parity.BattleFieldParityTests.CheckPlayerAndTerrainInitialization();
        ArcaneCasters.Parity.BattleSessionChecks.Run();
        ArcaneCasters.Parity.ModelCandidateChecks.Run();
        ArcaneCasters.Parity.ModelDecisionChecks.Run();
        ArcaneCasters.Parity.LocalInputChecks.Run();
        ArcaneCasters.Parity.BotCounterParityTests.CheckSnapshot();
        ArcaneCasters.Parity.InitialStateChecks.Run();
        ArcaneCasters.Parity.ModelPolicyChecks.Run();
        var registrationContext = new ArcaneCasters.Simulation.SimulationContext(1);
        ArcaneCasters.Simulation.StandardPrefabs.Register(registrationContext);
        foreach (var prefabName in ArcaneCasters.Simulation.PrefabCatalog.Names) if (!registrationContext.HasPrefab(prefabName)) throw new System.InvalidOperationException("UNREGISTERED PREFAB " + prefabName);
        ArcaneCasters.Parity.GeneralToadChecks.Run();
        ArcaneCasters.Parity.PveInstallerParityTests.CheckInstallLifecycle();
        ArcaneCasters.Parity.PveResultParityTests.CheckSharedResultAndRate();
        ArcaneCasters.Parity.PveStateParityTests.CheckSnapshotsAndDefaults();
        ArcaneCasters.Parity.PveActionChecks.Run();
        ArcaneCasters.Parity.PveScriptParityTests.CheckActionsReplayAndShields();
        ArcaneCasters.Parity.MagicInputParityTests.CheckBrainQueuedCast();
        ArcaneCasters.Parity.SeedSpiritParityTests.CheckVinePrefab();
        ArcaneCasters.Parity.VineParityTests.CheckSharedHitTracker();
        ArcaneCasters.Parity.VineParityTests.CheckRegisteredToss();
        ArcaneCasters.Parity.PriorityUnitChecks.Run();
        ArcaneCasters.Parity.FireLordSpiritChecks.Run();
        ArcaneCasters.Parity.SpecialShotParityTests.CheckBoulderCollision();
        CheckProjectileCombat();
        CheckCollisionStages();
        CheckCowardTransitions();
        var trackerContext = new SimulationContext(0) { DeltaTime = (Fix64)0.05m };
        var tracked = new SimObject(trackerContext, Master.LeftPlayer, "StormStag", new BEPUutilities.Vector3(2, 0, 2));
        var tracker = new MovementSpeedTracker(tracked, 10);
        tracker.Start(); tracker.Begin(); tracked.AddEffect(VisualEffect.Burn);
        for (int i = 1; i <= 3; i++) {
            tracked.SetPosition(new BEPUutilities.Vector3((Fix64)2 + (Fix64)i * (Fix64)0.6m, i, 2)); tracker.Update();
            Check(tracker.Tier == (i < 3 ? 1 : 4), "Charge samples three frames and ignores vertical movement");
        }
        Check(tracked.HasEffect(VisualEffect.StormStagCharge4), "Charge tier effect follows measured movement");
        for (int i = 0; i < 3; i++) tracker.Update();
        Check(tracker.Tier == 1 && !tracked.HasEffect(VisualEffect.StormStagCharge4), "Blocked movement drops charge tier");
        tracker.Reset(); Check(tracked.HasEffect(VisualEffect.Burn), "Charge reset preserves unrelated effects");
        tracked.AddEffect(VisualEffect.StormStagCharge3); tracker.OnDestroy();
        Check(!tracked.HasEffect(VisualEffect.StormStagCharge3) && tracked.HasEffect(VisualEffect.Burn), "Destroy removes only charge effects");
        var cards = Enumerable.Range(1, 10).Select(i => (long)i).ToArray();
        new JavaRandom(1).Shuffle(cards);
        Check(cards.SequenceEqual(new long[] { 7, 10, 8, 9, 5, 3, 1, 4, 2, 6 }), "Java shuffle parity");
        var economy = new PlayerEconomy(new long[] { 1, 2, 3, 4, 5, 6, 7 }, 1, 100);
        for (int tick = 0; tick < 4; tick++) economy.Tick();
        Check(economy.Mana == 0, "Mana must not arrive early");
        economy.Tick(); Check(economy.Mana == 1, "Mana must arrive on tick 5");
        for (int tick = 5; tick < 20; tick++) economy.Tick();
        Check(economy.Hand.Count == 1 && economy.Mana == 4, "One second economy");
        var card = economy.Hand[0];
        Check(!economy.UseCard(card, 5) && economy.Hand.Count == 1, "Rejected casts preserve card");
        Check(economy.UseCard(card, 4) && economy.Hand.Count == 0 && economy.RemainingDeck.Last() == card, "Card cycling");
        economy.BeginFever(); economy.BeginFever();
        for (int tick = 0; tick < 20; tick++) economy.Tick();
        Check(economy.Mana == 8 && economy.Hand.Count == 2, "Fever only applies once");
        var fractional = new PlayerEconomy(Array.Empty<long>(), 1, 10);
        fractional.ManaCharge.ModifierPercent = (Fix64)(-0.5m);
        for (int tick = 0; tick < 10; tick++) fractional.Tick();
        Check(fractional.Mana == 1, "Fractional mana is accumulated");

        var stream = new ConfirmedInputStream("match");
        ConfirmedFrame Frame(uint tick) => new ConfirmedFrame { sessionId = "match", tick = tick, inputs = new[] { Array.Empty<CastCommand>(), Array.Empty<CastCommand>() } };
        stream.Enqueue(Frame(1)); Check(!stream.TryTake(out _), "Missing tick waits");
        stream.Enqueue(Frame(0)); Check(stream.TryTake(out var first) && first.tick == 0, "Ordered tick 0");
        Check(stream.TryTake(out var second) && second.tick == 1, "Ordered tick 1");
        stream.Enqueue(Frame(0)); Check(stream.NextTick == 2, "Duplicate does not advance");
        var conflict = Frame(0); conflict.inputs[0] = new[] { new CastCommand { magicId = 1 } };
        Reject(() => stream.Enqueue(conflict));
        var foreign = Frame(2); foreign.sessionId = "different"; Reject(() => stream.Enqueue(foreign));
        var bad = Frame(2); bad.inputs[0] = new[] { new CastCommand { magicId = 1, xRaw = -1 } }; Reject(() => stream.Enqueue(bad));
        Reject(() => stream.Enqueue(Frame(12000)));
        var copied = Frame(2); copied.inputs[0] = new[] { new CastCommand { magicId = 1 } };
        stream.Enqueue(copied); copied.inputs[0][0].magicId = 99;
        Check(stream.TryTake(out var preserved) && preserved.inputs[0][0].magicId == 1, "Receive buffer owns input");
        var selection = Frame(3); selection.inputs[0] = new[] { new CastCommand { kind = "selectCard", magicId = 7 } };
        stream.Enqueue(selection); selection.inputs[0][0].kind = "unselectCard";
        Check(stream.TryTake(out var selected) && selected.inputs[0][0].kind == "selectCard", "Receive buffer owns command kind");
        Reject(() => stream.Enqueue(selection));
        var malformed = Frame(4); malformed.inputs[0] = new[] { new CastCommand { kind = "unknown", magicId = 1 } }; Reject(() => stream.Enqueue(malformed));
        malformed.inputs[0][0].kind = "selectCard"; malformed.inputs[0][0].xRaw = 1; Reject(() => stream.Enqueue(malformed));
        var emoteFrame = Frame(4); emoteFrame.inputs[0] = new[] { new CastCommand { kind = "emote", emote = "Cry" } };
        stream.Enqueue(emoteFrame); emoteFrame.inputs[0][0].emote = "Laugh"; Reject(() => stream.Enqueue(emoteFrame));
        Check(stream.TryTake(out var receivedEmote) && receivedEmote.inputs[0][0].emote == "Cry", "Receive buffer owns emote");
        emoteFrame.tick = 5; emoteFrame.inputs[0][0].emote = "cry"; Reject(() => stream.Enqueue(emoteFrame));
        emoteFrame.inputs[0][0].emote = "Cry"; emoteFrame.inputs[0][0].magicId = 1; Reject(() => stream.Enqueue(emoteFrame));
        var fire = new CombatHealth(10, Element.Fire);
        fire.OnDamaged(new Hit(1, 1, Element.Nature));
        Check(fire.Hp == 9, "Fractional hit truncates remaining HP");
        fire.OnDamaged(new Hit(-10, 1)); Check(fire.Hp == 10, "Healing caps at max HP");
        bool immune = true;
        int consumed = 0, reacted = 0, deaths = 0;
        fire.Immunities.Add(_ => immune);
        fire.Interceptors.Add(_ => { consumed++; return true; });
        fire.ElementReactions.Add(_ => reacted++);
        fire.OnDamaged(new Hit(5, 1, Element.Water));
        Check(consumed == 0 && reacted == 0 && fire.Hp == 10, "Immunity precedes shield and element reactions");
        immune = false; fire.OnDamaged(new Hit(5, 1, Element.Water));
        Check(consumed == 1 && reacted == 0 && fire.Hp == 10, "Shield prevents reactions");
        fire.Interceptors.Clear(); fire.Died += () => deaths++;
        fire.OnDamaged(new Hit(5, 1, Element.Water, Element.Water));
        fire.OnDamaged(new Hit(5, 1, Element.Water));
        Check(fire.Hp == 0 && deaths == 1 && reacted == 1, "Unique elements and exactly one death");
        Check(ElementalChart.Average(new[] { Element.Fire, Element.Water }, new[] { Element.Nature }) == (Fix64)1.25m, "Pairwise elements");
        var dot = new CombatHealth(10);
        dot.Interceptors.Add(_ => throw new Exception("DOT must bypass interceptors"));
        dot.ApplyDamage(new Hit(2, 0)); Check(dot.Hp == 8, "Direct damage path");
        var origin = new BEPUutilities.Vector3(5, 0, 5);
        var target = new BEPUutilities.Vector3(7, 0, 5);
        Check(CombatRange.Contains(origin, (Fix64)0.5m, target, (Fix64)1, (Fix64)0.5m), "Range measures collider edges");
        Check(!CombatRange.Contains(origin, (Fix64)0.5m, target, (Fix64)0.5m, (Fix64)0.5m), "Small bodies need to close in");
        var bodies = new[] { new PlacementBody(origin, (Fix64)0.3m) };
        Check(CastPlacement.Blocked(origin, (Fix64)0.3m, false, bodies, null), "Ground overlap blocks cast");
        Check(!CastPlacement.Blocked(origin, (Fix64)0.3m, true, bodies, null), "Airborne summon ignores ground body");
        Check(CastPlacement.Resolve(origin, origin, (Fix64)3, (Fix64)0.3m, false, bodies, null, out var spot), "Blocked aim snaps");
        Check(spot.X == (Fix64)5.5m && spot.Z == (Fix64)5, "First free ring starts at positive X");
        var water = new bool[18, 10]; water[5, 5] = true;
        Check(CastPlacement.Blocked(origin, (Fix64)0.3m, false, Array.Empty<PlacementBody>(), water), "Water blocks summon");
        Check(!CastPlacement.Resolve(origin, target, (Fix64)1, (Fix64)0.3m, false, bodies, null, out _), "Out of range aim rejected");
        var physics = new PhysicsWorld(BEPUutilities.Vector3.Zero);
        physics.AddSphere(1, origin, (Fix64)1, Fix64.Zero, true);
        physics.AddSphere(2, origin, (Fix64)0.5m, Fix64.One);
        var entered = physics.Step();
        Check(entered.Length == 1 && entered[0].First == 1 && entered[0].Second == 2 && entered[0].Phase == ContactPhase.Enter, "Trigger enters once");
        Check(physics.Position(2) == origin, "Trigger applies no collision impulse");
        Check(physics.Step().Single().Phase == ContactPhase.Stay, "Trigger stays");
        physics.Remove(1);
        Check(physics.Step().Single().Phase == ContactPhase.Exit, "Removed trigger exits once");
        Check(physics.Step().Length == 0, "Exit does not repeat");
        physics.AddBox(3, new BEPUutilities.Vector3(10, 0, 5), new BEPUutilities.Vector3(1, 2, 2), Fix64.Zero);
        Check(physics.RayCast(new BEPUutilities.Vector3(8, 0, 5), BEPUutilities.Vector3.Right, (Fix64)4, out int hitId, out var hitPoint)
            && hitId == 3 && hitPoint.X == (Fix64)9.5m, "Fixed ray hits box surface");
        Check(!physics.RayCast(origin, BEPUutilities.Vector3.Zero, (Fix64)4, out _, out _), "Zero ray rejected");
        var clock = new BattleClock(180000, 60000);
        for (int i = 0; i < 2400; i++) clock.Advance();
        Check(!clock.Fever && clock.RemainingMilliseconds == 60000, "Fever boundary is strict");
        clock.Advance(); Check(clock.Fever, "Fever starts on next confirmed tick");
        for (int i = 0; i < 2400; i++) clock.Advance();
        Check(clock.Ended && clock.Tick == 3600, "Clock stops at match duration");
        var timer = new IntervalTimer(); int firings = 0;
        for (int i = 0; i < 200; i++) firings += timer.Advance((Fix64)0.25m);
        Check(firings == 40, "No timer drift at exact quarter seconds");
        var scheduler = new TickScheduler();
        scheduler.Schedule(1, 8, 10); scheduler.Schedule(1, 7, 20); scheduler.Schedule(1, 9, 30);
        scheduler.CancelOwner(9);
        Check(scheduler.Advance(0).Length == 0, "Delayed actions wait for their tick");
        var scheduled = scheduler.Advance(1);
        Check(scheduled.Length == 2 && scheduled[0].Kind == 10 && scheduled[1].Kind == 20, "Same tick preserves scheduling order");
        Check(scheduler.Advance(2).Length == 0, "Scheduled action only fires once");
        Console.WriteLine("PASS core: economy, ordered replay, damage, placement, physics queries, match clock, scheduled actions");
    }
}








