using Core.Audio.Data;
using UnityEngine;

namespace Core.Audio.Runtime
{
    /// <summary>
    /// One pooled SFX object.
    /// Wraps a GameObject + AudioSource pair.
    /// </summary>
    internal sealed class AudioInstance
    {
        public int Id { get; private set; }
        public GameObject GameObject { get; }
        public AudioSource Source { get; }

        public bool IsPaused { get; private set; }
        public float LocalVolume { get; private set; } = 1f;
        public float StartedAt { get; private set; }

        public int Priority => Source != null ? Source.priority : 256;

        public AudioInstance(GameObject gameObject, AudioSource source)
        {
            GameObject = gameObject;
            Source = source;
        }

        /// <summary>
        /// Prepares this instance for new playback.
        /// </summary>
        public void Prepare(
            int id,
            AudioClip clip,
            Vector3 position,
            Quaternion rotation,
            AudioPlaybackSettings settings,
            float globalVolume,
            float startedAt)
        {
            Id = id;
            StartedAt = startedAt;
            IsPaused = false;

            GameObject.transform.SetPositionAndRotation(position, rotation);
            GameObject.SetActive(true);

            Source.clip = clip;

            ApplySettings(settings, globalVolume);
        }

        /// <summary>
        /// Stops playback and returns this instance to inactive state.
        /// </summary>
        public void Reset()
        {
            Id = AudioConstants.InvalidId;
            IsPaused = false;
            StartedAt = 0f;
            LocalVolume = 1f;

            if (Source != null)
            {
                Source.Stop();
                Source.clip = null;
                Source.loop = false;
                Source.volume = 1f;
                Source.pitch = 1f;
            }

            if (GameObject != null)
                GameObject.SetActive(false);
        }

        public void Pause()
        {
            IsPaused = true;
            Source.Pause();
        }

        public void Resume()
        {
            IsPaused = false;
            Source.UnPause();
        }

        public void SetLocalVolume(float volume, float globalVolume)
        {
            LocalVolume = Mathf.Clamp01(volume);
            RefreshVolume(globalVolume);
        }

        public void RefreshVolume(float globalVolume)
        {
            Source.volume = LocalVolume * Mathf.Clamp01(globalVolume);
        }

        public void SetPitch(float pitch)
        {
            Source.pitch = Mathf.Clamp(pitch, -3f, 3f);
        }

        public void SetPosition(Vector3 position)
        {
            GameObject.transform.position = position;
        }

        public void ApplySettings(AudioPlaybackSettings settings, float globalVolume)
        {
            if (settings == null)
                return;

            LocalVolume = Mathf.Clamp01(settings.Volume);

            Source.outputAudioMixerGroup = settings.OutputMixerGroup;
            Source.mute = settings.Mute;
            Source.bypassEffects = settings.BypassEffects;
            Source.bypassListenerEffects = settings.BypassListenerEffects;
            Source.bypassReverbZones = settings.BypassReverbZones;
            Source.loop = settings.Loop;
            Source.priority = settings.Priority;
            Source.volume = LocalVolume * Mathf.Clamp01(globalVolume);
            Source.pitch = Mathf.Clamp(settings.Pitch, -3f, 3f);
            Source.panStereo = settings.StereoPan;
            Source.spatialBlend = settings.SpatialBlend;
            Source.reverbZoneMix = settings.ReverbZoneMix;
            Source.dopplerLevel = settings.DopplerLevel;
            Source.spread = settings.Spread;
            Source.rolloffMode = settings.RolloffMode;
            Source.minDistance = Mathf.Max(0f, settings.MinDistance);
            Source.maxDistance = Mathf.Max(Source.minDistance, settings.MaxDistance);
        }
    }
}