using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Global.Sound.BGM
{
    public class BGMPlayer : SingletonObject<BGMPlayer>
    {
        private const float FadeDuration = 0.6f;

        private AudioSource audioSource;
        private SoundVolumeSetter volumeSetter;
        private Coroutine transition;
        private float fade = 1f;
        private bool overridden;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                // Duplicate instance from the newly loaded scene; it is being destroyed.
                return;
            }

            audioSource = GetComponent<AudioSource>();
            volumeSetter = GetComponent<SoundVolumeSetter>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            PlayBGM();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayBGM();
        }

        private void PlayBGM()
        {
            overridden = false;
            if (audioSource == null)
            {
                return;
            }

            BGMClipContainer bgmClipContainer = FindObjectOfType<BGMClipContainer>();
            AudioClip targetClip = bgmClipContainer != null ? bgmClipContainer.GetBGMClip() : null;

            audioSource.pitch = 1.0f;

            if (targetClip == null)
            {
                StartTransition(FadeOutAndStop());
                return;
            }

            // isPlaying matters as much as the clip: a scene without a container
            // stops playback while leaving the clip assigned, so comparing clips
            // alone would keep the BGM silent forever after coming back.
            if (audioSource.clip == targetClip && audioSource.isPlaying)
            {
                if (!Mathf.Approximately(fade, 1f))
                {
                    StartTransition(FadeTo(1f));
                }
                return;
            }

            StartTransition(SwitchTo(targetClip));
        }

        /// <summary>
        /// Fades to <paramref name="clip"/> until <see cref="RestoreSceneDefault"/> or the next scene load.
        /// A null clip does nothing, so the current BGM keeps playing.
        /// </summary>
        public void PlayOverride(AudioClip clip)
        {
            if (audioSource == null || clip == null)
            {
                return;
            }

            overridden = true;
            if (audioSource.clip == clip && audioSource.isPlaying)
            {
                if (!Mathf.Approximately(fade, 1f))
                {
                    StartTransition(FadeTo(1f));
                }
                return;
            }

            StartTransition(SwitchTo(clip));
        }

        /// <summary>Returns to the scene's own track. Does nothing unless <see cref="PlayOverride"/> is active.</summary>
        public void RestoreSceneDefault()
        {
            if (!overridden)
            {
                return;
            }

            PlayBGM();
        }

        public void SetPitch(float pitch)
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.pitch = pitch;
        }

        private void StartTransition(IEnumerator routine)
        {
            if (transition != null)
            {
                StopCoroutine(transition);
            }
            transition = StartCoroutine(routine);
        }

        private IEnumerator SwitchTo(AudioClip targetClip)
        {
            if (audioSource.isPlaying)
            {
                yield return FadeTo(0f);
            }

            audioSource.clip = targetClip;
            SetFade(0f);
            audioSource.Play();
            yield return FadeTo(1f);
        }

        private IEnumerator FadeOutAndStop()
        {
            if (audioSource.isPlaying)
            {
                yield return FadeTo(0f);
            }
            audioSource.Stop();
        }

        private IEnumerator FadeTo(float target)
        {
            float start = fade;
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFade(Mathf.Lerp(start, target, elapsed / FadeDuration));
                yield return null;
            }
            SetFade(target);
        }

        private void SetFade(float value)
        {
            fade = Mathf.Clamp01(value);
            if (volumeSetter != null)
            {
                volumeSetter.Multiplier = fade;
                return;
            }
            audioSource.volume = fade;
        }
    }
}
