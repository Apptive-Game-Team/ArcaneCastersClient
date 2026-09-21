using UnityEngine;

namespace GameScene
{
    /// <summary>
    /// 화면비가 바뀌어도 게임판이 잘리지 않게 카메라의 세로 FOV 를 맞춘다.
    ///
    /// 카메라의 FOV Axis 가 Vertical 이라 세로 FOV 가 고정이고 가로 시야는 화면비에
    /// 비례한다. 화면이 세로로 길어지면(화면비가 기준보다 작아지면) 가로 시야가 그만큼
    /// 좁아져 판 양옆이 화면 밖으로 나간다. 4:3 에서는 16:9 때의 0.75 배만 보인다.
    ///
    /// 기준 화면비에서의 가로 시야를 하한으로 두고, 그보다 좁아지는 구간에서만 세로 FOV 를
    /// 넓혀 가로 시야를 메운다. 기준보다 넓은 화면에서는 아무것도 하지 않으므로 지금 폰에서
    /// 보이는 화면은 그대로다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraAspectFitter : MonoBehaviour
    {
        [SerializeField] private float referenceAspect = 16f / 9f;
        [SerializeField] private float referenceVerticalFov = 25f;

        private Camera targetCamera;
        private float lastAspect = -1f;

        private void Awake()
        {
            targetCamera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }
            Apply();
        }

        /// <summary>화면 크기는 실행 중에도 바뀐다. 창 크기 조절과 기기 회전이 모두 여기로 들어온다.</summary>
        private void Update()
        {
            if (targetCamera == null)
            {
                return;
            }

            if (!Mathf.Approximately(targetCamera.aspect, lastAspect))
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (targetCamera == null || referenceAspect <= 0f)
            {
                return;
            }

            lastAspect = targetCamera.aspect;

            if (lastAspect >= referenceAspect)
            {
                targetCamera.fieldOfView = referenceVerticalFov;
                return;
            }

            float halfVertical = referenceVerticalFov * 0.5f * Mathf.Deg2Rad;
            float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * referenceAspect);
            targetCamera.fieldOfView =
                2f * Mathf.Atan(Mathf.Tan(halfHorizontal) / lastAspect) * Mathf.Rad2Deg;
        }
    }
}
