using Core.Audio.Configs;

namespace Core.Audio.Contracts
{
    public interface IAudioDatabase
    {
        UISoundConfigs UI { get; }
        MusicConfigs Music { get; }
        GameplaySoundConfigs Gameplay { get; }
        AmbienceSoundConfigs Ambience { get; }
    }
}