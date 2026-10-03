using UnityEngine;

namespace GameScene.ServedObjectComponent.Effect
{
    /// <summary>
    /// Builds the throwaway sprite clones death presentations animate.
    /// <para>
    /// The real <see cref="ServedObject"/> is still removed on the server's schedule; the clone
    /// copies the sprite only — no collider, hp bar, shadow or team indicator — so gameplay timing
    /// and hit feedback are untouched.
    /// </para>
    /// </summary>
    internal static class DeathVisualFactory
    {
        public static GameObject CreateSpriteClone(SpriteRenderer source, string name)
        {
            GameObject clone = new GameObject(name);
            clone.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            clone.transform.localScale = source.transform.lossyScale;
            GameScene.Object.PresentationWorld.For(source)?.Own(clone);

            SpriteRenderer cloneRenderer = clone.AddComponent<SpriteRenderer>();
            CopyRendererState(source, cloneRenderer);
            return clone;
        }

        public static void CopyRendererState(SpriteRenderer source, SpriteRenderer destination)
        {
            destination.sprite = source.sprite;
            destination.color = source.color;
            destination.flipX = source.flipX;
            destination.flipY = source.flipY;
            destination.sharedMaterial = source.sharedMaterial;
            destination.sortingLayerID = source.sortingLayerID;
            destination.sortingOrder = source.sortingOrder;
            destination.maskInteraction = source.maskInteraction;
            destination.spriteSortPoint = source.spriteSortPoint;
        }
    }
}
