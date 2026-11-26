namespace GameScene.Dto.Projectile
{
    [System.Serializable]
    public class ProjectileDto
    {
        public string type;
        public float duration;
        public ProjectileTarget start;
        public ProjectileTarget end;
        public int? sourceId;

        /// <summary>
        /// 빔 계열 projection 의 굵기, world 단위. 0 이하는 서버가 굵기를 정하지 않았다는 뜻이고,
        /// 그때는 받는 쪽이 자기 기본값을 쓴다 — 이 field 를 실어 보내지 않는 서버에서는 값이
        /// 오지 않아 0 으로 남는다. 지금은 SpiritBombBeam 만 읽는다.
        /// </summary>
        public float width;
    }
}
