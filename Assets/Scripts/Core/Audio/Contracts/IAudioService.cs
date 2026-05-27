using System;
using System.Threading;
using Core.Audio.Configs;
using Core.Audio.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Audio.Contracts
{
    public interface IAudioService
    {
        float MasterVolume { get; }
        float MusicVolume { get; }
        float SfxVolume { get; }

        bool IsMasterMuted { get; }

        event Action<float> MasterVolumeChanged;
        event Action<float> MusicVolumeChanged;
        event Action<float> SfxVolumeChanged;
        event Action<bool> MasterMuteChanged;

        int Play(SoundConfig sound);
        int Play(SoundConfig sound, Vector3 position);
        int Play(SoundConfig sound, Vector3 position, Quaternion rotation);

        int Play(AudioClip clip);
        int Play(AudioClip clip, Vector3 position);
        int Play(AudioClip clip, Vector3 position, Quaternion rotation, AudioPlaybackSettings settings = null);

        UniTask PlayMusicAsync(
            AudioClip clip,
            bool restartIfSame = false,
            CancellationToken token = default);
        
        UniTask PlayMusicAsync(
            MusicConfig music,
            bool restartIfSame = false,
            CancellationToken token = default);
        
        UniTask StopMusicAsync(CancellationToken token = default);

        void Pause(int id);
        void Resume(int id);
        void Stop(int id);

        void PauseAllSfx();
        void ResumeAllSfx();
        void StopAllSfx();

        bool IsActive(int id);

        bool SetVolume(int id, float volume);
        bool SetPitch(int id, float pitch);
        bool SetPosition(int id, Vector3 position);
        bool ApplySettings(int id, AudioPlaybackSettings settings);

        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);

        void SetMasterMuted(bool muted);
        void ToggleMasterMuted();
    }
}