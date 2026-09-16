using GameScene.Dto.Projectile;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    /// <summary>
    /// Spirit Bomb 빔을 밑동·가운데·끝 세 조각의 sprite 로 그린다. 가운데 조각만
    /// <see cref="SpriteDrawMode.Tiled"/> 라서 size.x 에 길이를 넣으면 무늬가 늘어나지 않고
    /// 반복되고, size.y 가 그대로 굵기가 된다. StretchProjectile 의 팔에서 가져온 방식이다.
    ///
    /// 그림은 asset 두 장이다. <c>Game/shoot/spirit_bomb_beam_segment</c> 가 가운데에서 반복되는
    /// 띠고, <c>Game/shoot/spirit_bomb_beam_cap</c> 이 밑동과 끝이 함께 쓰는 별이다. 띠는 위아래
    /// 가장자리가 직선이고 좌우 끝의 단면이 같아서 몇 장을 이어 붙여도 이음매가 보이지 않는다.
    /// 두 원소는 띠 안에서 같이 보인다 — 바깥 두 줄이 NATURE 의 풀색, 안쪽 세 줄이 LIGHTNING 의
    /// 금색이고 한가운데가 가장 밝다.
    ///
    /// 이 projection 은 prefab 이 없다. ProjectileSpawner.SpawnSpiritBombBeam 이 빈 GameObject 로
    /// 만들기 때문에 sprite 를 코드에서 조립한다. asset 에 딸린 Sprite 를 그대로 쓰지 않는 이유는
    /// pixelsPerUnit 이다 — asset 쪽은 100 으로 고정인데, tile 이 세로로 한 줄만 깔리려면 sprite
    /// 높이가 그때그때의 굵기와 정확히 같아야 한다. 그래서 texture 만 가져다 Sprite.Create 로
    /// 다시 만든다. Tiled 는 sprite 의 mesh 가 FullRect 일 때만 size 를 읽으므로 FullRect 도 함께
    /// 명시한다.
    ///
    /// 수명은 ProjectileSpawner 가 Destroy(gameObject, dto.duration) 로 잡는다. 스스로 파괴하지
    /// 않는다. 서버는 damage tick 마다 짧은 projection 을 하나씩 보내므로, 이어지는 projection
    /// 들이 4초 빔의 그때그때의 목표 위치를 따라간다.
    /// </summary>
    public class SpiritBombBeamProjectile : MonoBehaviour, IProjectile
    {
        private const string SegmentSpritePath = "Game/shoot/spirit_bomb_beam_segment";
        private const string CapSpritePath = "Game/shoot/spirit_bomb_beam_cap";

        /// <summary>
        /// 서버가 width 를 정하지 않았을 때 쓰는 굵기, world 단위. LineRenderer 두 가닥으로
        /// 그리던 시절의 굵기와 맞춘 값이다 — 코일 반지름 0.12 의 위아래 합 0.24 에 가닥 굵기
        /// 0.065 를 더하면 0.3 이다. 서버가 보내는 값의 범위는 대략 0.25 ~ 1.0 이라 이 기본값은
        /// 그 범위의 아래쪽에 앉는다.
        /// </summary>
        private const float DefaultWidth = 0.3f;

        /// <summary>
        /// 밑동과 끝의 크기, 굵기에 대한 배수. 끝이 더 커야 착탄으로 읽힌다. 런타임에 그리던
        /// 둥근 빛은 canvas 를 꽉 채워서 1.3 과 1.5 로 충분했지만, 지금 asset 은 여덟 갈래 별이라
        /// 갈래 사이가 비어 있다. 같은 배수로는 띠에 묻혀 보이지 않아 배수를 키웠다.
        /// </summary>
        private const float BaseCapScale = 1.8f;
        private const float TipCapScale = 2.4f;

        private const float PulseSpeed = 9f;
        private const float PulseAmount = 0.08f;
        private const float VisibleLength = 0.01f;

        private const int CoreSortingOrder = 12;
        private const int CapSortingOrder = 13;

        /// <summary>
        /// 시전자 쪽 끝의 높이. sprite 가운데에서 위 끝까지를 1 로 놓은 값이라, 0.6 은 sprite
        /// 높이의 80% 다 — 플레이어 sprite 는 2.2 world 단위라 발밑에서 1.76, 눈 높이쯤이다.
        /// 고정 숫자가 아니라 sprite 에서 읽는 값이라 시전자 sprite 가 바뀌어도 따라간다.
        /// </summary>
        private const float CasterHeightBias = 0.6f;

        /// <summary>
        /// 화면에서 바로 위에 있는 기준점까지의 거리, world 단위.
        /// <see cref="ServedObject.GetEdgeWorldPositionTowards"/> 는 넘긴 점의 방향만 쓰고 거리는
        /// 버리므로 값 자체는 0 보다 크기만 하면 된다.
        /// </summary>
        private const float ScreenUpProbeDistance = 1f;

        /// <summary>
        /// sprite 를 읽을 수 없는 끝을 올리는 거리, world 단위. 서버가 이 빔의 양 끝을 항상
        /// <see cref="ReferenceProjectileTarget"/> 로 보내므로 평소에는 쓰이지 않고, 대상이
        /// 죽어 <see cref="ObjectContainer"/> 에서 빠진 프레임에만 쓰인다. 플레이어 sprite
        /// 높이의 절반이다.
        /// </summary>
        private const float PositionLift = 1.1f;

        private static Texture2D segmentTexture;
        private static Texture2D capTexture;

        private ProjectileTarget startTarget;
        private ProjectileTarget endTarget;
        private Transform baseTransform;
        private Transform tipTransform;
        private SpriteRenderer coreRenderer;
        private SpriteRenderer tipRenderer;
        private Sprite coreSprite;
        private Sprite capSprite;
        private Vector3 lastStartPosition;
        private Vector3 lastEndPosition;
        private ServedObject startObject;
        private ServedObject endObject;
        private float beamWidth;
        private float startedAt;

        public void Init(ProjectileDto projectileDto)
        {
            Texture2D segment = LoadTexture(SegmentSpritePath, ref segmentTexture);
            Texture2D cap = LoadTexture(CapSpritePath, ref capTexture);
            if (segment == null || cap == null)
            {
                // 그릴 것이 없으니 LateUpdate 도 돌 필요가 없다. GameObject 는 그대로 두고
                // ProjectileSpawner 가 예약해 둔 Destroy 가 치운다.
                enabled = false;
                return;
            }

            startTarget = projectileDto.start;
            endTarget = projectileDto.end;
            startedAt = Time.time;
            TryUpdateTarget(startTarget, ref lastStartPosition, ref startObject);
            TryUpdateTarget(endTarget, ref lastEndPosition, ref endObject);

            // 0 이하는 서버가 굵기를 정하지 않았다는 뜻이다. width 를 아직 실어 보내지 않는
            // 서버와도 같이 돌아야 하므로 그때는 기본값으로 대체한다.
            beamWidth = projectileDto.width > 0f ? projectileDto.width : DefaultWidth;

            // pixelsPerUnit 을 굵기에서 거꾸로 계산해 sprite 의 원래 높이가 굵기와 정확히 같게
            // 만든다. 그래야 size.y 를 굵기로 줬을 때 tile 이 세로로 한 줄만 깔리고, 굵기가
            // tile 높이와 어긋나 위아래가 잘리거나 두 줄로 반복되는 일이 없다. tile 하나의
            // 가로 길이도 같은 비율로 따라 늘어나 무늬가 굵기에 비례한다.
            coreSprite = CreateSprite(segment, new Vector2(0f, 0.5f), segment.height / beamWidth);
            capSprite = CreateSprite(cap, new Vector2(0.5f, 0.5f), cap.height / beamWidth);

            baseTransform = CreateRenderer("Base", capSprite, CapSortingOrder).transform;
            coreRenderer = CreateRenderer("Core", coreSprite, CoreSortingOrder);
            tipRenderer = CreateRenderer("Tip", capSprite, CapSortingOrder);
            tipTransform = tipRenderer.transform;

            coreRenderer.drawMode = SpriteDrawMode.Tiled;
            coreRenderer.tileMode = SpriteTileMode.Continuous;

            UpdateBeam();
        }

        // LateUpdate 라서 이번 프레임에 시전자와 목표가 움직인 결과가 이미 반영된 위치를 읽는다.
        private void LateUpdate()
        {
            if (startTarget == null || endTarget == null)
            {
                return;
            }

            UpdateBeam();
        }

        private void OnDestroy()
        {
            // texture 는 Resources 의 asset 이라 건드리지 않는다. sprite 는 굵기마다
            // pixelsPerUnit 이 달라 projection 마다 새로 만들었으므로 그것만 치운다.
            if (coreSprite != null)
            {
                Destroy(coreSprite);
            }
            if (capSprite != null)
            {
                Destroy(capSprite);
            }
        }

        private void UpdateBeam()
        {
            TryUpdateTarget(startTarget, ref lastStartPosition, ref startObject);
            TryUpdateTarget(endTarget, ref lastEndPosition, ref endObject);

            // 서버가 보내는 위치는 sprite 의 발밑이다. sprite pivot 이 아래라
            // (PlayerCharacterBase.png.meta 의 spritePivot 은 y 0.0072) transform.position 은
            // 땅에 닿아 있고, 두 끝을 그대로 이으면 빔이 바닥을 기어 바닥 그림과 발밑에 묻힌다.
            // 그려질 두 점을 sprite 위로 올린 다음에 겨눈다. StretchProjectile.Aim 이 팔을
            // 어깨에서 내보내는 방식과 같다.
            Vector3 from = GetBeamEnd(startObject, lastStartPosition, CasterHeightBias);

            // 대상 쪽은 sprite 가운데다. 고정 높이가 아니라 sprite 에서 읽으므로 같은 코드가
            // slime 의 몸통에도 golem 의 몸통에도 맞는다.
            Vector3 to = endObject != null
                ? endObject.GetEdgeWorldPositionTowards(from, 0f)
                : lastEndPosition + ProjectileUtil.GetScreenUp() * PositionLift;

            // 길이도 회전도 카메라 평면 기준이다. 카메라가 기울어져 있어 Vector3.Distance 는
            // 화면에 그려야 할 길이를 주지 않는다 (.agents/docs/scene-space.md).
            float length = ProjectileUtil.GetCameraPlaneLength(from, to);

            transform.position = from;
            transform.rotation = ProjectileUtil.GetRotation(from, to);

            // size 는 scale 보다 먼저 적용되므로 가운데 조각은 scale 1 을 유지해야 길이가 맞는다.
            coreRenderer.size = new Vector2(length, beamWidth);
            coreRenderer.enabled = length > VisibleLength;
            tipRenderer.enabled = length > VisibleLength;
            tipTransform.localPosition = new Vector3(length, 0f, 0f);

            // 이 빔은 네 번 때리는 동안 같은 자리에 걸려 있어서, 아무것도 움직이지 않으면 정지
            // 화면처럼 보인다. 밑동과 끝만 천천히 키웠다 줄인다.
            float pulse = 1f + Mathf.Sin((Time.time - startedAt) * PulseSpeed) * PulseAmount;
            baseTransform.localScale = Vector3.one * (BaseCapScale * pulse);
            tipTransform.localScale = Vector3.one * (TipCapScale * pulse);
        }

        private SpriteRenderer CreateRenderer(string pieceName, Sprite sprite, int sortingOrder)
        {
            GameObject pieceObject = new GameObject(pieceName);
            pieceObject.transform.SetParent(transform, false);

            // material 을 따로 만들지 않는다. SpriteRenderer 는 기본 sprite material 을 이미
            // 들고 나오고, 그쪽은 Shader.Find 와 달리 WebGL 빌드에서 빠질 일이 없다.
            SpriteRenderer pieceRenderer = pieceObject.AddComponent<SpriteRenderer>();
            pieceRenderer.sprite = sprite;
            pieceRenderer.sortingLayerName = "Default";
            pieceRenderer.sortingOrder = sortingOrder;
            return pieceRenderer;
        }

        /// <summary>
        /// asset 한 장의 texture. Resources.Load 도 안에서 결과를 들고 있지만, 이 두 장은 빔이
        /// 때릴 때마다 필요하므로 static 에 붙잡아 문자열 조회까지 없앤다. Sprite 로 읽어서
        /// texture 만 꺼내는 것은 asset 이 sprite 로 import 됐는지 여기서 함께 확인하기 위해서다.
        /// </summary>
        private static Texture2D LoadTexture(string resourcePath, ref Texture2D cached)
        {
            if (cached != null)
            {
                return cached;
            }

            Sprite loaded = Resources.Load<Sprite>(resourcePath);
            if (loaded == null)
            {
                Debug.LogError($"Spirit bomb beam sprite not found: {resourcePath}");
                return null;
            }

            cached = loaded.texture;
            return cached;
        }

        private static Sprite CreateSprite(Texture2D texture, Vector2 pivot, float pixelsPerUnit)
        {
            // FullRect 가 핵심이다. Sprite.Create 의 기본값인 Tight 는 투명한 가장자리를 깎아낸
            // mesh 를 만들고, 그러면 SpriteRenderer.size 가 무시돼 Tiled 가 동작하지 않는다.
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                pivot,
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "SpiritBombBeamPiece";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>
        /// 빔의 한쪽 끝을 sprite 의 세로 중심선 위, 가운데와 위 끝 사이
        /// <paramref name="heightBias"/> 지점으로 올린다.
        /// <para>
        /// <see cref="ServedObject.GetEdgeWorldPositionTowards"/> 는 넘긴 점의 방향을 화면에서
        /// 읽고 거리는 renderer 자기 축으로 놓는다. 그래서 화면에서 바로 위에 있는 점을 넘기면
        /// 방향이 위로 고정되고, 결과는 sprite 위의 점이 된다 — 높이를 세계 좌표에서 직접
        /// 더하면 기울어진 카메라 때문에 sprite 를 벗어난다 (.agents/docs/scene-space.md).
        /// 방향을 만드는 데 <see cref="ProjectileUtil.GetScreenUp"/> 을 쓰므로 카메라 기울기가
        /// 바뀌어도 따라간다.
        /// </para>
        /// <para>
        /// <paramref name="servedObject"/> 가 없으면 — 대상이 죽어 컨테이너에서 빠진 프레임이다 —
        /// 읽을 sprite 도 없으니 마지막으로 알던 위치를 screen up 으로
        /// <see cref="PositionLift"/> 만큼 올려 쓴다.
        /// </para>
        /// </summary>
        private static Vector3 GetBeamEnd(ServedObject servedObject, Vector3 lastPosition, float heightBias)
        {
            Vector3 screenUp = ProjectileUtil.GetScreenUp();
            if (servedObject == null)
            {
                return lastPosition + screenUp * PositionLift;
            }

            Vector3 center = servedObject.GetEdgeWorldPositionTowards(lastPosition, 0f);
            return servedObject.GetEdgeWorldPositionTowards(center + screenUp * ScreenUpProbeDistance, heightBias);
        }

        /// <summary>
        /// 한쪽 끝이 가리키는 <see cref="ServedObject"/> 와 그 위치를 이번 프레임 값으로 새로
        /// 읽는다. 대상이 죽어 컨테이너에서 빠지면 <paramref name="servedObject"/> 는 null 이 되고
        /// <paramref name="lastPosition"/> 은 마지막으로 알던 값으로 남는다 — 빔이 원점으로
        /// 접히지 않고 그 자리에서 끝난다.
        /// </summary>
        private static void TryUpdateTarget(
            ProjectileTarget target,
            ref Vector3 lastPosition,
            ref ServedObject servedObject)
        {
            switch (target)
            {
                case PositionProjectileTarget position:
                    lastPosition = position.ToVector3();
                    servedObject = null;
                    break;
                case ReferenceProjectileTarget reference:
                    servedObject = ObjectContainer.Instance.FindById(reference.id);
                    if (servedObject != null)
                    {
                        lastPosition = servedObject.transform.position;
                    }
                    break;
            }
        }
    }
}
