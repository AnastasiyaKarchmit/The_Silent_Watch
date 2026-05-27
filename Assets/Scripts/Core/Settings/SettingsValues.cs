namespace Core.Settings
{
    public readonly struct SettingsValues
    {
        public readonly float MasterVolume;
        public readonly float MusicVolume;
        public readonly float SfxVolume;
        public readonly bool MasterMuted;

        public SettingsValues(float masterVolume, float musicVolume, float sfxVolume, bool masterMuted)
        {
            MasterVolume = masterVolume;
            MusicVolume = musicVolume;
            SfxVolume = sfxVolume;
            MasterMuted = masterMuted;
        }
    }
}