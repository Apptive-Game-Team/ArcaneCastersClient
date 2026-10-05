using Data.GameConfig;
using Data.Magic;
using GameScene;
using UnityEngine;

namespace TutorialScene
{
    public class TutorialFieldSelector : MonoBehaviour
    {
        private const int RangeIndicatorSortingOrder = 5;
        private const int AimIndicatorSortingOrder = 16;
        private const float AimIndicatorRadius = 0.18f;

        // 옛 내장 표의 Shoot 사거리. parameters 가 도착하기 전에만 쓴다.
        private const float DefaultRange = 18f;

        TutorialCardSender cardInputSender;
        private GameObject currentAimObj;
        private GameObject currentRangeObj;
        private GameObject currentSkillIndicator;
        private bool currentSkillIndicatorIsLine;

        void Start()
        {
            cardInputSender = FindObjectOfType<TutorialCardSender>();
            currentAimObj = CreateAimIndicator();
            currentRangeObj = CreateRangeIndicator();
            currentAimObj.SetActive(false);
            currentRangeObj.SetActive(false);
        
            currentSkillIndicator = CreateCircleSkillIndicator();
            currentSkillIndicatorIsLine = false;
            currentSkillIndicator.SetActive(false);
        }

        void Update()
        {
            if (!cardInputSender.IsFieldSelectMode())
            {
                currentAimObj.SetActive(false);
                currentRangeObj.SetActive(false);
                if (currentSkillIndicator != null) currentSkillIndicator.SetActive(false);

                return;
            }
        
            if (!currentAimObj.activeSelf) currentAimObj.SetActive(true);
            currentRangeObj.SetActive(true);


            // 사거리는 서버 parameters 에서 마법 이름으로 읽는다. parameters 가 아직 안 왔으면
            // 기본값(DefaultRange)으로 그린다.
            CombinedMagicData magic = cardInputSender.GetCurrentMagic();
            if (magic == null)
            {
                currentAimObj.SetActive(false);
                currentRangeObj.SetActive(false);
                if (currentSkillIndicator != null) currentSkillIndicator.SetActive(false);
                return;
            }

            if (!GameParameterResolver.TryGetMagicParameter(magic, "range", out float magicRange))
            {
                magicRange = DefaultRange;
            }

            GameParameterResolver.TryGetMagicParameter(magic, "radius", out float magicRadius);

            Vector3 casterPosition = new Vector3(1f, 0f, 5f);
            SetCircleWorldRadius(currentRangeObj, casterPosition, magicRange);


            bool wantLine = MagicIndicatorResolver.IsLaneAim(magic);


            if (currentSkillIndicator == null || currentSkillIndicatorIsLine != wantLine)
            {
                if (currentSkillIndicator != null) Destroy(currentSkillIndicator);
                currentSkillIndicator = wantLine ? CreateLineSkillIndicator() : CreateCircleSkillIndicator();
                currentSkillIndicatorIsLine = wantLine;
            }


            if (!currentSkillIndicator.activeSelf) currentSkillIndicator.SetActive(true);

            if (!TryGetGroundPosition(Input.mousePosition, out Vector3 mouseWorldPos))
            {
                return;
            }

            Vector3 previewPosition = ClampToRange(mouseWorldPos, casterPosition, magicRange);
            currentAimObj.transform.position = previewPosition;

            UpdateSkillIndicator(wantLine, casterPosition, previewPosition, magicRange, magicRadius);


            if (PointerInputUtility.IsPointerOverUi()) return;

            if (Input.GetMouseButtonUp(0))
            {
                cardInputSender.SendInput(previewPosition);
                currentAimObj.SetActive(false);
                currentRangeObj.SetActive(false);
                currentSkillIndicator.SetActive(false);
                cardInputSender.SetExpectedMagicUI();
            }
        }

        private static Vector3 ClampToRange(Vector3 targetPosition, Vector3 origin, float range)
        {
            float safeRange = Mathf.Max(range, 0f);
            Vector3 offset = targetPosition - origin;
            offset.y = 0f;
            if (offset.sqrMagnitude <= safeRange * safeRange)
            {
                return targetPosition;
            }

            return origin + offset.normalized * safeRange;
        }

        private static bool TryGetGroundPosition(Vector3 screenPosition, out Vector3 groundPosition)
        {
            groundPosition = Vector3.zero;
            Camera camera = Camera.main;
            if (camera == null)
            {
                return false;
            }

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = camera.ScreenPointToRay(screenPosition);
            if (!groundPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            groundPosition = ray.GetPoint(distance);
            groundPosition.y = 0f;
            return true;
        }

        private static void ConfigureAimIndicator(GameObject indicator)
        {
            SkillIndicatorShapeRenderer shapeRenderer = indicator.GetComponent<SkillIndicatorShapeRenderer>();
            if (shapeRenderer == null)
            {
                shapeRenderer = indicator.AddComponent<SkillIndicatorShapeRenderer>();
            }

            if (shapeRenderer != null)
            {
                shapeRenderer.SetLocalCircle(AimIndicatorRadius, AimIndicatorSortingOrder);
            }
        }

        private static void SetCircleWorldRadius(GameObject indicator, Vector3 position, float radius)
        {
            SkillIndicatorShapeRenderer shapeRenderer = indicator.GetComponent<SkillIndicatorShapeRenderer>();
            if (shapeRenderer == null)
            {
                shapeRenderer = indicator.AddComponent<SkillIndicatorShapeRenderer>();
            }

            shapeRenderer.SetCircle(position, radius, true, RangeIndicatorSortingOrder, 0f);
        }

        private void UpdateSkillIndicator(
            bool isLine,
            Vector3 casterPosition,
            Vector3 previewPosition,
            float range,
            float radius)
        {
            if (isLine)
            {
                LineSkillIndicator lineIndicator = currentSkillIndicator.GetComponent<LineSkillIndicator>();
                if (lineIndicator != null)
                {
                    lineIndicator.SetIndicator(casterPosition, previewPosition, range, radius);
                }
                return;
            }

            CircleSkillIndicator circleIndicator = currentSkillIndicator.GetComponent<CircleSkillIndicator>();
            if (circleIndicator != null)
            {
                circleIndicator.SetIndicator(previewPosition, radius);
            }
        }

        private static GameObject CreateAimIndicator()
        {
            GameObject indicator = new GameObject("AimIndicator");
            indicator.AddComponent<SkillIndicatorShapeRenderer>();
            ConfigureAimIndicator(indicator);
            return indicator;
        }

        private static GameObject CreateRangeIndicator()
        {
            GameObject indicator = new GameObject("RangeIndicator");
            indicator.AddComponent<SkillIndicatorShapeRenderer>();
            return indicator;
        }

        private static GameObject CreateLineSkillIndicator()
        {
            GameObject indicator = new GameObject("LineSkillIndicator");
            indicator.AddComponent<LineSkillIndicator>();
            return indicator;
        }

        private static GameObject CreateCircleSkillIndicator()
        {
            GameObject indicator = new GameObject("CircleSkillIndicator");
            indicator.AddComponent<CircleSkillIndicator>();
            return indicator;
        }
    }
}
