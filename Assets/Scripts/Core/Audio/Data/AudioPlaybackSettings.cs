using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Audio.Data
{
    [Serializable]
    public sealed class AudioPlaybackSettings
    {
        public AudioMixerGroup OutputMixerGroup;

        public bool Mute;
        public bool BypassEffects;
        public bool BypassListenerEffects;
        public bool BypassReverbZones;
        public bool Loop;

        [Range(0, 256)] public int Priority = 128;
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(-3f, 3f)] public float Pitch = 1f;
        [Range(-1f, 1f)] public float StereoPan;
        [Range(0f, 1f)] public float SpatialBlend = 1f;
        [Range(0f, 1.1f)] public float ReverbZoneMix = 1f;
        [Range(0f, 5f)] public float DopplerLevel = 1f;
        [Range(0f, 360f)] public float Spread;

        public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;

        [Min(0f)] public float MinDistance = 1f;
        [Min(0f)] public float MaxDistance = 500f;

        public AudioPlaybackSettings Clone()
        {
            return new AudioPlaybackSettings
            {
                OutputMixerGroup = OutputMixerGroup,
                Mute = Mute,
                BypassEffects = BypassEffects,
                BypassListenerEffects = BypassListenerEffects,
                BypassReverbZones = BypassReverbZones,
                Loop = Loop,
                Priority = Priority,
                Volume = Volume,
                Pitch = Pitch,
                StereoPan = StereoPan,
                SpatialBlend = SpatialBlend,
                ReverbZoneMix = ReverbZoneMix,
                DopplerLevel = DopplerLevel,
                Spread = Spread,
                RolloffMode = RolloffMode,
                MinDistance = MinDistance,
                MaxDistance = MaxDistance
            };
        }
    }
}