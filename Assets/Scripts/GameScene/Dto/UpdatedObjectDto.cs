using System.Collections.Generic;
using UnityEngine;

namespace GameScene.Dto
{
    [System.Serializable]
    public class UpdatedObjectDto
    {
        public int id;
        public Vector3 position;
        public string master;
        public string status;
        public List<string> effects;
        public List<Gauge> gauges;

        public UpdatedObjectDto() { }

        public UpdatedObjectDto(SnapshotObjectDto snapshotObjectDto)
        {
            id = snapshotObjectDto.id;
            master = snapshotObjectDto.master;
            position = new Vector3(snapshotObjectDto.x, snapshotObjectDto.y, snapshotObjectDto.z);
            status = snapshotObjectDto.status;
            effects = snapshotObjectDto.effects;
            gauges = snapshotObjectDto.gauges;
        }
    }
}
