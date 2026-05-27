using System.Collections.Generic;
using Core.Audio.Data;
using UnityEngine;

namespace Core.Audio.Configs
{
    [CreateAssetMenu(fileName = "SoundConfig", menuName = "Configs/Audio/Sound Config")]
    public sealed class SoundConfig : ScriptableObject
    {
        [SerializeField] private List<AudioClip> clips = new();
        [SerializeField] private AudioPlaybackSettings settings = new();

        [Header("Randomization")]
        [SerializeField, Range(0f, 3f)] private float pitchShift;
        [SerializeField, Range(0f, 1f)] private float volumeShift;

        public IReadOnlyList<AudioClip> Clips => clips;
        public AudioPlaybackSettings Settings => settings;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Count == 0)
                return null;

            return clips[Random.Range(0, clips.Count)];
        }

        public AudioPlaybackSettings CreatePlaybackSettings()
        {
            AudioPlaybackSettings result = settings.Clone();

            if (pitchShift > 0f)
                result.Pitch += Random.Range(-pitchShift, pitchShift);

            if (volumeShift > 0f)
                result.Volume *= Random.Range(1f - volumeShift, 1f + volumeShift);

            result.Volume = Mathf.Clamp01(result.Volume);
            result.Pitch = Mathf.Clamp(result.Pitch, -3f, 3f);

            return result;
        }
    }
}