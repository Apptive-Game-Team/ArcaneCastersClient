using System;
using System.Collections.Generic;
using GameScene.Dto;
using GameScene.Dto.Event;
using GameScene.Object;
using GameScene.ServedObjectComponent;
using GameScene.ServedObjectComponent.Effect;
using GameScene.ServedObjectComponent.Motion;
using GameScene.ServedObjectComponent.OnAttack;
using GameScene.ServedObjectComponent.Sound;
using Global;
using UnityEngine;

namespace GameScene.Handler
{
    /// <summary>
    /// Applies the one-off events of a frame. Runs before object updates so the attacker of a hit is
    /// still on the client when its position is read; that frame's update may destroy it.
    /// </summary>
    public class GameEventHandler : IFrameInfoHandler<List<GameEvent>>
    {
        private const float ImpactEdgeBias = 0.6f;
        private const string ShockBurstEffectName = "ShockBurst";
        private const string DetectionRangeGizmoCategory = "DetectionRange";

        public void Handler(List<GameEvent> events)
        {
            Handler(events, null);
        }

        public void Handler(List<GameEvent> events, PresentationWorld world)
        {
            if (events == null)
            {
                return;
            }

            foreach (GameEvent gameEvent in events)
            {
                if (gameEvent is HitEvent hit)
                {
                    HandleHit(hit, world);
                }
                else if (gameEvent is ShockEvent shock)
                {
                    HandleShock(shock, world);
                }
            }
        }

        internal void PlayAuthoredAttacks(List<GameEvent> events, PresentationWorld world = null)
        {
            if (events == null)
            {
                return;
            }

            foreach (GameEvent gameEvent in events)
            {
                switch (gameEvent)
                {
                    case SummonAttackEvent summon:
                        PlaySummonAttack(summon, world);
                        break;
                    case VineWitchCastEvent cast:
                        PlayVineWitchCast(cast, world);
                        break;
                }
            }
        }

        private static void PlaySummonAttack(SummonAttackEvent summon, PresentationWorld world)
        {
            ServedObject actor = PresentationWorld.Find(summon.actorId, world);
            ServedObject fist = PresentationWorld.Find(summon.targetId, world);
            if (actor != null && !actor.IsDespawning && actor.PrefabType == "MagmaSpirit" &&
                fist != null && fist.PrefabType == "MagmaFist" && actor.GetMaster() == fist.GetMaster())
            {
                actor.PlayAttackPresentation();
            }
        }

        private static void PlayVineWitchCast(VineWitchCastEvent cast, PresentationWorld world)
        {
            ServedObject actor = PresentationWorld.Find(cast.actorId, world);
            if (actor == null || actor.IsDespawning || actor.PrefabType != "PveVineWitch" ||
                !actor.TryGetComponent(out AttackFrameSequenceController pose) || !pose.isActiveAndEnabled)
            {
                return;
            }

            ServedObject target = PresentationWorld.Find(cast.targetId, world);
            if (target != null)
            {
                actor.FaceTowards(target.transform.position);
            }

            bool isFan = string.Equals(cast.type, VineWitchCastEvent.FanType, StringComparison.OrdinalIgnoreCase);
            pose.PlayForDuration(isFan ? 0.4f : 0.5f);
        }

        private static void HandleHit(HitEvent hit, PresentationWorld world)
        {
            ServedObject target = PresentationWorld.Find(hit.targetId, world);
            if (target == null)
            {
                return;
            }

            // Contact audio belongs to the target, including hits without an actor
            // or a visual HitEffectController and charged StormStag's early return.
            if (world == null) target.GetComponent<ServedObjectSfxController>()?.PlayHit();

            ServedObject actor = PresentationWorld.Find(hit.actorId, world);
            if (actor != null)
            {
                actor.FaceTowards(target.transform.position);
                actor.GetComponent<GroundedUnitPoseController>()?.PlayHitPose();
                actor.GetComponent<ThunderBirdFlightPresenter>()?.PlayHitPose();
                HitKnockback.Play(target, actor.transform.position);
            }

            if (actor != null && StormStagChargeImpactRules.ShouldPlay(actor.ActiveEffects))
            {
                DamagedObjectEffect.SetSelfDestroyEffect(
                    StormStagChargeImpactRules.EffectResourceName,
                    target.GetEdgeWorldPositionTowards(
                        actor.GetActualTransform().position,
                        ImpactEdgeBias), world);
                return;
            }

            HitEffectController hitEffect = target.GetComponentInChildren<HitEffectController>();
            if (hitEffect == null)
            {
                return;
            }

            if (actor == null)
            {
                hitEffect.PlayHit();
                return;
            }

            hitEffect.PlayHitFrom(actor.GetActualTransform().position);
        }

        private static void HandleShock(ShockEvent shock, PresentationWorld world)
        {
            ServedObject trap = PresentationWorld.Find(shock.actorId, world);
            if (trap == null)
            {
                return;
            }

            GameObject burstPrefab = (GameObject) Resources.Load($"Prefabs/Effects/{ShockBurstEffectName}");
            if (burstPrefab == null)
            {
                WDebug.LogWarning($"Effect prefab '{ShockBurstEffectName}' not found.");
                return;
            }

            GameObject burst = world != null ? world.SpawnEffect(burstPrefab, trap.GetActualTransform().position, Quaternion.identity) : UnityEngine.Object.Instantiate(
                burstPrefab,
                trap.GetActualTransform().position,
                Quaternion.identity);

            // If the trap sent no DetectionRange gizmo, leave the prefab's own scale rather than
            // skip the burst entirely.
            if (trap.TryGetGizmoRadius(DetectionRangeGizmoCategory, out float radius))
            {
                ScaleBurstToDiameter(burst, radius * 2f);
            }
        }

        /// <summary>Scales the burst so the sprite's width matches <paramref name="targetDiameter"/>,
        /// following the same sprite-bounds convention as <c>CircleSkillIndicator.GetScaleForRadius</c>.</summary>
        private static void ScaleBurstToDiameter(GameObject burst, float targetDiameter)
        {
            SpriteRenderer spriteRenderer = burst.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            float spriteWidth = spriteRenderer.sprite.bounds.size.x;
            if (spriteWidth <= 0f)
            {
                return;
            }

            float scale = targetDiameter / spriteWidth;
            Vector3 currentScale = burst.transform.localScale;
            burst.transform.localScale = new Vector3(scale, scale, currentScale.z);
        }
    }
}
