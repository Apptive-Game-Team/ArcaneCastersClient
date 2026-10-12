namespace GameScene.Dto
{
    /// <summary>
    /// 강 맵 overlay 의 그림이 땅 위 어디를 덮는지 정하는 순수 규칙. 장면에 의존하지 않아서 EditMode test 가 직접 부른다.
    /// 물 그림은 서버의 물 칸(열 8 과 9, 행 0 4 5 9)과 다리 칸(행 1 2 3 6 7 8)을 한꺼번에 덮고,
    /// 부드러운 둑이 열 8 과 10 의 경계선 밖으로 0.25 unit 까지 나온다. 모든 그림은 256 pixels per unit 이다.
    /// </summary>
    public static class RiverOverlayLayout
    {
        public const float PixelsPerUnit = 256f;

        /// <summary>서버는 y = 0 으로 보내고 땅 mesh 도 y = 0 이라서, 그림을 이만큼 띄워 깜빡임을 막는다.</summary>
        public const float GroundLift = 0.02f;

        public const int WaterPixelWidth = 1024;
        public const int WaterPixelHeight = 2560;
        public const int BridgePixelSize = 1024;

        /// <summary>물 그림의 왼쪽 아래 모서리 world 좌표. 그림의 맨 위 줄이 z = 10, 맨 아래 줄이 z = 0 이다.</summary>
        public const float WaterMinX = 7f;
        public const float WaterMinZ = 0f;

        /// <summary>두 다리가 놓이는 x 중심. 강의 두 열(8 과 10 의 경계선 사이) 한가운데이다.</summary>
        public const float BridgeCenterX = 9f;

        public const int BridgeCount = 2;

        public static float WaterWidth => WaterPixelWidth / PixelsPerUnit;

        public static float WaterDepth => WaterPixelHeight / PixelsPerUnit;

        public static float BridgeSize => BridgePixelSize / PixelsPerUnit;

        /// <summary>물 그림이 덮는 땅 위 사각형. x 7 부터 11, z 0 부터 10.</summary>
        public static GroundRectangle WaterRectangle =>
            new GroundRectangle(WaterMinX, WaterMinZ, WaterMinX + WaterWidth, WaterMinZ + WaterDepth);

        /// <summary>
        /// 다리 그림의 중심 z. 0 번은 행 1 부터 3 의 다리(열린 구간 z 1 부터 4)로 중심이 2.5 이고,
        /// 1 번은 행 6 부터 8 의 다리(z 6 부터 9)로 중심이 7.5 이다.
        /// </summary>
        public static float BridgeCenterZ(int index)
        {
            switch (index)
            {
                case 0: return 2.5f;
                case 1: return 7.5f;
                default: throw new System.ArgumentOutOfRangeException(nameof(index));
            }
        }

        /// <summary>다리 그림이 덮는 땅 위 사각형. 중심이 (9, 중심 z) 이고 한 변이 4 unit 이다.</summary>
        public static GroundRectangle BridgeRectangle(int index)
        {
            float half = BridgeSize / 2f;
            float centerZ = BridgeCenterZ(index);
            return new GroundRectangle(BridgeCenterX - half, centerZ - half, BridgeCenterX + half, centerZ + half);
        }
    }

    /// <summary>땅(x, z 평면) 위의 축에 나란한 사각형.</summary>
    public readonly struct GroundRectangle
    {
        public readonly float MinX;
        public readonly float MinZ;
        public readonly float MaxX;
        public readonly float MaxZ;

        public GroundRectangle(float minX, float minZ, float maxX, float maxZ)
        {
            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
        }

        public float CenterX => (MinX + MaxX) / 2f;

        public float CenterZ => (MinZ + MaxZ) / 2f;
    }
}
