using System;
using System.Threading;
using Core.Audio.Contracts;
using Core.Input.Contracts;
using Core.Input.Runtime;
using Core.Patterns.MVP;
using Core.UI.Popups.Contracts;
using Core.UI.Popups.Requests;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;
using R3;

namespace Features.MainMenu.States.MainMenuState
{
    public sealed class MainMenuPresenter : IPresenter
    {
         private readonly MainMenuModel _model;
        private readonly IWindowService _windowService;
        private readonly IInputService _inputService;
        private readonly IUISoundPlayer _uiSoundPlayer;
        private readonly IPopupService _popupService;

        private readonly ReactiveCommand<Unit> _playClickedCommand = new();
        private readonly ReactiveCommand<Unit> _playRequestedCommand = new();
        private readonly ReactiveCommand<Unit> _settingsCommand = new();
        private readonly ReactiveCommand<Unit> _quitClickedCommand = new();
        private readonly ReactiveCommand<Unit> _quitRequestedCommand = new();
        private readonly CompositeDisposable _disposables = new();

        private MainMenuView _view;
        private bool _isHandlingPlayClick;
        private bool _isHandlingQuitClick;

        public Observable<Unit> PlayRequested => _playRequestedCommand;
        public Observable<Unit> SettingsRequested => _settingsCommand;
        public Observable<Unit> QuitRequested => _quitRequestedCommand;

        public MainMenuPresenter(
            MainMenuModel model,
            IWindowService windowService,
            IInputService inputService,
            IUISoundPlayer uiSoundPlayer, IPopupService popupService)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _uiSoundPlayer = uiSoundPlayer ?? throw new ArgumentNullException(nameof(uiSoundPlayer));
            _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));

            SubscribeToEvents();
        }

        public async UniTask EnterAsync(CancellationToken token = default)
        {
            _inputService.SetMode(InputMode.UIOnly);
            
            _view = await _windowService.GetOrCreateAsync<MainMenuView>(
                WindowId.MainMenu,
                token);

            token.ThrowIfCancellationRequested();

            _view.Initialize(_playClickedCommand, _settingsCommand, _quitClickedCommand);

            await _view.ShowAsync();
        }

        public async UniTask ExitAsync(CancellationToken token = default)
        {
            if (_view != null)
                await _view.HideAsync();

            _view = null;
        }

        public void HideInstantly()
        {
            _view?.HideInstantly();
        }

        private void SubscribeToEvents()
        {
            _playClickedCommand
                .Subscribe(_ =>
                {
                    _uiSoundPlayer.PlayButtonClick();
                    HandlePlayClickedAsync().Forget();
                })
                .AddTo(_disposables);

            
            _playClickedCommand
                .Subscribe(_ => _uiSoundPlayer.PlayButtonClick())
                .AddTo(_disposables);

            _settingsCommand
                .Subscribe(_ => _uiSoundPlayer.PlayButtonClick())
                .AddTo(_disposables);
            
            _quitClickedCommand
                .Subscribe(_ =>
                {
                    _uiSoundPlayer.PlayButtonClick();
                    HandleQuitClickedAsync().Forget();
                })
                .AddTo(_disposables);
        }
        
        private async UniTask HandlePlayClickedAsync()
        {
            if (_isHandlingPlayClick)
                return;

            _isHandlingPlayClick = true;

            try
            {
                // await _model.EnsureSaveLoadedAsync();
                //
                // if (!_model.HasPreviousPlaySession())
                // {
                //     _playRequestedCommand.Execute(Unit.Default);
                //     return;
                // }
                //
                // bool continuePreviousSession = await _popupService.ShowAsync(
                //     new ConfirmationPopupRequest(
                //         title: "Continue previous session?",
                //         message: "Saves were found. Do you want to continue from your last checkpoint or reset progress and start from the beginning?",
                //         yesText: "Continue",
                //         noText: "New Game"));
                //
                // if (!continuePreviousSession)
                //     await _model.ResetProgressAsync();

                _playRequestedCommand.Execute(Unit.Default);
            }
            finally
            {
                _isHandlingPlayClick = false;
            }
        }

        private async UniTask HandleQuitClickedAsync()
        {
            if (_isHandlingQuitClick)
                return;
            
            _isHandlingQuitClick = true;

            try
            {
                await _model.SaveBeforeQuit();
                
                _quitRequestedCommand.Execute(Unit.Default);
            }
            finally
            {
               _isHandlingQuitClick = false;
            }
        }
        
        public void Dispose()
        {
            _playClickedCommand.Dispose();
            _playRequestedCommand.Dispose();
            _settingsCommand.Dispose();
            _quitRequestedCommand.Dispose();
            _disposables.Dispose();
        }
    }
}