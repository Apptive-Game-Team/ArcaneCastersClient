using System.Collections.Generic;
using Data.Sound;
using LobbyScene.SettingPage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sound
{
    public enum GameSfxCategory
    {
        Movement,
        SpawnDeath,
        Attack,
        HitHeal
    }

    public enum GameSfxPriority
    {
        Movement,
        Spawn,
        Attack,
        HitHeal,
        Death
    }

    public class GameSfxPlayer : MonoBehaviour
    {
        private const int OverallCap = 10;
        private static GameSfxPlayer instance;

        private readonly List<Voice> voices = new();
        private readonly Stack<AudioSource> idleSources = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            instance = null;
        }

        public static void Play(
            AudioClip clip,
            GameSfxCategory category,
            GameSfxPriority priority,
            float volume = 1f,
            float pitch = 1f)
        {
            if (clip == null)
            {
                return;
            }

            EnsureInstance().PlayInternal(clip, category, priority, volume, pitch);
        }

        private static GameSfxPlayer EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            // Own host object: sharing one with other systems let unrelated code
            // pick up a voice AudioSource through GetComponent<AudioSource>().
            GameObject host = new GameObject(nameof(GameSfxPlayer));
            DontDestroyOnLoad(host);
            instance = host.AddComponent<GameSfxPlayer>();
            return instance;
        }

        private void Awake()
        {
            SoundDataSetter.OnSoundDataChanged += ApplyVolumeToActiveVoices;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SoundDataSetter.OnSoundDataChanged -= ApplyVolumeToActiveVoices;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Voices outlive their scene otherwise: a death sound could keep
            // playing into ResultScene.
            StopAllVoices();
        }

        private void PlayInternal(
            AudioClip clip,
            GameSfxCategory category,
            GameSfxPriority priority,
            float volume,
            float pitch)
        {
            RemoveCompletedVoices();

            Voice categoryVictim = null;
            if (Count(category) >= GetCategoryCap(category))
            {
                categoryVictim = FindOldestLowerPriorityVoice(category, priority, null);
                if (categoryVictim == null)
                {
                    return;
                }
            }

            Voice overallVictim = null;
            int countAfterCategoryEviction = voices.Count - (categoryVictim != null ? 1 : 0);
            if (countAfterCategoryEviction >= OverallCap)
            {
                overallVictim = FindOldestLowerPriorityVoice(null, priority, categoryVictim);
                if (overallVictim == null)
                {
                    return;
                }
            }

            RemoveVoice(categoryVictim);
            RemoveVoice(overallVictim);

            AudioSource source = RentSource();
            source.pitch = pitch;
            float baseVolume = Mathf.Clamp01(volume);
            source.volume = baseVolume * SoundData.GameScale;
            source.PlayOneShot(clip);
            voices.Add(new Voice(
                source,
                clip,
                category,
                priority,
                baseVolume,
                Time.unscaledTime + clip.length / Mathf.Max(Mathf.Abs(pitch), 0.01f)));
        }

        private void Update()
        {
            RemoveCompletedVoices();
        }

        private AudioSource RentSource()
        {
            while (idleSources.Count > 0)
            {
                AudioSource pooled = idleSources.Pop();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void ReturnSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            idleSources.Push(source);
        }

        private void ApplyVolumeToActiveVoices()
        {
            float scale = SoundData.GameScale;
            foreach (Voice voice in voices)
            {
                if (voice.Source != null)
                {
                    voice.Source.volume = voice.BaseVolume * scale;
                }
            }
        }

        private void StopAllVoices()
        {
            for (int index = voices.Count - 1; index >= 0; index--)
            {
                ReturnSource(voices[index].Source);
            }
            voices.Clear();
        }

        private void RemoveCompletedVoices()
        {
            for (int index = voices.Count - 1; index >= 0; index--)
            {
                Voice voice = voices[index];
                if (voice.Source != null && Time.unscaledTime < voice.EndTime)
                {
                    continue;
                }

                ReturnSource(voice.Source);
                voices.RemoveAt(index);
            }
        }

        private int Count(GameSfxCategory category)
        {
            int count = 0;
            foreach (Voice voice in voices)
            {
                if (voice.Category == category)
                {
                    count++;
                }
            }
            return count;
        }

        private Voice FindOldestLowerPriorityVoice(
            GameSfxCategory? category,
            GameSfxPriority requestedPriority,
            Voice excluded)
        {
            foreach (Voice voice in voices)
            {
                if (voice == excluded ||
                    (category.HasValue && voice.Category != category.Value) ||
                    (int)voice.Priority >= (int)requestedPriority)
                {
                    continue;
                }

                return voice;
            }

            return null;
        }

        private void RemoveVoice(Voice voice)
        {
            if (voice == null)
            {
                return;
            }

            voices.Remove(voice);
            ReturnSource(voice.Source);
        }

        private static int GetCategoryCap(GameSfxCategory category)
        {
            return category switch
            {
                GameSfxCategory.Movement => 2,
                GameSfxCategory.SpawnDeath => 3,
                GameSfxCategory.Attack => 4,
                GameSfxCategory.HitHeal => 4,
                _ => 1
            };
        }

        private sealed class Voice
        {
            public readonly AudioSource Source;
            public readonly AudioClip Clip;
            public readonly GameSfxCategory Category;
            public readonly GameSfxPriority Priority;
            public readonly float BaseVolume;
            public readonly float EndTime;

            public Voice(
                AudioSource source,
                AudioClip clip,
                GameSfxCategory category,
                GameSfxPriority priority,
                float baseVolume,
                float endTime)
            {
                Source = source;
                Clip = clip;
                Category = category;
                Priority = priority;
                BaseVolume = baseVolume;
                EndTime = endTime;
            }
        }
    }
}
