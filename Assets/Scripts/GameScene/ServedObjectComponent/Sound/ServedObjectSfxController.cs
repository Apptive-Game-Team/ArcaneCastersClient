using System.Collections.Generic;
using Sound;
using Sound.Config;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Sound
{
    public class ServedObjectSfxController : MonoBehaviour
    {
        private static readonly HashSet<string> WarnedRuntimeTypes = new();
        private static ObjectSfxCatalog catalog;
        private static bool catalogLoadAttempted;

        private ServedObject servedObject;
        private ObjectSfxProfile profile;
        private ObjectSfxEventSlot attackSlot;
        private float nextMovementTime;
        private bool deathPlayed;
        private bool ownsAttack;
        private bool ownsMovement;
        private bool ownsHit;
        private bool ownsHeal;
        private bool ownsDeath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            catalog = null;
            catalogLoadAttempted = false;
            WarnedRuntimeTypes.Clear();
        }

        public static void Attach(ServedObject target, string runtimeType, bool playSpawn)
        {
            if (target.GetComponent<ServedObjectSfxController>() != null ||
                !TryResolveProfile(runtimeType, out ObjectSfxProfile resolvedProfile,
                    out ObjectSfxEventSlot resolvedAttack))
            {
                return;
            }

            ServedObjectSfxController controller =
                target.gameObject.AddComponent<ServedObjectSfxController>();
            controller.Initialize(target, resolvedProfile, resolvedAttack, playSpawn);
        }

        private static bool TryResolveProfile(
            string runtimeType,
            out ObjectSfxProfile resolvedProfile,
            out ObjectSfxEventSlot resolvedAttack)
        {
            resolvedProfile = null;
            resolvedAttack = null;
            if (!catalogLoadAttempted)
            {
                catalog = Resources.Load<ObjectSfxCatalog>(ObjectSfxCatalog.ResourcesPath);
                catalogLoadAttempted = true;
            }

            if (catalog == null)
            {
                WarnOnce(
                    "__MissingCatalog__",
                    $"Object SFX catalog is missing at Resources/{ObjectSfxCatalog.ResourcesPath}. " +
                    "Profile lifecycle SFX will remain silent.");
                return false;
            }

            if (!catalog.TryResolve(runtimeType, out resolvedProfile, out resolvedAttack))
            {
                WarnOnce(
                    runtimeType,
                    $"Object SFX catalog has no row for runtime type '{runtimeType}'. " +
                    "Profile lifecycle SFX will remain silent.");
                return false;
            }

            return resolvedProfile != null || resolvedAttack != null;
        }

        // Offline tutorial casts have no authoritative ServedObject attack event.
        public static void PlayAttackForType(string runtimeType)
        {
            if (TryResolveProfile(runtimeType, out _, out ObjectSfxEventSlot attack))
                PlaySlot(attack, GameSfxCategory.Attack, GameSfxPriority.Attack);
        }

        public static void PlaySpawnForType(string runtimeType)
        {
            if (TryResolveProfile(runtimeType, out ObjectSfxProfile resolvedProfile, out _))
                PlaySpawn(resolvedProfile);
        }

        // Cosmetic effects do not have a server object to own a lifecycle controller.
        public static void PlaySpawn(ObjectSfxProfile spawnProfile)
        {
            if (spawnProfile != null)
                PlaySlot(spawnProfile.Spawn, GameSfxCategory.SpawnDeath, GameSfxPriority.Spawn);
        }

        private static void WarnOnce(string key, string message)
        {
            string normalizedKey = string.IsNullOrEmpty(key) ? "__EmptyRuntimeType__" : key;
            if (WarnedRuntimeTypes.Add(normalizedKey))
            {
                Debug.LogWarning(message);
            }
        }

        private void Initialize(
            ServedObject target,
            ObjectSfxProfile resolvedProfile,
            ObjectSfxEventSlot resolvedAttack,
            bool playSpawn)
        {
            servedObject = target;
            profile = resolvedProfile;
            attackSlot = resolvedAttack;
            deathPlayed = false;
            nextMovementTime = 0f;

            ownsMovement = profile != null && profile.Movement.Enabled;
            if (ownsMovement)
            {
                servedObject.OnMoved += PlayMovement;
            }

            ownsHit = profile != null && profile.Hit.Enabled;
            if (ownsHit)
            {
                servedObject.OnHpDecreased += PlayHit;
            }

            ownsHeal = profile != null && profile.Heal.Enabled;
            if (ownsHeal)
            {
                servedObject.OnHpIncreased += PlayHeal;
            }

            ownsDeath = profile != null && profile.Death.Enabled;
            if (ownsDeath)
            {
                servedObject.OnDestroyed += PlayDeath;
            }

            ownsAttack = attackSlot != null && attackSlot.Enabled && attackSlot.Clip != null;
            if (ownsAttack)
            {
                servedObject.OnAttack += PlayAttack;
            }

            if (playSpawn && profile != null)
            {
                PlaySpawn(profile);
            }
        }

        private void PlayMovement()
        {
            if (Time.unscaledTime < nextMovementTime)
            {
                return;
            }

            nextMovementTime = Time.unscaledTime + profile.MovementCooldown;
            PlaySlot(
                profile.Movement,
                GameSfxCategory.Movement,
                GameSfxPriority.Movement);
        }

        private void PlayAttack() =>
            PlaySlot(attackSlot, GameSfxCategory.Attack, GameSfxPriority.Attack);

        private void PlayHeal() =>
            PlaySlot(profile.Heal, GameSfxCategory.HitHeal, GameSfxPriority.HitHeal);

        private void PlayHit() =>
            PlaySlot(profile.Hit, GameSfxCategory.HitHeal, GameSfxPriority.HitHeal);

        private void PlayDeath()
        {
            if (deathPlayed)
            {
                return;
            }

            deathPlayed = true;
            PlaySlot(
                profile.Death,
                GameSfxCategory.SpawnDeath,
                GameSfxPriority.Death);
        }

        private static void PlaySlot(
            ObjectSfxEventSlot slot,
            GameSfxCategory category,
            GameSfxPriority priority)
        {
            if (slot == null || !slot.Enabled || slot.Clip == null)
            {
                return;
            }

            GameSfxPlayer.Play(slot.Clip, category, priority, slot.Volume, slot.Pitch);
        }

        private void OnDestroy()
        {
            if (servedObject == null)
            {
                return;
            }

            if (ownsMovement)
            {
                servedObject.OnMoved -= PlayMovement;
            }

            if (ownsHit)
            {
                servedObject.OnHpDecreased -= PlayHit;
            }

            if (ownsHeal)
            {
                servedObject.OnHpIncreased -= PlayHeal;
            }

            if (ownsDeath)
            {
                servedObject.OnDestroyed -= PlayDeath;
            }

            if (ownsAttack)
            {
                servedObject.OnAttack -= PlayAttack;
            }
        }
    }
}
