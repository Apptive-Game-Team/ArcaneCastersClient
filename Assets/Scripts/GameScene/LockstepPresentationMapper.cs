using System.Linq;
using ArcaneCasters.Simulation;
using GameScene.Dto;
using GameScene.Dto.Event;
using GameScene.Dto.Projectile;

namespace GameScene {
    public static class LockstepPresentationMapper {
        public static EmoteInfo Emote(Master side, ArcaneCasters.Simulation.Emote emote) => new EmoteInfo {
            type = "emote", side = side.ToString(), emote = emote.ToString()
        };
        public static BotThoughtInfo Thought(BotThought thought) => new BotThoughtInfo {
            type = "botThought", botSide = thought.Side.ToString(), ruleId = thought.RuleId, reason = thought.Reason,
            cards = thought.Cards.Select(id => (object)id).ToArray(), target = Vector(thought.Target)
        };
        public static ResultInfo Result(BattleResultMessage result) => new ResultInfo {
            type = "result", leftPlayer = result.LeftPlayer, rightPlayer = result.RightPlayer,
            lastLeftPlayerMmr = result.Mmr.LastLeftMmr, lastRightPlayerMmr = result.Mmr.LastRightMmr,
            newLeftPlayerMmr = result.Mmr.NewLeftMmr, newRightPlayerMmr = result.Mmr.NewRightMmr
        };
        public static PveObjectiveInfo Objective(PveObjectiveState state) => new PveObjectiveInfo {
            type = "pveObjective", winCondition = state.WinCondition.ToString(), surviveSeconds = state.SurviveSeconds,
            remainingSeconds = state.RemainingSeconds, objectivesTotal = state.ObjectivesTotal, objectivesRemaining = state.ObjectivesRemaining
        };
        public static PveStateInfo State(PveChannelState state) => new PveStateInfo { type = "pveState", channel = state.Channel, value = state.Value, seq = state.Seq };
        public static PveScriptEventInfo Script(PveScriptEvent message) => new PveScriptEventInfo {
            type = "pveScriptEvent", key = message.Key, speakerObjectId = message.SpeakerObjectId, lines = message.Lines.ToList(), seq = message.Seq
        };
        public static MagicValidInfo Input(InputResponse response) => new MagicValidInfo {
            type = "magicValid", id = response.Id, magicId = response.MagicId, valid = response.Valid,
            updatedMana = response.UpdatedMana, message = response.Message, resultCode = InputCode(response.ResultCode)
        };
        private static string InputCode(InputResultCode code) {
            switch (code) {
                case InputResultCode.Success: return "SUCCESS";
                case InputResultCode.FailInsufficientMana: return "FAIL_INSUFFICIENT_MANA";
                case InputResultCode.FailLackOfCard: return "FAIL_LACK_OF_CARD";
                case InputResultCode.FailInvalidPlace: return "FAIL_INVALID_PLACE";
                case InputResultCode.FailInvalidMagic: return "FAIL_INVALID_MAGIC";
                case InputResultCode.FailGameEnded: return "FAIL_GAME_ENDED";
                default: throw new System.ArgumentOutOfRangeException(nameof(code));
            }
        }
        public static ServerMessage Frame(BattlePresentationFrame frame) {
            var events = frame.Events.Where(value => value.Type == "hit" || value.Type == "shock").Select(Event).ToList();
            var projectiles = frame.Events.Where(value => value.Type != "hit" && value.Type != "shock").Select(Projectile).ToList();
            if (frame.IsSync) return new SyncFrameInfo {
                suppressSpawnPresentation = frame.IsRecovery,
                type = "sync", tickRate = frame.TickRate, remainingTime = frame.RemainingTime, updatedMana = frame.Mana,
                leftPlayerHp = frame.LeftHp, rightPlayerHp = frame.RightHp, events = events, projectileDtos = projectiles,
                snapshotResponseDto = new SnapshotDto { frame = frame.Frame, myCards = frame.Cards, objects = frame.Objects.Select(Object).ToArray() }
            };
            return new FrameInfoDto {
                type = "frame", remainingTime = frame.RemainingTime, updatedMana = frame.Mana, leftPlayerHp = frame.LeftHp, rightPlayerHp = frame.RightHp,
                cards = new CardInfo { added = frame.Cards }, events = events,
                objects = new ObjectsInfo {
                    create = frame.Created.Select(value => new CreatedObjectDto(Object(value))).ToArray(),
                    update = frame.Updated.Select(value => new UpdatedObjectDto(Object(value))).ToArray(), projectile = projectiles.ToArray()
                }
            };
        }
        private static GameEvent Event(BattleEvent value) => value.Type == "hit"
            ? (GameEvent)new HitEvent { type = "hit", actorId = value.SourceId, targetId = value.TargetId }
            : new ShockEvent { type = "shock", actorId = value.SourceId };
        private static ProjectileDto Projectile(BattleEvent value) => new ProjectileDto {
            type = value.Type, duration = (float)value.Duration, width = (float)value.Width,
            start = Target(value.StartPosition, value.SourceId), end = Target(value.EndPosition, value.TargetId)
        };
        private static ProjectileTarget Target(BEPUutilities.Vector3? position, int id) => position.HasValue
            ? (ProjectileTarget)new PositionProjectileTarget { targetType = "position", x = (float)position.Value.X, y = (float)position.Value.Y, z = (float)position.Value.Z }
            : new ReferenceProjectileTarget { targetType = "reference", id = id };
        // float 변환은 기존 화면 DTO를 만드는 이 경계에서만 수행한다.
        public static SnapshotObjectDto Object(ObjectSnapshot snapshot) => new SnapshotObjectDto {
            id = snapshot.Id, prefab = snapshot.Prefab, master = snapshot.Master.ToString(), status = snapshot.Status.ToString(),
            x = (float)snapshot.Position.X, y = (float)snapshot.Position.Y, z = (float)snapshot.Position.Z,
            effects = snapshot.Effects.Select(effect => effect.ToString()).ToList(),
            gauges = snapshot.Gauges.Select(gauge => new Dto.Gauge { value = (float)gauge.Value, maxValue = (float)gauge.MaxValue, category = gauge.Category == GaugeCategory.Hp ? "HP" : "TTL" }).ToList(),
            gizmos = snapshot.Gizmos.Select(gizmo => new Dto.debug.Gizmo {
                relativePosition = Vector(gizmo.RelativePosition), radius = (float)gizmo.Radius,
                boxSize = gizmo.BoxSize.HasValue ? Vector(gizmo.BoxSize.Value) : (UnityEngine.Vector3?)null,
                type = gizmo.Type.ToString(), category = gizmo.Category.ToString()
            }).ToList()
        };
        private static UnityEngine.Vector3 Vector(BEPUutilities.Vector3 value) => new UnityEngine.Vector3((float)value.X, (float)value.Y, (float)value.Z);
    }
}
