using System.Collections.Generic;
using GameScene.Dto.debug;
using UnityEngine;

namespace GameScene.Dto
{
    [System.Serializable]
    public class CreatedObjectDto
    {
        public int id;
        public string master;
        public Vector3 position;
        public string type; // enum 대응 가능
        public List<Gizmo> gizmos;

        public CreatedObjectDto()
        {
        
        }
    
        public CreatedObjectDto(SnapshotObjectDto objectDto)
        {
            id = objectDto.id;
            master = objectDto.master;
            position = new Vector3(objectDto.x, objectDto.y, objectDto.z);
            type = objectDto.prefab;
            gizmos = objectDto.gizmos;
        }
    }
}