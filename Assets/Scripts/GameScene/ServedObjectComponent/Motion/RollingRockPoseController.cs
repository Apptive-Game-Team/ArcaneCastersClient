using GameScene.Object;
using UnityEngine;

namespace GameScene.ServedObjectComponent.Motion
{
    public sealed class RollingRockPoseController : ServedObjectBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private Sprite[] rollFrames;

        private Transform presentation;
        private Camera sceneCamera;
        private Vector3 lastServerGround;
        private Vector3 lastRenderedGround;
        private float circumference;
        private float phase;
        private bool correcting;

        protected override void OnBound()
        {
            if (targetRenderer == null || targetRenderer.sprite == null || rollFrames == null || rollFrames.Length == 0)
            {
                enabled = false;
                return;
            }

            presentation = Owner.PresentationWorld?.transform;
            sceneCamera = PresentationWorld.CameraFor(this);
            lastRenderedGround = lastServerGround = LocalGround(transform.position);
            circumference = targetRenderer.sprite.rect.width / targetRenderer.sprite.pixelsPerUnit * Mathf.PI;
            targetRenderer.flipX = false;
            Owner.OnServerPositionUpdated += HandleServerPosition;
        }

        protected override void OnUnbound()
        {
            if (Owner != null)
            {
                Owner.OnServerPositionUpdated -= HandleServerPosition;
            }
        }

        private void HandleServerPosition(Vector3 position)
        {
            // PositionUpdater는 새 보간 전에 직전 목표 좌표에 맞춘다.
            if (correcting)
            {
                lastRenderedGround = lastServerGround;
            }

            position.y = 0f;
            // 20Hz에서 초당 8칸 이동과 반사 시 0.05칸 이격을 함께 허용한다.
            float correctionDistance = Mathf.Max(0.55f, 12f * GameConfig.FrameDuration);
            correcting = (position - lastServerGround).sqrMagnitude > correctionDistance * correctionDistance;
            lastServerGround = position;
        }

        private void LateUpdate()
        {
            if (Owner == null || Owner.IsDespawning)
            {
                return;
            }

            // 방향별로 자세 순서를 바꿔서 화면 왼쪽 위 조명을 유지한다.
            targetRenderer.flipX = false;
            Vector3 ground = LocalGround(transform.position);
            Vector3 step = ground - lastRenderedGround;
            lastRenderedGround = ground;
            if (correcting || step.sqrMagnitude < 0.00000001f)
            {
                return;
            }

            Vector2 travel = CameraPlaneStep(step);
            float distance = travel.magnitude;
            float direction = Mathf.Abs(travel.x) > distance * 0.05f ? Mathf.Sign(travel.x) : Mathf.Sign(travel.y);
            phase = Mathf.Repeat(phase + direction * distance / circumference, 1f);
            int frame = Mathf.RoundToInt(phase * rollFrames.Length) % rollFrames.Length;
            Sprite pose = rollFrames[frame];
            if (pose != targetRenderer.sprite)
            {
                targetRenderer.sprite = pose;
            }
        }

        private Vector3 LocalGround(Vector3 worldPosition)
        {
            Vector3 ground = presentation != null ? presentation.InverseTransformPoint(worldPosition) : worldPosition;
            ground.y = 0f;
            return ground;
        }

        private Vector2 CameraPlaneStep(Vector3 step)
        {
            if (sceneCamera == null)
            {
                return new Vector2(step.x, step.z);
            }

            Vector3 right = sceneCamera.transform.right;
            Vector3 up = sceneCamera.transform.up;
            if (presentation != null)
            {
                right = presentation.InverseTransformDirection(right);
                up = presentation.InverseTransformDirection(up);
            }
            return new Vector2(Vector3.Dot(step, right), Vector3.Dot(step, up));
        }
    }
}
