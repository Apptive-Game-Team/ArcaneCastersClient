using DG.Tweening;
using GameScene.Dto.Projectile;
using GameScene.ServedObjectComponent;
using UnityEngine;

namespace GameScene.Object.Projectile
{
    /// <summary>
    /// A beam spanning start to end, rather than a shot travelling from one to the other.
    /// The sprite is authored with a left-centre pivot and a nozzle/middle/tip 9-slice (a
    /// left/right spriteBorder in its .meta, spriteMeshType FullRect — the art side's job), so
    /// growing SpriteRenderer.size.x under Tiled draw mode repeats the straight middle zone while
    /// the nozzle and tip keep their authored proportions at every beam length. Tiled mode repeats
    /// along Y as well, so size.y stays at the sprite's own height and the transform carries the
    /// vertical scale that sets the drawn thickness.
    /// </summary>
    public class BeamProjectile : MonoBehaviour, IProjectile
    {
        private const float MinimumLength = 0.01f;
        private const float MinimumDuration = 0.05f;
        private const float GrowInFraction = 0.2f;
        private const float FadeOutFraction = 0.3f;

        [SerializeField] private SpriteRenderer beamRenderer;

        /// <summary>
        /// Height of the sprite rect in world units. It is not the width of the jet: the beam
        /// sprite is mostly empty above and below its water band, which fills 39% of the image, so
        /// 1.6 here draws a jet about 0.62 units thick. The server's SeaSerpentHydroPump hit test
        /// covers 1.0 either side of the line, and the drawn jet is meant to sit inside that.
        /// </summary>
        [SerializeField] private float thickness = 2f;

        private float spriteHeight = 1f;
        private PresentationWorld world;

        public void Init(ProjectileDto projectileDto)
        {
            world = PresentationWorld.For(this);
            Vector3 start = ProjectileUtil.GetPosition(projectileDto.start, world);

            if (!TryGetEnd(projectileDto.end, out Vector3 end))
            {
                Destroy(gameObject);
                return;
            }

            transform.position = start;
            transform.rotation = ProjectileUtil.GetRotation(start, end, world);

            if (beamRenderer == null)
            {
                return;
            }

            // Tiled draw mode needs the sprite's mesh type to be FullRect; a Tight mesh clips the
            // tile to the sprite's opaque silhouette instead of repeating the middle zone.
            if (beamRenderer.drawMode != SpriteDrawMode.Tiled)
            {
                beamRenderer.drawMode = SpriteDrawMode.Tiled;
            }

            // Tiled repeats the sprite along Y too, so a size.y above the sprite's own height
            // stacks that many copies of the water band: 3.2 over a 0.79-unit-tall sprite drew
            // four parallel jets instead of one thick one. size.y therefore stays at the authored
            // height and the transform scales Y to reach the thickness. SpiritBombBeamProjectile
            // hits the same rule and re-creates its Sprite at a matching pixelsPerUnit instead;
            // scaling here keeps the spriteBorder that the .meta authored for the nozzle and tip.
            spriteHeight = beamRenderer.sprite != null ? beamRenderer.sprite.bounds.size.y : 0f;
            if (spriteHeight <= 0f)
            {
                spriteHeight = thickness;
            }

            transform.localScale = new Vector3(1f, thickness / spriteHeight, 1f);

            // The beam lies in the camera plane the same way a stretching arm does (see
            // scene-space.md): a world-space distance is foreshortened along +Z and not along +X,
            // so reach would appear to change with facing. GetCameraPlaneLength matches the length
            // GetRotation already aimed along.
            float length = Mathf.Max(ProjectileUtil.GetCameraPlaneLength(start, end, world), MinimumLength);
            AnimateBeam(length, Mathf.Max(projectileDto.duration, MinimumDuration));
        }

        private void AnimateBeam(float length, float duration)
        {
            Color color = beamRenderer.color;
            beamRenderer.size = new Vector2(0f, spriteHeight);
            beamRenderer.color = new Color(color.r, color.g, color.b, 0f);

            float growIn = duration * GrowInFraction;
            float fadeOut = duration * FadeOutFraction;
            float hold = Mathf.Max(0f, duration - growIn - fadeOut);

            DOTween.Sequence()
                .Append(DOTween.To(() => beamRenderer.size.x,
                        value => beamRenderer.size = new Vector2(value, spriteHeight), length, growIn)
                    .SetEase(Ease.OutQuad))
                .Join(beamRenderer.DOFade(1f, growIn))
                .AppendInterval(hold)
                .Append(beamRenderer.DOFade(0f, fadeOut))
                .SetLink(gameObject)
                .SetTarget(this);
        }

        /// <summary>
        /// The serpent sends a position target for both ends, but ProjectileTarget also allows a
        /// live-object reference, so this resolves either without throwing. A reference whose
        /// object is already gone reports failure instead of drawing a beam into the world origin.
        /// </summary>
        private bool TryGetEnd(ProjectileTarget target, out Vector3 position)
        {
            if (target is ReferenceProjectileTarget reference)
            {
                ServedObject servedObject = PresentationWorld.Find(reference.id, world);
                if (servedObject == null)
                {
                    position = Vector3.zero;
                    return false;
                }

                position = servedObject.transform.position;
                return true;
            }

            position = ProjectileUtil.GetPosition(target, world);
            return true;
        }
    }
}
