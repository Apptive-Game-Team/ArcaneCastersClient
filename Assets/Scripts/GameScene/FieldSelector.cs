using System.Collections.Generic;
using Data;
using Data.GameConfig;
using Data.Magic;
using GameScene.Card;
using GameScene.Player;
using GameScene.ServedObjectComponent;
using Global;
using Global.Sound;
using Sound;
using UnityEngine;

namespace GameScene
{
    /// <summary>
    /// 필드 선택 모드에서 조준/사거리/스킬 인디케이터를 마우스에 맞춰 갱신한다.
    /// <para>
    /// 이 Update는 마우스를 따라가야 하므로 프레임당 비용이 곧 체감 반응성이다.
    /// 씬 전역 탐색, GetComponent, UI 레이캐스트처럼 매 프레임 반복할 이유가 없는 작업은
    /// 캐시하거나 실제로 필요한 시점까지 미룬다.
    /// </para>
    /// </summary>
    public class FieldSelector : MonoBehaviour
    {
        private const int RangeIndicatorSortingOrder = 5;
        private const int AimIndicatorSortingOrder = 16;
        private const float AimIndicatorRadius = 0.18f;

        /// <summary>
        /// layer 하나가 먹는 sorting order 폭. <see cref="SkillIndicatorShapeRenderer"/> 가
        /// 테두리를 채움보다 1 위에 두므로 2 씩 띄워야 layer 끼리 섞이지 않는다.
        /// </summary>
        private const int SkillIndicatorSortingStep = 2;

        /// <summary>
        /// indicator layer 가 쓸 수 있는 가장 낮은 sorting order. 사거리 원이 5 라 그 위여야 한다.
        /// </summary>
        private const int LowestSkillIndicatorSortingOrder = 6;

        /// <summary>
        /// circle layer 가 문서에 <c>edgeWidth</c> 를 적지 않았을 때 쓰는 기본 테두리 두께.
        /// magma_explosion 처럼 <c>edgeWidth</c> 가 없는 document 도 이 두께의 테두리를 얻어,
        /// 사거리 원·조준 원과 같은 채움만으로는 경계가 안 보이던 문제를 없앤다.
        /// </summary>
        private const float DefaultSkillIndicatorLayerEdgeWidth = 0.15f;

        /// <summary>참조를 찾지 못했을 때 씬 전체 스캔을 매 프레임 되풀이하지 않기 위한 재시도 간격.</summary>
        private const float MissingReferenceRetryInterval = 0.5f;

        CardInputSender cardInputSender;
        private GameObject currentAimObj;
        private GameObject currentRangeObj;

        // 인디케이터 컴포넌트는 생성 시점에 잡아 둔다. 매 프레임 GetComponent를 부를 이유가 없다.
        private SkillIndicatorShapeRenderer aimShapeRenderer;
        private SkillIndicatorShapeRenderer rangeShapeRenderer;

        // indicator document 는 layer 를 여러 개 담을 수 있다. layer 마다 GameObject 를 하나씩 쓰되,
        // 프레임마다 만들고 부수지 않고 pool 에 남겨 두었다가 남는 것은 비활성화만 한다.
        // 도형이 바뀌어도 SkillIndicatorShapeRenderer 가 같은 mesh 버퍼를 다시 채우므로 재사용해도 된다.
        private readonly List<GameObject> skillIndicatorLayers = new List<GameObject>(4);
        private readonly List<SkillIndicatorShapeRenderer> skillIndicatorLayerRenderers =
            new List<SkillIndicatorShapeRenderer>(4);

        // Resolve 가 매 프레임 여기에 결과를 담는다. list 를 새로 만들지 않아야 GC 가 생기지 않는다.
        private readonly List<ResolvedIndicatorShape> resolvedShapes = new List<ResolvedIndicatorShape>(4);

        // 매 프레임 다시 구할 필요가 없는 참조/결과 캐시.
        private Camera cachedCamera;
        private ServedObject cachedCaster;
        private Vector3 cachedCasterSourcePosition;
        private Vector3 cachedCasterGroundPosition;
        private bool hasCachedCasterGroundPosition;
        private float nextCasterSearchTime;
        private float nextGroundColliderSearchTime;

