using System;
using System.Collections.Generic;
using Global;
using UnityEngine;
using UnityEngine.UI;

namespace GameScene.ServedObjectComponent
{
    public class ServedObjectEffectRenderer
    {
        private const string NoEffect = "None";
        private const float StackedEffectAlpha = 0.65f;

        private readonly Func<string, Transform> effectParentProvider;
        private readonly Func<float> objectSizeProvider;
        private readonly float scaleReferenceHeight;
        private readonly float scaleMultiplier;
        private readonly float scaleMin;
        private readonly float scaleMax;
        private readonly List<GameObject> effectInstances = new List<GameObject>();
        private readonly List<string> activeEffects = new List<string>();

        public ServedObjectEffectRenderer(
            Func<string, Transform> effectParentProvider,
            Func<float> objectSizeProvider,
            float scaleReferenceHeight,
            float scaleMultiplier,
            float scaleMin,
            float scaleMax)
        {
            this.effectParentProvider = effectParentProvider;
            this.objectSizeProvider = objectSizeProvider;
            this.scaleReferenceHeight = scaleReferenceHeight;
            this.scaleMultiplier = scaleMultiplier;
            this.scaleMin = scaleMin;
            this.scaleMax = scaleMax;
        }

        public void SetEffects(List<string> effects)
        {
            List<string> normalizedEffects = NormalizeEffects(effects);
            if (AreSameEffects(activeEffects, normalizedEffects))
            {
                return;
            }

            ClearEffects();
            activeEffects.AddRange(normalizedEffects);

            HashSet<string> spawnedResourceNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (string effect in activeEffects)
            {
                string resourceName = effect;
                if (spawnedResourceNames.Add(resourceName))
                {
                    SpawnEffect(effect, resourceName);
                }
            }
        }

        private void SpawnEffect(string effect, string resourceName)
        {
            // Knockback is presented by the authoritative position update, not a status decal.
            if (effect == "Knockback") return;
            // Resolved per effect, not once for the whole list: only the names the object anchors
            // (the element auras on the player) go to the anchor, everything else stays on the
            // object, and a missing parent skips just this one effect instead of the whole list.
            Transform effectParent = effectParentProvider?.Invoke(effect);
            if (effectParent == null)
            {
                return;
            }

            GameObject effectPrefab = Resources.Load<GameObject>($"Prefabs/Effects/{resourceName}");
            if (effectPrefab == null)
            {
                WDebug.LogWarning($"Effect prefab '{resourceName}' for '{effect}' not found.");
                return;
            }

            var world = GameScene.Object.PresentationWorld.For(effectParent);
            GameObject effectInstance;
            if (world != null)
            {
                effectInstance = world.InstantiateInactive(effectPrefab, effectParent.position, effectParent.rotation);
                world.Activate(effectInstance);
                effectInstance.transform.SetParent(effectParent, false);
            }
            else effectInstance = UnityEngine.Object.Instantiate(effectPrefab, effectParent);
            effectInstance.transform.localPosition = Vector3.zero;
            effectInstance.transform.localRotation = Quaternion.identity;
            ApplyEffectScale(effectInstance.transform);
            ApplyEffectAlpha(effectInstance);
            effectInstances.Add(effectInstance);
        }

        private void ClearEffects()
        {
            foreach (GameObject effectInstance in effectInstances)
            {
                if (effectInstance != null)
                {
                    UnityEngine.Object.Destroy(effectInstance);
                }
            }

            effectInstances.Clear();
            activeEffects.Clear();
        }

        private static List<string> NormalizeEffects(List<string> effects)
        {
            List<string> normalizedEffects = new List<string>();
            if (effects == null)
            {
                return normalizedEffects;
            }

            foreach (string effect in effects)
            {
                if (string.IsNullOrWhiteSpace(effect))
                {
                    continue;
                }

                string normalizedEffect = effect.Trim();
                if (string.Equals(normalizedEffect, NoEffect, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!normalizedEffects.Contains(normalizedEffect))
                {
                    normalizedEffects.Add(normalizedEffect);
                }
            }

            return normalizedEffects;
        }

        private static bool AreSameEffects(List<string> currentEffects, List<string> nextEffects)
        {
            if (currentEffects.Count != nextEffects.Count)
            {
                return false;
            }

            for (int i = 0; i < currentEffects.Count; i++)
            {
                if (!string.Equals(currentEffects[i], nextEffects[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyEffectScale(Transform effectTransform)
        {
            if (effectTransform == null)
            {
                return;
            }

            float objectSize = objectSizeProvider?.Invoke() ?? 0f;
            if (objectSize <= 0f || scaleReferenceHeight <= 0f)
            {
                effectTransform.localScale = Vector3.one;
                return;
            }

            float scaleFactor = objectSize / scaleReferenceHeight;
            scaleFactor *= scaleMultiplier;
            scaleFactor = Mathf.Clamp(scaleFactor, scaleMin, scaleMax);
            effectTransform.localScale = Vector3.one * scaleFactor;
        }

        private static void ApplyEffectAlpha(GameObject effectInstance)
        {
            foreach (SpriteRenderer spriteRenderer in effectInstance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Color color = spriteRenderer.color;
                color.a = StackedEffectAlpha;
                spriteRenderer.color = color;
            }

            foreach (Graphic graphic in effectInstance.GetComponentsInChildren<Graphic>(true))
            {
                Color color = graphic.color;
                color.a = StackedEffectAlpha;
                graphic.color = color;
            }
        }
    }
}
