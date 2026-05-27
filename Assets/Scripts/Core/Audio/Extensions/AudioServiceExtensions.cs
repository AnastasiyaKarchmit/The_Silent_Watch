using System.Threading;
using Core.Audio.Configs;
using Core.Audio.Contracts;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Audio.Extensions
{
    public static class AudioServiceExtensions
    {
        public static int PlayIfExists(this IAudioService audioService, SoundConfig sound)
        {
            if (audioService == null || sound == null)
                return 0;

            return audioService.Play(sound);
        }

        public static int PlayIfExists(
            this IAudioService audioService,
            SoundConfig sound,
            Vector3 position)
        {
            if (audioService == null || sound == null)
                return 0;

            return audioService.Play(sound, position);
        }

        public static UniTask PlayMusicIfExistsAsync(
            this IAudioService audioService,
            MusicConfig music,
            bool restartIfSame = false,
            CancellationToken token = default)
        {
            if (audioService == null || music == null || music.Clip == null)
                return UniTask.CompletedTask;

            return audioService.PlayMusicAsync(music, restartIfSame, token);
        }
    }
}