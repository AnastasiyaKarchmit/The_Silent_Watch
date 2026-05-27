using System;
using Core.Audio.Contracts;
using Core.Audio.Extensions;

namespace Core.Audio.Runtime
{
    public sealed class UISoundPlayer : IUISoundPlayer
    {
        private readonly IAudioService _audioService;
        private readonly IAudioDatabase _audioDatabase;

        public UISoundPlayer(
            IAudioService audioService,
            IAudioDatabase audioDatabase)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _audioDatabase = audioDatabase ?? throw new ArgumentNullException(nameof(audioDatabase));
        }

        public void PlayButtonClick()
        {
            _audioService.PlayIfExists(_audioDatabase.UI.ButtonClick);
        }

        public void PlayButtonHover()
        {
            _audioService.PlayIfExists(_audioDatabase.UI.ButtonHover);
        }

        public void PlayBack()
        {
            _audioService.PlayIfExists(_audioDatabase.UI.ButtonBack);
        }

        public void PlayConfirm()
        {
            _audioService.PlayIfExists(_audioDatabase.UI.ButtonConfirm);
        }

        public void PlayCancel()
        {
            _audioService.PlayIfExists(_audioDatabase.UI.ButtonCancel);
        }

        public void PlayError()
        {
            _audioService.PlayIfExists(_audioDatabase.UI.Error);
        }

        public void PlayPopupOpen()
        {
            _audioService.PlayIfExists(_audioDatabase.UI.PopupOpen);
        }

        public void PlayPopupClose()
        {
            _audioService.Play(_audioDatabase.UI.PopupClose);
        }
    }
}