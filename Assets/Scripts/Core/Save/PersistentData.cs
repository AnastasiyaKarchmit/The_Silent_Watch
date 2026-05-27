using System;

namespace Core.Save
{
    [Serializable]
    public sealed class PersistentData
    {
        public int Version = 1;

        public PlayerData Player = new();
        public GameplayData Gameplay = new();
        public SettingsData Settings = new();
    }
    [Serializable]
    public sealed class PlayerData
    {
        public int SoftCurrency;
        public int SelectedCharacterId;
    }
    
    [Serializable]
    public sealed class GameplayData
    {
        public int LastCompletedLevel;
    }
    
    [Serializable]
    public sealed class SettingsData
    {
        public float MasterVolume = 0.5f;
        public float MusicVolume = 1f;
        public float SfxVolume = 1f;
        public bool MasterMuted;
    }
}