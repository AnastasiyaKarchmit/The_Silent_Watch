using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Audio.Configs;
using Core.Audio.Contracts;
using Core.Audio.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Core.Audio.Runtime
{
     /// <summary>
    /// Root-level audio service.
    /// 
    /// Responsibilities:
    /// - exposes simple public API for music and SFX;
    /// - owns music volume and SFX volume;
    /// - creates the persistent AudioService GameObject;
    /// - delegates music logic to MusicAudioChannel;
    /// - delegates pooled SFX logic to SfxAudioPool;
    /// - ticks the SFX pool through VContainer ITickable.
    ///
    /// Register this service in RootLifetimeScope as an entry point.
    /// </summary>
    public sealed class AudioService : IAudioService, IInitializable, ITickable, IDisposable
    {
        private readonly AudioServiceConfig _config;

        private GameObject _root;
        private MusicAudioChannel _musicChannel;
        private SfxAudioPool _sfxPool;

        private bool _isInitialized;
        private bool _isDisposed;

        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        public bool IsMasterMuted { get; private set; }
        private float EffectiveMasterVolume => IsMasterMuted ? 0f : MasterVolume;
        

        public event Action<float> MasterVolumeChanged;
        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action<bool> MasterMuteChanged;

        public AudioService(AudioServiceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            MasterVolume = Mathf.Clamp01(_config.DefaultMasterVolume);
            MusicVolume = Mathf.Clamp01(_config.DefaultMusicVolume);
            SfxVolume = Mathf.Clamp01(_config.DefaultSfxVolume);
            IsMasterMuted = _config.DefaultMasterMuted;
        }

        /// <summary>
        /// Called by VContainer after construction.
        /// Creates the persistent audio root and initializes music/SFX channels.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            _root = new GameObject("AudioService");
            Object.DontDestroyOnLoad(_root);

            _musicChannel = new MusicAudioChannel(
                _root.transform,
                _config.MusicMixerGroup,
                _config.MusicFadeDuration,
                MusicVolume,
                EffectiveMasterVolume);

            _sfxPool = new SfxAudioPool(
                _root.transform,
                _config.DefaultSfxSettings,
                _config.InitialPoolSize,
                _config.MaxPoolSize,
                SfxVolume,
                EffectiveMasterVolume);

            _isInitialized = true;
        }

        /// <summary>
        /// Called by VContainer every frame.
        /// Used to return finished non-looping SFX back to the pool.
        /// </summary>
        public void Tick()
        {
            if (!_isInitialized || _isDisposed)
                return;

            _sfxPool.ReleaseFinished();
        }

        /// <summary>
        /// Plays a non-positional SFX using a SoundConfig.
        /// Use this for UI sounds, clicks, notifications, etc.
        /// </summary>
        public int Play(SoundConfig sound)
        {
            return Play(sound, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Plays a positional SFX using a SoundConfig.
        /// Use this for world sounds such as footsteps, hits, pickups, explosions.
        /// </summary>
        public int Play(SoundConfig sound, Vector3 position)
        {
            return Play(sound, position, Quaternion.identity);
        }

        /// <summary>
        /// Plays a positional and rotated SFX using a SoundConfig.
        /// Returns an id that can later be used to pause, resume, stop, or modify this sound.
        /// </summary>
        public int Play(SoundConfig sound, Vector3 position, Quaternion rotation)
        {
            if (sound == null)
                return AudioConstants.InvalidId;

            var clip = sound.GetRandomClip();

            if (clip == null)
                return AudioConstants.InvalidId;

            return Play(
                clip,
                position,
                rotation,
                sound.CreatePlaybackSettings());
        }

        /// <summary>
        /// Plays a raw clip using default SFX settings.
        /// Use SoundConfig instead when possible.
        /// </summary>
        public int Play(AudioClip clip)
        {
            return Play(clip, Vector3.zero, Quaternion.identity, null);
        }

        /// <summary>
        /// Plays a raw clip at a world position using default SFX settings.
        /// Use SoundConfig instead when possible.
        /// </summary>
        public int Play(AudioClip clip, Vector3 position)
        {
            return Play(clip, position, Quaternion.identity, null);
        }

        /// <summary>
        /// Plays a raw clip with explicit playback settings.
        /// Returns an active sound id.
        /// </summary>
        public int Play(
            AudioClip clip,
            Vector3 position,
            Quaternion rotation,
            AudioPlaybackSettings settings = null)
        {
            EnsureInitialized();

            if (_isDisposed || clip == null)
                return AudioConstants.InvalidId;

            return _sfxPool.Play(clip, position, rotation, settings);
        }

        /// <summary>
        /// Fades from current music to a new music clip.
        /// If the same clip is already playing, it does nothing unless restartIfSame is true.
        /// </summary>
        public UniTask PlayMusicAsync(
            AudioClip clip,
            bool restartIfSame = false,
            CancellationToken token = default)
        {
            EnsureInitialized();

            if (_isDisposed || clip == null)
                return UniTask.CompletedTask;

            return _musicChannel.PlayAsync(clip, restartIfSame, token);
        }
        
        public UniTask PlayMusicAsync(
            MusicConfig music,
            bool restartIfSame = false,
            CancellationToken token = default)
        {
            if (music == null || music.Clip == null)
                return UniTask.CompletedTask;

            return PlayMusicAsync(music.Clip, restartIfSame, token);
        }

        /// <summary>
        /// Fades out and stops the current music.
        /// </summary>
        public UniTask StopMusicAsync(CancellationToken token = default)
        {
            EnsureInitialized();

            if (_isDisposed)
                return UniTask.CompletedTask;

            return _musicChannel.StopAsync(token);
        }

        /// <summary>
        /// Pauses one active SFX by id.
        /// </summary>
        public void Pause(int id)
        {
            if (!_isInitialized || _isDisposed)
                return;

            _sfxPool.Pause(id);
        }

        /// <summary>
        /// Resumes one paused SFX by id.
        /// </summary>
        public void Resume(int id)
        {
            if (!_isInitialized || _isDisposed)
                return;

            _sfxPool.Resume(id);
        }

        /// <summary>
        /// Stops one active SFX by id and returns it to the pool.
        /// </summary>
        public void Stop(int id)
        {
            if (!_isInitialized || _isDisposed)
                return;

            _sfxPool.Stop(id);
        }

        /// <summary>
        /// Pauses all active SFX. Music is not affected.
        /// </summary>
        public void PauseAllSfx()
        {
            if (!_isInitialized || _isDisposed)
                return;

            _sfxPool.PauseAll();
        }

        /// <summary>
        /// Resumes all paused SFX. Music is not affected.
        /// </summary>
        public void ResumeAllSfx()
        {
            if (!_isInitialized || _isDisposed)
                return;

            _sfxPool.ResumeAll();
        }

        /// <summary>
        /// Stops all active SFX. Music is not affected.
        /// </summary>
        public void StopAllSfx()
        {
            if (!_isInitialized || _isDisposed)
                return;

            _sfxPool.StopAll();
        }

        /// <summary>
        /// Returns true if an SFX id still belongs to an active pooled sound.
        /// </summary>
        public bool IsActive(int id)
        {
            return _isInitialized && !_isDisposed && _sfxPool.IsActive(id);
        }

        /// <summary>
        /// Changes local volume of one active SFX.
        /// This value is still multiplied by global SfxVolume.
        /// </summary>
        public bool SetVolume(int id, float volume)
        {
            if (!_isInitialized || _isDisposed)
                return false;

            return _sfxPool.SetVolume(id, volume);
        }

        /// <summary>
        /// Changes pitch of one active SFX.
        /// </summary>
        public bool SetPitch(int id, float pitch)
        {
            if (!_isInitialized || _isDisposed)
                return false;

            return _sfxPool.SetPitch(id, pitch);
        }

        /// <summary>
        /// Moves one active SFX to another world position.
        /// Useful for sounds attached to moving objects if you do not parent the AudioSource.
        /// </summary>
        public bool SetPosition(int id, Vector3 position)
        {
            if (!_isInitialized || _isDisposed)
                return false;

            return _sfxPool.SetPosition(id, position);
        }

        /// <summary>
        /// Applies a full settings object to one active SFX.
        /// </summary>
        public bool ApplySettings(int id, AudioPlaybackSettings settings)
        {
            if (!_isInitialized || _isDisposed || settings == null)
                return false;

            return _sfxPool.ApplySettings(id, settings);
        }
        
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);

            ApplyMasterVolume();

            MasterVolumeChanged?.Invoke(MasterVolume);
        }

        public void SetMasterMuted(bool muted)
        {
            if (IsMasterMuted == muted)
                return;

            IsMasterMuted = muted;

            ApplyMasterVolume();

            MasterMuteChanged?.Invoke(IsMasterMuted);
        }

        public void ToggleMasterMuted()
        {
            SetMasterMuted(!IsMasterMuted);
        }

        /// <summary>
        /// Sets global music volume.
        /// Use this from settings/options UI.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);

            if (_isInitialized && !_isDisposed)
                _musicChannel.SetMusicVolume(MusicVolume);

            MusicVolumeChanged?.Invoke(MusicVolume);
        }

        /// <summary>
        /// Sets global SFX volume.
        /// Use this from settings/options UI.
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            SfxVolume = Mathf.Clamp01(volume);

            if (_isInitialized && !_isDisposed)
                _sfxPool.SetSfxVolume(SfxVolume);

            SfxVolumeChanged?.Invoke(SfxVolume);
        }

        private void ApplyMasterVolume()
        {
            if (!_isInitialized || _isDisposed)
                return;

            float effectiveMaster = EffectiveMasterVolume;

            _musicChannel.SetMasterVolume(effectiveMaster);
            _sfxPool.SetMasterVolume(effectiveMaster);
        }
        

        /// <summary>
        /// Disposes music fade tokens and destroys the persistent AudioService GameObject.
        /// Called when the root lifetime scope is disposed.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            _musicChannel?.Dispose();
            _sfxPool?.Dispose();

            _musicChannel = null;
            _sfxPool = null;

            if (_root != null)
                Object.Destroy(_root);

            _root = null;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
                Initialize();
        }
    }

    internal static class AudioConstants
    {
        public const int InvalidId = 0;
    }
}