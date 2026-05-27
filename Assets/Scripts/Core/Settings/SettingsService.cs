using System;
using Core.Audio.Configs;
using Core.Audio.Contracts;
using Core.Save;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace Core.Settings
{
    public sealed class SettingsService : ISettingsService, ISaveDataProvider, IInitializable, IDisposable
    {
         private readonly ISaveSystem _saveSystem;
        private readonly IAudioService _audioService;
        private readonly AudioServiceConfig _audioConfig;

        private float _masterVolume;
        private float _musicVolume;
        private float _sfxVolume;
        private bool _masterMuted;

        private bool _isRegistered;

        public event Action<SettingsValues> Changed;

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;
        public bool MasterMuted => _masterMuted;

        public SettingsService(
            ISaveSystem saveSystem,
            IAudioService audioService,
            AudioServiceConfig audioConfig)
        {
            _saveSystem = saveSystem ?? throw new ArgumentNullException(nameof(saveSystem));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _audioConfig = audioConfig ?? throw new ArgumentNullException(nameof(audioConfig));

            ApplyDefaultsFromConfig();
        }

        public void Initialize()
        {
            if (_isRegistered)
                return;

            _saveSystem.Register(this);
            _isRegistered = true;

            ApplyRuntimeSettings();
            NotifyChanged();
        }

        public UniTask LoadAsync(PersistentData data)
        {
            if (data.Settings == null)
            {
                ApplyDefaultsFromConfig();
                data.Settings = CreateSettingsDataFromCurrentValues();
            }
            else
            {
                LoadFromData(data.Settings);
            }

            ApplyRuntimeSettings();
            NotifyChanged();

            return UniTask.CompletedTask;
        }

        public void Save(PersistentData data)
        {
            data.Settings = CreateSettingsDataFromCurrentValues();
        }

        public SettingsValues GetValues()
        {
            return new SettingsValues(
                _masterVolume,
                _musicVolume,
                _sfxVolume,
                _masterMuted);
        }

        public void SetMasterVolume(float value)
        {
            value = Mathf.Clamp01(value);

            if (Mathf.Approximately(_masterVolume, value))
                return;

            _masterVolume = value;

            ApplyRuntimeSettings();
            NotifyChanged();
        }

        public void SetMusicVolume(float value)
        {
            value = Mathf.Clamp01(value);

            if (Mathf.Approximately(_musicVolume, value))
                return;

            _musicVolume = value;

            ApplyRuntimeSettings();
            NotifyChanged();
        }

        public void SetSfxVolume(float value)
        {
            value = Mathf.Clamp01(value);

            if (Mathf.Approximately(_sfxVolume, value))
                return;

            _sfxVolume = value;

            ApplyRuntimeSettings();
            NotifyChanged();
        }

        public void SetMasterMuted(bool muted)
        {
            if (_masterMuted == muted)
                return;

            _masterMuted = muted;

            ApplyRuntimeSettings();
            NotifyChanged();
        }

        public void ToggleMasterMuted()
        {
            SetMasterMuted(!_masterMuted);
        }

        public void ResetToDefaults()
        {
            ApplyDefaultsFromConfig();

            ApplyRuntimeSettings();
            NotifyChanged();
        }

        public void Dispose()
        {
            if (!_isRegistered)
                return;

            _saveSystem.Unregister(this);
            _isRegistered = false;
        }

        private void ApplyDefaultsFromConfig()
        {
            _masterVolume = Mathf.Clamp01(_audioConfig.DefaultMasterVolume);
            _musicVolume = Mathf.Clamp01(_audioConfig.DefaultMusicVolume);
            _sfxVolume = Mathf.Clamp01(_audioConfig.DefaultSfxVolume);
            _masterMuted = _audioConfig.DefaultMasterMuted;
        }

        private void LoadFromData(SettingsData data)
        {
            _masterVolume = Mathf.Clamp01(data.MasterVolume);
            _musicVolume = Mathf.Clamp01(data.MusicVolume);
            _sfxVolume = Mathf.Clamp01(data.SfxVolume);
            _masterMuted = data.MasterMuted;
        }

        private SettingsData CreateSettingsDataFromCurrentValues()
        {
            return new SettingsData
            {
                MasterVolume = _masterVolume,
                MusicVolume = _musicVolume,
                SfxVolume = _sfxVolume,
                MasterMuted = _masterMuted
            };
        }

        private void ApplyRuntimeSettings()
        {
            _audioService.SetMasterVolume(_masterVolume);
            _audioService.SetMusicVolume(_musicVolume);
            _audioService.SetSfxVolume(_sfxVolume);
            _audioService.SetMasterMuted(_masterMuted);
        }

        private void NotifyChanged()
        {
            Changed?.Invoke(GetValues());
        }
    }
}