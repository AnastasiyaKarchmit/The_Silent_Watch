using System;
using System.Collections.Generic;
using System.Linq;
using Core.Audio.Data;
using UnityEngine;

namespace Core.Audio.Runtime
{
    /// <summary>
    /// Pool of AudioSources used for SFX.
    /// 
    /// Responsibilities:
    /// - create pooled AudioSources;
    /// - play SFX and return ids;
    /// - pause/resume/stop SFX by id;
    /// - return finished non-looping SFX to the pool;
    /// - steal the least important sound if the pool is full.
    /// </summary>
    internal sealed class SfxAudioPool : IDisposable
    {
        private readonly Dictionary<int, AudioInstance> _active = new();
        private readonly Queue<AudioInstance> _inactive = new();
        private readonly List<AudioInstance> _all = new();
        private readonly List<AudioInstance> _releaseBuffer = new();

        private readonly Transform _root;
        private readonly AudioPlaybackSettings _defaultSettings;

        private readonly int _maxPoolSize;

        private int _nextId;
        private bool _isDisposed;
        
        private float _sfxVolume;
        private float _masterVolume;

        private float EffectiveVolume => _sfxVolume * _masterVolume;

        public SfxAudioPool(
            Transform parent,
            AudioPlaybackSettings defaultSettings,
            int initialPoolSize,
            int maxPoolSize,
            float initialSfxVolume,
            float initialMasterVolume)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var rootObject = new GameObject("SFX");
            rootObject.transform.SetParent(parent);
            _root = rootObject.transform;

            _defaultSettings = defaultSettings ?? new AudioPlaybackSettings();

            _sfxVolume = Mathf.Clamp01(initialSfxVolume);
            _masterVolume = Mathf.Clamp01(initialMasterVolume);

            int maxVirtualVoices = UnityEngine.AudioSettings.GetConfiguration().numVirtualVoices;
            _maxPoolSize = Mathf.Clamp(maxPoolSize, 1, maxVirtualVoices);

            if (maxPoolSize > maxVirtualVoices)
            {
                Debug.LogWarning(
                    $"SFX pool size is greater than Unity virtual voices. " +
                    $"Pool size will be clamped to {maxVirtualVoices}.");
            }

            int safeInitialSize = Mathf.Clamp(initialPoolSize, 1, _maxPoolSize);
            Expand(safeInitialSize);
        }

        /// <summary>
        /// Plays a clip through a pooled AudioSource.
        /// Returns id of the active SFX.
        /// </summary>
        public int Play(
            AudioClip clip,
            Vector3 position,
            Quaternion rotation,
            AudioPlaybackSettings settings)
        {
            if (_isDisposed || clip == null)
                return AudioConstants.InvalidId;

            AudioInstance instance = GetFreeInstance();
            int id = CreateId();

            AudioPlaybackSettings playbackSettings =
                settings?.Clone() ?? _defaultSettings.Clone();

            instance.Prepare(
                id,
                clip,
                position,
                rotation,
                playbackSettings,
                EffectiveVolume,
                Time.unscaledTime);

            _active.Add(id, instance);

            instance.Source.Play();

            return id;
        }

        /// <summary>
        /// Releases all non-looping SFX whose AudioSource has stopped playing.
        /// Call this from ITickable.
        /// </summary>
        public void ReleaseFinished()
        {
            if (_isDisposed || _active.Count == 0)
                return;

            _releaseBuffer.Clear();

            foreach (AudioInstance instance in _active.Values)
            {
                if (instance == null)
                    continue;

                if (instance.IsPaused)
                    continue;

                if (instance.Source.loop)
                    continue;

                if (!instance.Source.isPlaying)
                    _releaseBuffer.Add(instance);
            }

            foreach (AudioInstance instance in _releaseBuffer)
                Release(instance, true);

            _releaseBuffer.Clear();
        }

        /// <summary>
        /// Pauses one active SFX by id.
        /// </summary>
        public void Pause(int id)
        {
            if (_active.TryGetValue(id, out AudioInstance instance))
                instance.Pause();
        }

        /// <summary>
        /// Resumes one paused SFX by id.
        /// </summary>
        public void Resume(int id)
        {
            if (_active.TryGetValue(id, out AudioInstance instance))
                instance.Resume();
        }

