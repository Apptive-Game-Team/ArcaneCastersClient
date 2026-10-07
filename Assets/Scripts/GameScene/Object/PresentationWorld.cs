using System.Collections.Generic;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene.Object
{
    /// <summary>An isolated owner for recorded presentation. Never replaces live scene singletons.</summary>
    public sealed class PresentationWorld : MonoBehaviour
    {
        private readonly Dictionary<int, ServedObject> objects = new();
        private IReadOnlyDictionary<string, float> parameters;
        private Transform content;
        private Transform initialization;
        public Camera Camera { get; set; }
        public bool IsClearing { get; private set; }
        public IReadOnlyDictionary<int, ServedObject> Objects => objects;
        public float DamageFlashInterval { get; set; }

        public static PresentationWorld For(Component component) => component != null ? component.GetComponentInParent<PresentationWorld>(true) : null;
        public static Camera CameraFor(Component component) => For(component)?.Camera ?? UnityEngine.Camera.main;
        public static ServedObject Find(int id, PresentationWorld world = null) => world != null ? world.FindById(id) : ObjectContainer.Instance?.FindById(id);
        public ServedObject FindById(int id) => objects.TryGetValue(id, out ServedObject obj) ? obj : null;
        public Vector3 ToWorld(Vector3 recorded) => transform.TransformPoint(recorded);
        public bool TryParameter(string owner, string key, out float value)
        {
            value = 0f;
            return parameters != null && parameters.TryGetValue(owner + "." + key, out value);
        }
        public void SetParameters(IReadOnlyDictionary<string, float> values) => parameters = values;
        public void Register(ServedObject obj) => objects.Add(obj.id, obj);
        public void Unregister(int id)
        {
            if (objects.Remove(id, out ServedObject obj) && obj != null) Destroy(obj.gameObject);
        }

        private void EnsureRoots()
        {
            if (content != null) return;
            content = new GameObject("PresentationContent").transform;
            content.SetParent(transform, false);
            initialization = new GameObject("InactiveInitialization").transform;
            initialization.SetParent(content, false);
            initialization.gameObject.SetActive(false);
        }

        public GameObject InstantiateInactive(GameObject prefab, Vector3 worldPosition, Quaternion rotation)
        {
            EnsureRoots();
            return Instantiate(prefab, worldPosition, rotation, initialization);
        }

        public void Activate(GameObject obj)
        {
            EnsureRoots();
            // Suppress only scene/input/audio dependencies, not prefab presentation behaviors.
            foreach (MonoBehaviour component in obj.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is GameScene.Player.PlayerNameSetter nameSetter) nameSetter.SuppressPreviewName();
                if (component is Selectable || component is GameScene.Player.PlayerNameSetter ||
                    component is GameScene.ServedObjectComponent.Sound.ServedObjectSfxController)
                    component.enabled = false;
            }
            foreach (AudioSource source in obj.GetComponentsInChildren<AudioSource>(true))
            {
                source.playOnAwake = false;
                source.mute = true;
                source.enabled = false;
            }
            foreach (Collider collider in obj.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            obj.transform.SetParent(content, true);
            obj.SetActive(true);
        }

        public GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (IsClearing) return null;
            GameObject obj = InstantiateInactive(prefab, position, rotation);
            Activate(obj);
            return obj;
        }

        public void Own(GameObject obj)
        {
            EnsureRoots();
            obj.transform.SetParent(content, true);
        }

        public void Clear()
        {
            IsClearing = true;
            objects.Clear();
            if (content != null)
            {
                foreach (OnDestroySpawner spawner in content.GetComponentsInChildren<OnDestroySpawner>(true)) spawner.SuppressPresentation = true;
                content.gameObject.SetActive(false);
                Destroy(content.gameObject);
            }
            content = initialization = null;
            IsClearing = false;
        }

        private void OnDestroy() => Clear();
    }
}
