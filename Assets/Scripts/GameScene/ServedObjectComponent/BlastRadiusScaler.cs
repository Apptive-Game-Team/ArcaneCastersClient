using Global;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// 서버가 생성 payload 에 실어 보낸 폭발 radius 에 맞춰 폭발 prefab 전체를 키우거나 줄인다.
    /// database 의 <c>magma_explosion.radius</c> 를 0.5 에서 1.5 로 올리면 그림도 3배가 된다.
    /// <para>
    /// radius 는 gizmo 에서 읽는다. <c>AreaOfEffect</c> 를 먼저 보고, 없으면 <c>Collider</c> 를
    /// 본다. 둘 다 없으면 prefab 의 scale 을 그대로 둔다. 서버 <c>GameObject.start()</c> 는
    /// PrefabInitializer 의 <c>addCollider</c> 로 <c>Collider</c> gizmo 를 넣고, 미뤄 둔
    /// component 를 components 에 합친 뒤 <c>Explode.start()</c> 를 돌려 <c>AreaOfEffect</c>
    /// gizmo 를 넣는다. <c>ObjectsInfoDtoBuilder.createGameObject</c> 가 그 뒤에 gizmo 목록을
    /// 복사하므로, <c>addComponent</c> 로 미뤄 넣는 ShockOverload 까지 포함해 세 prefab 모두
    /// 두 gizmo 를 다 싣고 온다. <c>Collider</c> fallback 은 Explode 를 나중에 붙이는 prefab 이
    /// 생겼을 때를 위한 것이다. 세 prefab 모두 collider 와 Explode 가 같은 <c>radius</c>
    /// parameter 를 읽으므로 두 gizmo 의 값은 같다.
    /// </para>
    /// <para>
    /// scale 은 받은 radius 를 <see cref="referenceRadius"/> 로 나눈 값이고, x 와 y 에 같은
    /// 배율을 쓴다. <c>AuraRadiusScaler</c> 처럼 sprite 의 <c>rect</c> 와 <c>pixelsPerUnit</c>
    /// 에서 뽑지 않는 이유는, 저쪽 그림은 원 한 개라서 그림의 가로 반폭이 곧 그 원의 radius 지만
    /// 폭발 그림은 그렇지 않기 때문이다. 불기둥이 위로 솟고 투명한 여백도 frame 마다 다르다 —
    /// <c>MagmaExplosionStrike1</c> 부터 <c>4</c> 까지 불투명한 가장 넓은 줄의 폭이 각각 2.02,
    /// 2.14, 2.15, 1.96 world 단위다. 그림이 그려진 radius 는 사람이 한 번 재서
    /// <see cref="referenceRadius"/> 에 적어 두는 수밖에 없다. 두 축을 따로 늘리면 불기둥이
    /// 눌리거나 늘어나므로 같은 배율을 쓴다.
    /// </para>
    /// <para>
    /// 땅의 원과 서 있는 sprite 의 관계는 <c>.agents/docs/scene-space.md</c> 에 적힌 그대로다.
    /// camera 는 X 로 45° 기울어 있고 sprite 는 world XY 평면에 선다. 45° 에서는 sine 과 cosine
    /// 이 같아서, XY 평면에 선 반지름 r 의 원과 땅 XZ 평면에 누운 반지름 r 의 원이 같은 타원으로
    /// 맺힌다. 가로는 아예 줄어들지 않으므로 sprite 의 가로 반폭이 곧 땅의 radius 다. camera 의
    /// X 회전이 45° 가 아니게 되면 이 대응이 깨진다.
    /// </para>
    /// <para>
    /// Awake 가 아니라 <c>OnBound</c> 에서 도는 이유는 gizmo 가 <c>ObjectSpawner</c> 의
    /// <c>SetGizmos</c> 로 들어온 뒤라야 읽히기 때문이다. 같은 prefab 의
    /// <c>SpriteFrameAnimator</c> 는 sprite 만 바꾸고 scale 은 건드리지 않고,
    /// <c>ShadowVisualizer</c> 는 <c>Shadow</c> 라는 이름의 child 가 없어 LateUpdate 에서 바로
    /// 돌아간다. <c>PopupBookVisualPresenter.Attach</c> 는 <c>actual</c> 의 localScale 을 읽고
    /// 되돌려 놓지만 <c>BindListeners</c> 보다 먼저 끝난다. 그래서 여기서 쓴 scale 이 나중에
    /// 덮이지 않는다.
    /// </para>
    /// <para>
    /// 이 component 를 <c>actual</c> 이 아니라 root 에 붙이는 이유도 <c>Attach</c> 에 있다.
    /// <c>Attach</c> 는 sprite 아래 모서리에 spawn 연출의 회전 축을 놓는데, 그 높이를 prefab
    /// scale 로 재서 고정한다. root 를 키우면 그 축까지 같은 배율로 따라 커져서 축이 계속 sprite
    /// 아래 모서리에 있지만, <c>actual</c> 만 키우면 축만 제자리에 남아 어긋난다.
    /// </para>
    /// <para>
    /// <c>Bind</c> 는 한 번만 부르므로 배율도 한 번만 곱한다. prefab 의 원래 scale 은 Awake 에서
    /// 따로 담아 두고 거기에 곱하므로, 두 번 불려도 값이 겹쳐 쌓이지 않는다.
    /// </para>
    /// </summary>
    public class BlastRadiusScaler : ServedObjectBehaviour
    {
        /// <summary>서버 <c>GizmoCategory</c> 의 이름 그대로다. <c>Explode.start</c> 가 넣는다.</summary>
        private const string AreaOfEffectCategory = "AreaOfEffect";

        /// <summary>서버 <c>GameObject.addCollider</c> 가 <c>CircleCollider</c> 마다 넣는 category 다.</summary>
        private const string ColliderCategory = "Collider";

        /// <summary>
        /// 그림이 그려진 radius, world 단위. prefab scale 1 일 때 이 그림이 덮는 땅의 반지름이다.
        /// 받은 radius 를 이 값으로 나눈 배율을 prefab scale 에 곱한다.
        /// </summary>
        [SerializeField] private float referenceRadius = 1f;

        private Vector3 prefabScale = Vector3.one;
        private bool prefabScaleCaptured;

        private void Awake()
        {
            CapturePrefabScale();
        }

        protected override void OnBound()
        {
            CapturePrefabScale();

            if (referenceRadius <= Mathf.Epsilon)
            {
                WDebug.LogWarning(
                    $"BlastRadiusScaler on '{name}' has referenceRadius {referenceRadius}; " +
                    "keeping the prefab scale.");
                return;
            }

            if (!TryResolveRadius(out float radius))
            {
                // gizmo 가 하나도 없는 생성(동기화 복구 등)에서는 prefab scale 을 그대로 둔다.
                return;
            }

            transform.localScale = prefabScale * (radius / referenceRadius);
        }

        private void CapturePrefabScale()
        {
            if (prefabScaleCaptured)
            {
                return;
            }

            prefabScale = transform.localScale;
            prefabScaleCaptured = true;
        }

        private bool TryResolveRadius(out float radius)
        {
            radius = 0f;
            if (Owner == null)
            {
                return false;
            }

            return Owner.TryGetGizmoRadius(AreaOfEffectCategory, out radius)
                   || Owner.TryGetGizmoRadius(ColliderCategory, out radius);
        }
    }
}