        // 경고 로그가 필드 선택 중 매 프레임 쏟아지지 않도록 상태 전환에서만 남긴다.
        private bool warnedMissingCaster;
        private bool warnedMissingMagicData;
        private string warnedMissingRangeMagicName;

        private string lastLoggedMagicName;
        private float lastLoggedRange;
        private float lastLoggedRadius;
        private bool hasLoggedMagicParameters;

        [SerializeField] private GameObject aimObject;
        [SerializeField] private GameObject rangeObject;
        [SerializeField] private Collider groundCollider;
        [SerializeField] private string groundObjectName = "PopupBookGround";
        private AudioSource interactionAudioSource;

        void Start()
        {
            cardInputSender = FindObjectOfType<CardInputSender>();
            interactionAudioSource = gameObject.GetComponent<AudioSource>();
            if (interactionAudioSource == null)
            {
                interactionAudioSource = gameObject.AddComponent<AudioSource>();
            }
            SoundVolumeSetter.Attach(interactionAudioSource, SoundVolumeSetter.SoundType.UI);
            currentAimObj = CreateAimIndicator(out aimShapeRenderer);
            currentRangeObj = CreateRangeIndicator(out rangeShapeRenderer);
            currentAimObj.SetActive(false);
            currentRangeObj.SetActive(false);
        }

        void Update()
        {
            if (!CardInputSender.Instance.IsFieldSelectMode())
            {
                if (currentAimObj.activeSelf) currentAimObj.SetActive(false);
                if (currentRangeObj.activeSelf) currentRangeObj.SetActive(false);
                HideSkillIndicatorLayersFrom(0);

                return;
            }

            if (!currentAimObj.activeSelf) currentAimObj.SetActive(true);
            if (!currentRangeObj.activeSelf) currentRangeObj.SetActive(true);


            if (!TryGetCurrentMagicParameters(out CombinedMagicData magicData, out float range, out float radius))
            {
                if (currentRangeObj.activeSelf) currentRangeObj.SetActive(false);
                HideSkillIndicatorLayersFrom(0);
                return;
            }
            LogMagicParametersIfChanged(magicData, range, radius);

            Vector3 casterPosition = GetCasterPosition();
            rangeShapeRenderer.SetCircle(casterPosition, range, true, RangeIndicatorSortingOrder, 0f);

            if (!TryGetGroundPosition(Input.mousePosition, out Vector3 mouseWorldPos))
            {
                return;
            }

            Vector3 previewPosition = ClampToRange(mouseWorldPos, casterPosition, range);
            aimShapeRenderer.SetCircle(previewPosition, AimIndicatorRadius, true, AimIndicatorSortingOrder, 0f);

            MagicIndicatorResolver.Resolve(
                magicData,
                casterPosition,
                previewPosition,
                MagicIndicatorResolver.GetForwardDirection(),
                range,
                resolvedShapes);
            DrawSkillIndicatorLayers(resolvedShapes);

            // UI 레이캐스트는 클릭을 걸러내는 용도뿐이므로, 실제로 버튼을 뗀 프레임에만 수행한다.
            if (!Input.GetMouseButtonUp(0))
            {
                return;
            }

            if (PointerInputUtility.IsPointerOverUiOrSelectable()) return;

            if (!CardInputSender.Instance.TrySendInput(previewPosition))
            {
                return;
            }

            interactionAudioSource.PlayOneShot(SoundAssets.FieldConfirm);

            currentAimObj.SetActive(false);
            currentRangeObj.SetActive(false);
            HideSkillIndicatorLayersFrom(0);
            CardInputSender.Instance.SetExpectedMagicUI();
        }