        /// <summary>
        /// Stops one active SFX by id and returns it to the pool.
        /// </summary>
        public void Stop(int id)
        {
            if (_active.TryGetValue(id, out AudioInstance instance))
                Release(instance, true);
        }

        /// <summary>
        /// Pauses all active SFX.
        /// </summary>
        public void PauseAll()
        {
            foreach (AudioInstance instance in _active.Values)
                instance.Pause();
        }

        /// <summary>
        /// Resumes all paused SFX.
        /// </summary>
        public void ResumeAll()
        {
            foreach (AudioInstance instance in _active.Values)
                instance.Resume();
        }

        /// <summary>
        /// Stops all active SFX and returns them to the pool.
        /// </summary>
        public void StopAll()
        {
            _releaseBuffer.Clear();
            _releaseBuffer.AddRange(_active.Values);

            foreach (AudioInstance instance in _releaseBuffer)
                Release(instance, true);

            _releaseBuffer.Clear();
        }

        /// <summary>
        /// Returns true if id belongs to an active SFX.
        /// </summary>
        public bool IsActive(int id)
        {
            return _active.ContainsKey(id);
        }

        /// <summary>
        /// Sets local volume for one active SFX.
        /// Final AudioSource volume = local volume * global SFX volume.
        /// </summary>
        public bool SetVolume(int id, float volume)
        {
            if (!_active.TryGetValue(id, out AudioInstance instance))
                return false;

            instance.SetLocalVolume(volume, EffectiveVolume);
            return true;
        }


        /// <summary>
        /// Sets pitch for one active SFX.
        /// </summary>
        public bool SetPitch(int id, float pitch)
        {
            if (!_active.TryGetValue(id, out AudioInstance instance))
                return false;

            instance.SetPitch(pitch);
            return true;
        }

        /// <summary>
        /// Moves one active SFX to a new position.
        /// </summary>
        public bool SetPosition(int id, Vector3 position)
        {
            if (!_active.TryGetValue(id, out AudioInstance instance))
                return false;

            instance.SetPosition(position);
            return true;
        }

        /// <summary>
        /// Applies full playback settings to one active SFX.
        /// </summary>
        public bool ApplySettings(int id, AudioPlaybackSettings settings)
        {
            if (settings == null)
                return false;

            if (!_active.TryGetValue(id, out AudioInstance instance))
                return false;

            instance.ApplySettings(settings, EffectiveVolume);
            return true;
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            RefreshActiveVolumes();
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            RefreshActiveVolumes();
        }

        private void RefreshActiveVolumes()
        {
            foreach (AudioInstance instance in _active.Values)
                instance.RefreshVolume(EffectiveVolume);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            StopAll();

            _active.Clear();
            _inactive.Clear();
            _all.Clear();
            _releaseBuffer.Clear();
        }

        private AudioInstance GetFreeInstance()
        {
            if (_inactive.Count > 0)
                return _inactive.Dequeue();

            if (_all.Count < _maxPoolSize)
                return CreateInstance();

            return StealLeastImportantInstance();
        }

        private AudioInstance StealLeastImportantInstance()
        {
            AudioInstance instance = _active.Values
                // In Unity, lower priority value is more important.
                // So the "least important" sound has the highest priority value.
                .OrderByDescending(x => x.Priority)
                // If priority is the same, steal the oldest sound.
                .ThenBy(x => x.StartedAt)
                .FirstOrDefault();

            if (instance == null)
                return CreateInstance();

            Release(instance, false);
            return instance;
        }

        private void Release(AudioInstance instance, bool addToInactivePool)
        {
            if (instance == null)
                return;

            _active.Remove(instance.Id);

            instance.Reset();

            if (addToInactivePool)
                _inactive.Enqueue(instance);
        }

        private void Expand(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                AudioInstance instance = CreateInstance();
                instance.Reset();
                _inactive.Enqueue(instance);
            }
        }

        private AudioInstance CreateInstance()
        {
            var gameObject = new GameObject($"SFX Audio {_all.Count}");
            gameObject.transform.SetParent(_root);
            gameObject.SetActive(false);

            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;

            var instance = new AudioInstance(gameObject, source);
            _all.Add(instance);

            return instance;
        }

        private int CreateId()
        {
            if (_nextId == int.MaxValue)
                _nextId = 0;

            do
            {
                _nextId++;
            }
            while (_active.ContainsKey(_nextId) || _nextId == AudioConstants.InvalidId);

            return _nextId;
        }
    }
}