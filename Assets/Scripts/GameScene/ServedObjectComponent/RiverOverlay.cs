using GameScene.Dto;
using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// 강 맵의 물과 다리 그림을 한 번만 그리는 overlay. 물 그림 한 장과 같은 다리 그림 두 장을 땅에 눕혀 놓는다.
    /// 위치와 크기는 모두 <see cref="RiverOverlayLayout"/> 에서 읽는다. 서버 object 가 아니라서
    /// <see cref="PositionUpdater"/> 가 옮기지 않으므로 높이 <see cref="RiverOverlayLayout.GroundLift"/> 는 만들 때 한 번만 준다.
    /// 생명은 <see cref="RiverOverlayMember"/> 가 센다.
    /// </summary>
    public class RiverOverlay : MonoBehaviour
    {
        private const int WaterSortingOrder = -2;
        private const int BridgeSortingOrder = -1;

        [SerializeField] private Sprite waterSprite;
        [SerializeField] private Sprite bridgeSprite;

        private void Awake()
        {
            GroundRectangle water = RiverOverlayLayout.WaterRectangle;
            // 물 sprite 의 pivot 은 왼쪽 아래다.
            CreatePiece("Water", waterSprite, water.MinX, water.MinZ, WaterSortingOrder);

            for (int i = 0; i < RiverOverlayLayout.BridgeCount; i++)
            {
                GroundRectangle bridge = RiverOverlayLayout.BridgeRectangle(i);
                // 다리 sprite 의 pivot 은 가운데다.
                CreatePiece($"Bridge{i}", bridgeSprite, bridge.CenterX, bridge.CenterZ, BridgeSortingOrder);
            }
        }

        private void CreatePiece(string pieceName, Sprite sprite, float x, float z, int sortingOrder)
        {
            GameObject piece = new GameObject(pieceName);
            piece.transform.SetParent(transform, false);
            // x 로 90도 눕히면 sprite 의 위쪽이 +z 가 되고 앞면이 위를 본다. 기존 river prefab 과 같다.
            piece.transform.localPosition = new Vector3(x, RiverOverlayLayout.GroundLift, z);
            piece.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            SpriteRenderer pieceRenderer = piece.AddComponent<SpriteRenderer>();
            pieceRenderer.sprite = sprite;
            pieceRenderer.sortingOrder = sortingOrder;
        }
    }
}