        private Vector3 GetCasterPosition()
        {
            ServedObject caster = ResolveCaster();
            if (caster == null)
            {
                if (!warnedMissingCaster)
                {
                    warnedMissingCaster = true;
                    WDebug.LogWarning($"[FieldSelector] Could not find caster object for {SceneContext.Me}.");
                }
                return currentRangeObj.transform.position;
            }

            warnedMissingCaster = false;
            Vector3 position = caster.transform.position;

            // 캐스터가 그대로면 지면 투영 레이캐스트를 다시 쏠 이유가 없다.
            if (hasCachedCasterGroundPosition && cachedCasterSourcePosition == position)
            {
                return cachedCasterGroundPosition;
            }

            // 투영에 실패했을 때는 캐시하지 않는다. 지면 콜라이더가 뒤늦게 잡히면 그때 제대로 투영되어야 한다.
            if (!TryProjectToGround(position, out Vector3 groundPosition))
            {
                hasCachedCasterGroundPosition = false;
                return position;
            }

            cachedCasterSourcePosition = position;
            cachedCasterGroundPosition = groundPosition;
            hasCachedCasterGroundPosition = true;
            return cachedCasterGroundPosition;
        }

        /// <summary>
        /// 캐스터를 찾는 씬 스캔은 비싸므로 결과를 들고 있는다.
        /// 아직 못 찾았을 때(사망/미스폰)만 일정 간격으로 다시 시도한다.
        /// </summary>
        private ServedObject ResolveCaster()
        {
            if (cachedCaster != null)
            {
                return cachedCaster;
            }

            if (Time.unscaledTime < nextCasterSearchTime)
            {
                return null;
            }

            nextCasterSearchTime = Time.unscaledTime + MissingReferenceRetryInterval;
            cachedCaster = FindPlayerObject(SceneContext.Me);
            hasCachedCasterGroundPosition = false;
            return cachedCaster;
        }

        private static ServedObject FindPlayerObject(string master)
        {
            PlayerNameSetter[] playerNameSetters = FindObjectsByType<PlayerNameSetter>(FindObjectsSortMode.None);
            foreach (PlayerNameSetter playerNameSetter in playerNameSetters)
            {
                ServedObject servedObject = playerNameSetter.GetComponent<ServedObject>();
                if (servedObject != null && servedObject.GetMaster() == master)
                {
                    return servedObject;
                }
            }

            return null;
        }

        private static Vector3 ClampToRange(Vector3 targetPosition, Vector3 origin, float range)
        {
            float safeRange = Mathf.Max(range, 0f);
            Vector3 flattenedTarget = targetPosition;
            flattenedTarget.y = origin.y;
            Vector3 offset = flattenedTarget - origin;
            if (offset.sqrMagnitude <= safeRange * safeRange)
            {
                return targetPosition;
            }

            Vector3 clampedPosition = origin + offset.normalized * safeRange;
            clampedPosition.y = targetPosition.y;
            return clampedPosition;
        }

        public bool TryGetGroundPosition(Vector3 screenPosition, out Vector3 groundPosition)
        {
            groundPosition = Vector3.zero;
            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            bool hasGroundCollider = TryGetGroundCollider(out Collider resolvedGroundCollider);
            if (hasGroundCollider && resolvedGroundCollider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                groundPosition = hit.point;
                return true;
            }

            Plane groundPlane = hasGroundCollider
                ? new Plane(resolvedGroundCollider.transform.up, resolvedGroundCollider.transform.position)
                : new Plane(Vector3.up, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            groundPosition = ray.GetPoint(distance);
            return true;
        }

        private Camera ResolveCamera()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }

            return cachedCamera;
        }

        private bool TryProjectToGround(Vector3 worldPosition, out Vector3 groundPosition)
        {
            if (!TryGetGroundCollider(out Collider resolvedGroundCollider))
            {
                groundPosition = worldPosition;
                return false;
            }

            Ray downRay = new Ray(worldPosition + Vector3.up * 100f, Vector3.down);
            if (resolvedGroundCollider.Raycast(downRay, out RaycastHit hit, 200f))
            {
                groundPosition = hit.point;
                return true;
            }

            Plane groundPlane = new Plane(resolvedGroundCollider.transform.up, resolvedGroundCollider.transform.position);
            if (groundPlane.Raycast(downRay, out float distance))
            {
                groundPosition = downRay.GetPoint(distance);
                return true;
            }

            groundPosition = worldPosition;
            return false;
        }

