using UnityEngine;

namespace GameScene.ServedObjectComponent
{
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float frameInterval = 0.12f;
        [SerializeField] private bool loop = true;

        private int frameIndex;
        private float elapsed;
        private float playbackStarted;
        private float playbackDuration;
        private bool reversePlayback;

        private void Awake()
        {
            ApplyFrame(0);
        }

        internal void PlayOnce(float duration, bool reverse = false)
        {
            playbackStarted = Time.time;
            playbackDuration = duration;
            reversePlayback = reverse;
            ApplyFrame(reverse && frames != null ? frames.Length - 1 : 0);
        }

        private void Update()
        {
            if (frames == null || frames.Length <= 1)
            {
                return;
            }

            if (playbackDuration > 0f)
            {
                float progress = Mathf.Clamp01((Time.time - playbackStarted) / playbackDuration);
                int nextIndex = Mathf.Min((int)(progress * frames.Length), frames.Length - 1);
                if (reversePlayback)
                {
                    nextIndex = frames.Length - 1 - nextIndex;
                }

                if (frameIndex != nextIndex) ApplyFrame(nextIndex);
                return;
            }

            if (frameInterval <= 0f)
            {
                return;
            }

            elapsed += Time.deltaTime;
            while (elapsed >= frameInterval)
            {
                elapsed -= frameInterval;
                int nextIndex = frameIndex + 1;
                if (nextIndex >= frames.Length)
                {
                    nextIndex = loop ? 0 : frames.Length - 1;
                }

                ApplyFrame(nextIndex);
            }
        }

        private void ApplyFrame(int index)
        {
            if (!spriteRenderer || frames == null || frames.Length == 0)
            {
                return;
            }

            frameIndex = Mathf.Clamp(index, 0, frames.Length - 1);
            Sprite frame = frames[frameIndex];
            if (frame)
            {
                spriteRenderer.sprite = frame;
            }
        }
    }
}
