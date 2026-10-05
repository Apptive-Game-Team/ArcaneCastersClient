using System.Collections.Generic;
using System.Linq;
using GameScene.Exception;
using GameScene.ServedObjectComponent;
using Global;

namespace GameScene.Object
{
    public class ObjectContainer : LocalSingletonObject<ObjectContainer>
    {
        private readonly Dictionary<int, ServedObject> objects = new Dictionary<int, ServedObject>();

        public void RegisterObject(ServedObject obj)
        {
            if (obj == null)
            {
                throw new System.ArgumentNullException(nameof(obj), "ServedObject to register cannot be null.");
            }

            if (!objects.TryAdd(obj.id, obj))
            {
                WDebug.LogWarning($"Object with ID {obj.id} already exists in the container. Ignoring registration.");
                throw new DuplicatedException($"Object with ID {obj.id} already exists.");
            }
            WDebug.Log("Registered object with ID: " + obj.id);
        }

        public void UnregisterObject(int id)
        {
            if (objects.Remove(id, out ServedObject servedObject))
            {
                if (servedObject != null && servedObject.gameObject != null)
                {
                    Destroy(servedObject.gameObject);
                    WDebug.Log($"Unregistered and destroyed object with ID: {id}");
                }
                else
                {
                    WDebug.Log($"Unregistered object with ID: {id}. GameObject was already destroyed or null.");
                }
            }
            else
            {
                WDebug.LogWarning($"Attempted to unregister an object with ID {id} that was not in the container.");
            }
        }

        public void Clear()
        {
            foreach (var obj in objects.Values)
            {
                if (obj != null && obj.gameObject != null)
                {
                    Destroy(obj.gameObject);
                }
            }
            objects.Clear();
            WDebug.Log("Cleared all objects from the container.");
        }
        
        public bool IsExist(int id)
        {
            return objects.ContainsKey(id);
        }

        public ServedObject FindById(int id)
        {
            objects.TryGetValue(id, out ServedObject obj);
            return obj;
        }

        /// <summary>
        /// 등록된 몸 전체. 값 컬렉션의 struct enumerator 를 그대로 내주므로 foreach 가 할당하지 않는다.
        /// 매 프레임 훑는 경로(<see cref="PlacementPreview"/>)는 <see cref="GetIds"/> 의 ToList 대신 이것을 쓴다.
        /// </summary>
        public Dictionary<int, ServedObject>.ValueCollection Values => objects.Values;

        public List<int> GetIds()
        {
            return objects.Keys.ToList();
        }
    }
}