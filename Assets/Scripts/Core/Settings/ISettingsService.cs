using System;

namespace Core.Settings
{
    public interface ISettingsService
    {
        event Action<SettingsValues> Changed;
        
        float MasterVolume { get; }
        float MusicVolume { get; }
        float SfxVolume { get; }

        SettingsValues GetValues();

        void SetMasterVolume(float value);
        void SetMusicVolume(float value);
        void SetSfxVolume(float value);
        void ResetToDefaults();
    }
}