        private bool TryGetGroundCollider(out Collider resolvedGroundCollider)
        {
            if (groundCollider != null)
            {
                resolvedGroundCollider = groundCollider;
                return true;
            }

            // 인스펙터 참조가 비어 있어도 씬 전체 스캔이 매 프레임 반복되지는 않게 한다.
            if (Time.unscaledTime < nextGroundColliderSearchTime)
            {
                resolvedGroundCollider = null;
                return false;
            }

            nextGroundColliderSearchTime = Time.unscaledTime + MissingReferenceRetryInterval;

            if (!string.IsNullOrEmpty(groundObjectName))
            {
                GameObject groundObject = GameObject.Find(groundObjectName);
                if (groundObject != null && groundObject.TryGetComponent(out groundCollider))
                {
                    resolvedGroundCollider = groundCollider;
                    return true;
                }
            }

            resolvedGroundCollider = FindObjectByName<Collider>("Ground") ??
                                     FindObjectByName<Collider>("Panel");
            groundCollider = resolvedGroundCollider;
            return resolvedGroundCollider != null;
        }

        private static T FindObjectByName<T>(string namePart) where T : Component
        {
            T[] components = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (T component in components)
            {
                if (component != null && component.name.Contains(namePart, System.StringComparison.OrdinalIgnoreCase))
                {
                    return component;
                }
            }

            return null;
        }

        private bool TryGetCurrentMagicParameters(out CombinedMagicData magicData, out float range, out float radius)
        {
            magicData = null;
            range = 0f;
            radius = 0f;

            if (!CardInputSender.Instance.TryGetCurrentMagicData(out magicData))
            {
                if (!warnedMissingMagicData)
                {
                    warnedMissingMagicData = true;
                    WDebug.LogWarning("[FieldSelector] Could not resolve current magic data.");
                }
                return false;
            }

            warnedMissingMagicData = false;

            if (!GameParameterResolver.TryGetMagicParameter(magicData, "range", out range))
            {
                if (warnedMissingRangeMagicName != magicData.serverName)
                {
                    warnedMissingRangeMagicName = magicData.serverName;
                    WDebug.LogWarning($"[FieldSelector] Could not find range parameter for {magicData.serverName}.");
                }
                return false;
            }

            warnedMissingRangeMagicName = null;
            GameParameterResolver.TryGetMagicParameter(magicData, "radius", out radius);
            return true;
        }

        /// <summary>
        /// 푼 도형을 순서대로 그린다. pool 이 모자라면 그때만 GameObject 를 만들고, 남으면 비활성화만 한다.
        /// 여기서는 list 도 문자열도 새로 만들지 않으므로 프레임마다 GC 가 생기지 않는다.
        /// </summary>
        private void DrawSkillIndicatorLayers(List<ResolvedIndicatorShape> shapes)
        {
            for (int i = 0; i < shapes.Count; i++)
            {
                SkillIndicatorShapeRenderer layerRenderer = GetOrCreateSkillIndicatorLayer(i);
                GameObject layerObject = skillIndicatorLayers[i];
                if (!layerObject.activeSelf) layerObject.SetActive(true);

                ResolvedIndicatorShape shape = shapes[i];
                int sortingOrder = GetLayerSortingOrder(i, shapes.Count);
                if (shape.kind == ResolvedIndicatorShape.Kind.Circle)
                {
                    // document 가 edgeWidth 를 적었으면 그 두께, 안 적었으면 기본 두께로 테두리를 그린다.
                    // 안은 두 경우 모두 채운다 — 테두리만 남기면 지면 위에서 범위가 흐릿하게만 보인다.
                    // 채움은 SkillIndicatorShapeRenderer.LayerFillColor 를 써서 사거리 원·조준 원의
                    // 주황(DefaultFillColor)과 색으로 구분된다.
                    float edgeWidth = shape.edgeWidth > 0f ? shape.edgeWidth : DefaultSkillIndicatorLayerEdgeWidth;
                    layerRenderer.SetCircle(
                        shape.origin, shape.radius, true, sortingOrder, edgeWidth,
                        SkillIndicatorShapeRenderer.LayerFillColor);
                }
                else
                {
                    // SetLine 의 width 는 전체 폭이라 다시 반으로 나눈다.
                    layerRenderer.SetLine(
                        shape.origin, shape.target, shape.length, sortingOrder, shape.halfWidth * 2f,
                        SkillIndicatorShapeRenderer.LayerFillColor);
                }
            }

            HideSkillIndicatorLayersFrom(shapes.Count);
        }

