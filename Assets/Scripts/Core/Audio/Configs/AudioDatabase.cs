using System;
using Core.Audio.Contracts;
using UnityEngine;

namespace Core.Audio.Configs
{
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "Configs/Audio/Audio Database")]
    public sealed class AudioDatabase : ScriptableObject, IAudioDatabase
    {
        [Header("Groups")]
        [SerializeField] private UISoundConfigs ui = new();
        [SerializeField] private MusicConfigs music = new();
        [SerializeField] private GameplaySoundConfigs gameplay = new();
        [SerializeField] private AmbienceSoundConfigs ambience = new();

        public UISoundConfigs UI => ui;
        public MusicConfigs Music => music;
        public GameplaySoundConfigs Gameplay => gameplay;
        public AmbienceSoundConfigs Ambience => ambience;

#if UNITY_EDITOR
        private void OnValidate()
        {
            ui ??= new UISoundConfigs();
            music ??= new MusicConfigs();
            gameplay ??= new GameplaySoundConfigs();
            ambience ??= new AmbienceSoundConfigs();
        }
#endif
    }

    [Serializable]
    public sealed class UISoundConfigs
    {
        [field: SerializeField] public SoundConfig ButtonClick { get; private set; }
        [field: SerializeField] public SoundConfig ButtonHover { get; private set; }
        [field: SerializeField] public SoundConfig ButtonBack { get; private set; }
        [field: SerializeField] public SoundConfig ButtonConfirm { get; private set; }
        [field: SerializeField] public SoundConfig ButtonCancel { get; private set; }
        [field: SerializeField] public SoundConfig Error { get; private set; }
        [field: SerializeField] public SoundConfig PopupOpen { get; private set; }
        [field: SerializeField] public SoundConfig PopupClose { get; private set; }
    }

    [Serializable]
    public sealed class MusicConfigs
    {
        [field: SerializeField] public MusicConfig MainMenu { get; private set; }
        [field: SerializeField] public MusicConfig Gameplay { get; private set; }
        [field: SerializeField] public MusicConfig Loading { get; private set; }
        [field: SerializeField] public MusicConfig Victory { get; private set; }
        [field: SerializeField] public MusicConfig Defeat { get; private set; }
    }

    [Serializable]
    public sealed class GameplaySoundConfigs
    {
        [Header("Player")]
        [field: SerializeField] public SoundConfig PlayerJump { get; private set; }
        [field: SerializeField] public SoundConfig PlayerLand { get; private set; }
        [field: SerializeField] public SoundConfig PlayerHit { get; private set; }

        [Header("Items")]
        [field: SerializeField] public SoundConfig PickupItem { get; private set; }
        [field: SerializeField] public SoundConfig DropItem { get; private set; }

        [Header("Combat")]
        [field: SerializeField] public SoundConfig Attack { get; private set; }
        [field: SerializeField] public SoundConfig Impact { get; private set; }
    }

    [Serializable]
    public sealed class AmbienceSoundConfigs
    {
        [field: SerializeField] public SoundConfig CaveLoop { get; private set; }
        [field: SerializeField] public SoundConfig WindLoop { get; private set; }
        [field: SerializeField] public SoundConfig RainLoop { get; private set; }
    }
}