using UnityEngine;

namespace Global
{
    /// <summary>
    /// 배경 sprite 를 orthographic 카메라가 보는 범위 전체에 꽉 차게 늘린다. 비율은 유지하고
    /// 넘치는 쪽은 화면 밖으로 잘린다. 화면비는 창 크기 조절과 기기 회전으로 실행 중에도
    /// 바뀌므로 바뀔 때마다 다시 맞춘다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteCameraCover : MonoBehaviour
    {
        [Tooltip("비우면 Camera.main 을 쓴다.")]
        [SerializeField] private Camera targetCamera;

        private SpriteRenderer spriteRenderer;
        private float lastAspect = -1f;
        private float lastOrthographicSize = -1f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            Camera camera = targetCamera != null ? targetCamera : Camera.main;
            if (camera == null || !camera.orthographic || spriteRenderer.sprite == null)
            {
                return;
            }

            if (Mathf.Approximately(camera.aspect, lastAspect)
                && Mathf.Approximately(camera.orthographicSize, lastOrthographicSize))
            {
                return;
            }

            lastAspect = camera.aspect;
            lastOrthographicSize = camera.orthographicSize;

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                return;
            }

            float viewHeight = camera.orthographicSize * 2f;
            float viewWidth = viewHeight * camera.aspect;
            float scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
