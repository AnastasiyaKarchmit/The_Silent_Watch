using System;
using System.Threading;
using Core.Audio.Contracts;
using Cysharp.Threading.Tasks;

namespace Features.MainMenu
{
    public sealed class MainMenuAudioController : IDisposable
    {
        private readonly IAudioService _audioService;
        private readonly IAudioDatabase _audioDatabase;

        public MainMenuAudioController(
            IAudioService audioService,
            IAudioDatabase audioDatabase)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _audioDatabase = audioDatabase ?? throw new ArgumentNullException(nameof(audioDatabase));
        }

        public UniTask EnterAsync(CancellationToken token)
        {
            return _audioService.PlayMusicAsync(
                _audioDatabase.Music.MainMenu,
                restartIfSame: false,
                token);
        }

        public UniTask ExitAsync(CancellationToken token)
        {
            return _audioService.StopMusicAsync(token);
        }

        public void Dispose()
        {
        }
    }
}