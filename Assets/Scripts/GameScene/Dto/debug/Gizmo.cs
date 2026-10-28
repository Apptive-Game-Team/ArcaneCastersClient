using UnityEngine;

namespace GameScene.Dto.debug
{
    [System.Serializable]
    public class Gizmo
    {
        public Vector3 relativePosition;
        public float radius;
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Include)]
        public Vector3? boxSize;
        public string type; // Circle, Box
        public string category; // Collider, AttackRange, AreaOfEffect, DetectionRange, SpawnArea
    }
}
