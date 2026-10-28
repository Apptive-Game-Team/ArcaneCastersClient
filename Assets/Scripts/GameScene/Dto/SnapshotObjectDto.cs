using System;
using System.Collections.Generic;
using GameScene.Dto.debug;

namespace GameScene.Dto
{
    [Serializable]
    public class SnapshotObjectDto
    {
        public int id;
        public string prefab;
        public float x;
        public float y;
        public float z;
        public string master;
        public string status;
        public List<string> effects;
        public List<Gizmo> gizmos;
        public List<Gauge> gauges;
    }
}
