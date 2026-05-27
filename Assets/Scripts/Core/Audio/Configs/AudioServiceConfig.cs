using Core.Audio.Data;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio.Configs
{
    [CreateAssetMenu(fileName = "AudioServiceConfig", menuName = "Configs/Audio/Audio Service Config")]
    public sealed class AudioServiceConfig : ScriptableObject
    {
        [Header("Pool")]
        [Min(1)] public int InitialPoolSize = 10;
        [Min(1)] public int MaxPoolSize = 50;

        [Header("Defaults")]
        public AudioPlaybackSettings DefaultSfxSettings = new();

        [Header("Music")]
        public AudioMixerGroup MusicMixerGroup;
        [Range(0f, 5f)] public float MusicFadeDuration = 1f;

        [Header("Volumes")]
        [Range(0f, 1f)] public float DefaultMasterVolume = 1f;
        [Range(0f, 1f)] public float DefaultMusicVolume = 1f;
        [Range(0f, 1f)] public float DefaultSfxVolume = 1f;

        [Header("Mute")]
        public bool DefaultMasterMuted;
    }
}