using System;
using System.Threading;
using Core.Input.Contracts;
using Core.Input.Runtime;
using Core.Patterns.MVP;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Features.Gameplay.States.PauseState
{
    public class PausePresenter : IPresenter
    {
        private readonly GameplayModel _model;
        private readonly IWindowService _windowService;
        private readonly IInputService _inputService;

        private readonly ReactiveCommand<Unit> _resumeCommand = new();
        private readonly ReactiveCommand<Unit> _settingsCommand = new();
        private readonly ReactiveCommand<Unit> _backToMenuCommand = new();
        
        private readonly CompositeDisposable _screenDisposables = new();
        
        private readonly TimeSpan _inputThrottle = TimeSpan.FromMilliseconds(300);
        
        public Observable<Unit> ResumeRequested => _resumeCommand;
        public Observable<Unit> SettingsRequested => _settingsCommand;
        public Observable<Unit> BackToMenuRequested => _backToMenuCommand;
        
        private PauseView _view;

        public PausePresenter(
            GameplayModel model, 
            IWindowService windowService,
            IInputService inputService)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        }
        
        public async UniTask EnterAsync(CancellationToken token = default)
        {
            _inputService.SetMode(InputMode.Disabled);
            Time.timeScale = 0;

            _view = await _windowService.GetOrCreateAsync<PauseView>(
                WindowId.Pause,
                token);

            token.ThrowIfCancellationRequested();

            _view.Initialize(_resumeCommand, _settingsCommand, _backToMenuCommand);

            _view.ShowInstantly();
            
            _inputService.SetMode(InputMode.UIOnly);

            //Cursor.lockState = CursorLockMode.Confined;

            SubscribeToInput();
        }

        public async UniTask ExitAsync(CancellationToken token = default)
        {
            _screenDisposables.Clear();
            
            if (_view != null)
                await _view.HideAsync();

            _view = null;
        }
        
        public void HideInstantly()
        {
            _screenDisposables.Clear();
            _view?.HideInstantly();
        }
        
        private void SubscribeToInput()
        {
            _screenDisposables.Clear();

            _inputService.UI.Cancel.Performed
                .Where(pressed => pressed)
                .ThrottleFirst(_inputThrottle)
                .Subscribe(_ => _resumeCommand.Execute(Unit.Default))
                .AddTo(_screenDisposables);
        }

        public void Dispose()
        {
            _screenDisposables.Dispose();
            _resumeCommand.Dispose();
            _settingsCommand.Dispose();
            _backToMenuCommand.Dispose();
        }
    }
}