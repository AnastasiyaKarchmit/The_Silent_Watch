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

namespace Features.Gameplay.States.GameplayState
{
    public class GameplayPresenter : IPresenter
    {
        private readonly GameplayModel _model;
        private readonly IWindowService _windowService;
        private readonly IInputService _inputService;
        
        private readonly CompositeDisposable _screenDisposables = new();
        
        private readonly ReactiveCommand<Unit> _pauseCommand = new();
        
        private readonly TimeSpan _inputThrottle = TimeSpan.FromMilliseconds(300);
        
        private GameplayView _view;
        
        public Observable<Unit> PauseRequested => _pauseCommand;
        
        public GameplayPresenter(
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
            //_inputService.SetMode(InputMode.Disabled);

            _view = await _windowService.GetOrCreateAsync<GameplayView>(
                WindowId.GameplayHud,
                token);

            token.ThrowIfCancellationRequested();

            //_view.Initialize(_playCommand, _settingsCommand);

            await _view.ShowAsync();

            SubscribeToInput();
            
            _inputService.SetMode(InputMode.Gameplay);
            
            Time.timeScale = 1;
        }

        public UniTask ExitAsync(CancellationToken token = default)
        {
            if (_view != null)
                _view.HideInstantly();

            _view = null;
            
            return UniTask.CompletedTask;
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
                .Subscribe(_ => _pauseCommand.Execute(Unit.Default))
                .AddTo(_screenDisposables);
        }
        
        public void Dispose()
        {
            _screenDisposables.Dispose();
            _pauseCommand.Dispose();
        }
    }
}