        /// <summary>
        /// layer 무더기를 조준점(<see cref="AimIndicatorSortingOrder"/>) 바로 아래에 붙인다. 마지막 layer 가
        /// 가장 위에 오고, 나머지는 아래로 <see cref="SkillIndicatorSortingStep"/> 씩 내려간다.
        /// layer 가 하나뿐이면 14 가 되어 이 변경 전과 같은 자리다.
        /// 위에서부터 쌓는 이유는 layer 개수가 마법마다 다르기 때문이다. 아래에서부터 쌓으면 layer 가
        /// 둘 이상인 마법에서 위쪽 layer 가 조준점과 <see cref="SelectionGroundIndicator"/>(17) 를 덮는다.
        /// </summary>
        private static int GetLayerSortingOrder(int index, int layerCount)
        {
            int order = AimIndicatorSortingOrder - (layerCount - index) * SkillIndicatorSortingStep;
            return order < LowestSkillIndicatorSortingOrder ? LowestSkillIndicatorSortingOrder : order;
        }

        private SkillIndicatorShapeRenderer GetOrCreateSkillIndicatorLayer(int index)
        {
            // pool 이 커지는 것은 layer 가 지금까지보다 많은 마법을 처음 겨눌 때뿐이다.
            while (skillIndicatorLayers.Count <= index)
            {
                // 도형은 월드 좌표로 그려지고 필드 경계 클리핑도 월드 기준이므로 부모를 두지 않는다.
                GameObject layerObject = new GameObject($"SkillIndicatorLayer{skillIndicatorLayers.Count}");
                skillIndicatorLayers.Add(layerObject);
                skillIndicatorLayerRenderers.Add(layerObject.AddComponent<SkillIndicatorShapeRenderer>());
            }

            return skillIndicatorLayerRenderers[index];
        }

        private void HideSkillIndicatorLayersFrom(int firstUnusedIndex)
        {
            for (int i = firstUnusedIndex; i < skillIndicatorLayers.Count; i++)
            {
                GameObject layerObject = skillIndicatorLayers[i];
                if (layerObject != null && layerObject.activeSelf)
                {
                    layerObject.SetActive(false);
                }
            }
        }

        private static GameObject CreateAimIndicator(out SkillIndicatorShapeRenderer shapeRenderer)
        {
            GameObject indicator = new GameObject("AimIndicator");
            shapeRenderer = indicator.AddComponent<SkillIndicatorShapeRenderer>();
            shapeRenderer.SetLocalCircle(AimIndicatorRadius, AimIndicatorSortingOrder);
            return indicator;
        }

        private static GameObject CreateRangeIndicator(out SkillIndicatorShapeRenderer shapeRenderer)
        {
            GameObject indicator = new GameObject("RangeIndicator");
            shapeRenderer = indicator.AddComponent<SkillIndicatorShapeRenderer>();
            return indicator;
        }

        private void LogMagicParametersIfChanged(CombinedMagicData magicData, float range, float radius)
        {
            // 문자열 키를 만들어 비교하면 매 프레임 문자열이 할당된다. 값 비교로 충분하다.
            if (hasLoggedMagicParameters &&
                lastLoggedMagicName == magicData.serverName &&
                lastLoggedRange == range &&
                lastLoggedRadius == radius)
            {
                return;
            }

            hasLoggedMagicParameters = true;
            lastLoggedMagicName = magicData.serverName;
            lastLoggedRange = range;
            lastLoggedRadius = radius;
            WDebug.Log($"[FieldSelector] magic={magicData.serverName}, range={range:F3}, radius={radius:F3}");
        }
    }
}
