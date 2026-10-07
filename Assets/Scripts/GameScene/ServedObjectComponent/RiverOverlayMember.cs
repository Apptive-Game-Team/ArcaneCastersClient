using System.Collections.Generic;
using GameScene.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameScene.ServedObjectComponent
{
    /// <summary>
    /// RiverWater 와 RiverBridge prefab 에 붙인다. 살아 있는 river object 를 세다가, 첫 번째가 깨어날 때
    /// <see cref="RiverOverlay"/> 를 만들고 마지막이 파괴될 때 같이 파괴한다. 그래서 object 가 어떤 순서로 와도
    /// overlay 는 한 번만 있고, match 가 끝나 object 가 모두 사라지면 남지 않으며, 같은 scene 의 다시 하기도 같은 길로 처리된다.
    /// 녹화 재생처럼 <see cref="PresentationWorld"/> 가 따로 있으면 world 마다 센다. world 안의 overlay 는 world 의 자식이다.
    /// </summary>
    public class RiverOverlayMember : MonoBehaviour
    {
        private const string OverlayResource = "Prefabs/RiverOverlay";

        private class Entry
        {
            public int Count;
            public GameObject Overlay;
        }

        // 실제 경기(PresentationWorld 없음)는 key 가 null 이라 sentinel 로 바꾼다.
        private static readonly object LiveKey = new object();
        private static readonly Dictionary<object, Entry> Entries = new Dictionary<object, Entry>();

        private object key;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Entries.Clear();
        }

        public static int LiveCount(PresentationWorld world = null)
        {
            return Entries.TryGetValue((object)world ?? LiveKey, out Entry entry) ? entry.Count : 0;
        }

        private void Awake()
        {
            PresentationWorld world = PresentationWorld.For(this);
            key = (object)world ?? LiveKey;
            if (!Entries.TryGetValue(key, out Entry entry))
            {
                entry = new Entry();
                Entries[key] = entry;
            }

            entry.Count++;
            // 이미 있어도 scene 이 먼저 내려가 overlay 만 사라졌을 수 있어서, 비어 있으면 다시 만든다.
            if (entry.Overlay == null)
            {
                entry.Overlay = CreateOverlay(world, gameObject.scene);
            }
        }

        private void OnDestroy()
        {
            if (key == null || !Entries.TryGetValue(key, out Entry entry))
            {
                return;
            }

            entry.Count--;
            if (entry.Count > 0)
            {
                return;
            }

            if (entry.Overlay != null)
            {
                Destroy(entry.Overlay);
            }

            Entries.Remove(key);
        }

        private static GameObject CreateOverlay(PresentationWorld world, Scene scene)
        {
            GameObject prefab = Resources.Load<GameObject>(OverlayResource);
            if (prefab == null)
            {
                Debug.LogError($"Missing runtime prefab: {OverlayResource}");
                return null;
            }

            GameObject overlay = world != null
                ? Instantiate(prefab, world.transform, false)
                : Instantiate(prefab);
            overlay.name = "RiverOverlay";
            if (world == null && scene.IsValid() && scene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(overlay, scene);
            }

            return overlay;
        }
    }
}
