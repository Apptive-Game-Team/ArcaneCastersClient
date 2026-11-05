using DG.Tweening;
using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Effect
{
    /// <summary>
    /// Two reactions to taking damage, driven by different signals.
    /// <para>
    /// The recoil follows any HP drop, including sources with no attacker such as damage over time.
    /// HP drops that arrive while a recoil is still playing, or within <see cref="recoilCooldown"/> of
    /// the last one, are skipped: restarting the bounce on every tick stacked scale tweens on the same
    /// transform and kept the sprite wobbling for as long as the damage lasted.
    /// The star only plays for a server <c>hit</c> event, which is what tells us where the blow came
    /// from, and it is placed on the side of the sprite facing the attacker.
    /// </para>
    /// </summary>
    public class HitEffectController : MonoBehaviour
    {
        [SerializeField] private ServedObject servedObject;

        [Tooltip("How far toward the attacker the star sits, as a fraction of the sprite's half size.")]
        [SerializeField, Range(0f, 1f)] private float attackerSideBias = 0.6f;

        [Tooltip("Seconds from the start of one recoil before another HP drop may start the next.")]
        [SerializeField, Min(0f)] private float recoilCooldown = 0.5f;

        private Sequence _recoil;
        private float _recoilStartedAt = float.NegativeInfinity;

        private void Start()
        {
            if (servedObject == null)
            {
                servedObject = GetComponentInParent<ServedObject>();
            }

            if (servedObject != null)
            {
                servedObject.OnHpDecreased += PlayRecoil;
            }
        }

        private void OnDestroy()
        {
            if (servedObject != null)
            {
                servedObject.OnHpDecreased -= PlayRecoil;
            }
        }

        public void PlayHitFrom(Vector3 attackerWorldPosition)
        {
            PlayHitFrom(attackerWorldPosition, "HitEffect");
        }

        public void PlayHitFrom(Vector3 attackerWorldPosition, string effectName)
        {
            if (servedObject == null)
            {
                PlayHit(effectName);
                return;
            }

            WDebug.Log($"Hit effect played for ServedObject ID: {servedObject.id}");
            DamagedObjectEffect.SetSelfDestroyEffect(
                effectName,
                servedObject.GetEdgeWorldPositionTowards(attackerWorldPosition, attackerSideBias), servedObject.PresentationWorld);
        }

        /// <summary>Used when the attacker is no longer on the client, so there is no side to favour.</summary>
        public void PlayHit()
        {
            PlayHit("HitEffect");
        }

        private void PlayHit(string effectName)
        {
            DamagedObjectEffect.SetSelfDestroyEffect(effectName, transform.position, GameScene.Object.PresentationWorld.For(this));
        }

        private void PlayRecoil()
        {
            if (_recoil != null && _recoil.IsActive() && _recoil.IsPlaying())
            {
                return;
            }

            if (Time.time - _recoilStartedAt < recoilCooldown)
            {
                return;
            }

            _recoilStartedAt = Time.time;
            _recoil = DOTweenAction.BounceMob(transform);
        }
    }
}
