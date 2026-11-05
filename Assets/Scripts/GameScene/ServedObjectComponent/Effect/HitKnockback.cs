using UnityEngine;

namespace GameScene.ServedObjectComponent.Effect
{
    /// <summary>
    /// Pushes the sprite of a <see cref="ServedObject"/> away from its attacker and lets it settle back.
    /// <para>
    /// Only <see cref="ServedObject.GetActualTransform"/> moves. The root transform is the server's
    /// position and stays where <c>PositionUpdater</c> put it, so the match is unaffected.
    /// </para>
    /// <para>
    /// The offset is written without a tween: <see cref="Update"/> (ordered before every other script)
    /// takes last frame's offset back out, and <see cref="LateUpdate"/> adds this frame's offset on top.
    /// Any other script or tween that writes the sprite's <c>localPosition</c> in between, such as
    /// <c>DOTweenAction.Hop</c>, therefore sees the sprite at its normal position. A hit during an
    /// earlier push starts from the current offset and the total is capped at
    /// <see cref="MaxDistance"/>, so a crowd of attackers shakes the target instead of shoving it away.
    /// The component disables itself when the sprite is back home, so idle objects pay nothing.
    /// </para>
    /// The push direction is a ground-plane (XZ) vector, so it needs no camera correction: the sprite
    /// is billboarded but its position still lives on the ground plane.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class HitKnockback : MonoBehaviour
    {
        public const float PushDistance = 0.15f;
        public const float MaxDistance = 0.2f;
        public const float PushSeconds = 0.06f;
        public const float ReturnSeconds = 0.12f;

        private Transform _sprite;
        private Vector3 _appliedLocal;
        private Vector3 _worldOffset;
        private Vector3 _pushFrom;
        private Vector3 _pushTo;
        private float _startedAt;

        /// <summary>Starts a push of the sprite away from <paramref name="attackerWorldPosition"/>.</summary>
        public static void Play(ServedObject target, Vector3 attackerWorldPosition)
        {
            if (target == null)
            {
                return;
            }

            Transform sprite = target.GetActualTransform();
            if (sprite == null || sprite == target.transform)
            {
                return;
            }

            if (!target.TryGetComponent(out HitKnockback knockback))
            {
                knockback = target.gameObject.AddComponent<HitKnockback>();
            }

            knockback.Push(sprite, attackerWorldPosition);
        }

        private void Push(Transform sprite, Vector3 attackerWorldPosition)
        {
            Vector3 away = transform.position - attackerWorldPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _sprite = sprite;
            _pushFrom = _worldOffset;
            _pushTo = Vector3.ClampMagnitude(_worldOffset + away.normalized * PushDistance, MaxDistance);
            _startedAt = Time.time;
            enabled = true;
        }

        private void Update()
        {
            RemoveApplied();
        }

        private void LateUpdate()
        {
            if (_sprite == null)
            {
                _worldOffset = Vector3.zero;
                enabled = false;
                return;
            }

            float elapsed = Time.time - _startedAt;
            if (elapsed >= PushSeconds + ReturnSeconds)
            {
                _worldOffset = Vector3.zero;
                enabled = false;
                return;
            }

            _worldOffset = elapsed < PushSeconds
                ? Vector3.Lerp(_pushFrom, _pushTo, 1f - Mathf.Pow(1f - elapsed / PushSeconds, 2f))
                : Vector3.Lerp(_pushTo, Vector3.zero, Mathf.SmoothStep(0f, 1f, (elapsed - PushSeconds) / ReturnSeconds));

            Transform parent = _sprite.parent;
            _appliedLocal = parent != null ? parent.InverseTransformVector(_worldOffset) : _worldOffset;
            _sprite.localPosition += _appliedLocal;
        }

        private void OnDisable()
        {
            RemoveApplied();
        }

        private void RemoveApplied()
        {
            if (_sprite != null && _appliedLocal != Vector3.zero)
            {
                _sprite.localPosition -= _appliedLocal;
            }

            _appliedLocal = Vector3.zero;
        }
    }
}
