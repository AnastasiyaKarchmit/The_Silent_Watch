using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Gameplay.States.GameplayState;
using Features.Gameplay.States.PauseState;
using Features.Shared.SettingsState;
using R3;
using Stateless;
using UnityEngine;

namespace Features.Gameplay
{
    public enum GameplayFlowState
    {
        Gameplay,
        Pause,
        Settings
    }

    public enum GameplayFlowTrigger
    {
        StartGameplay,
        OpenPause,
        OpenSettings
    }
    public class GameplayFlowController : IDisposable
    {
        private readonly GameplayPresenter _gameplayPresenter;
        private readonly PausePresenter _pausePresenter;
        private readonly SettingsPresenter _settingsPresenter;
        
        private readonly StateMachine<GameplayFlowState, GameplayFlowTrigger> _stateMachine;
        private readonly CompositeDisposable _disposables = new();
        private readonly ReactiveCommand<Unit> _backToMenuRequested = new();
        
        private CancellationToken _currentToken;
        
        private bool _isTransitioning;
        
        public Observable<Unit> BackToMenuRequested => _backToMenuRequested;

        public GameplayFlowController(
            GameplayPresenter gameplayPresenter,
            PausePresenter pausePresenter,
            SettingsPresenter settingsPresenter)
        {
            _gameplayPresenter = gameplayPresenter ??  throw new ArgumentNullException(nameof(gameplayPresenter));
            _pausePresenter = pausePresenter ??  throw new ArgumentNullException(nameof(pausePresenter));
            _settingsPresenter = settingsPresenter ??  throw new ArgumentNullException(nameof(settingsPresenter));
            
            _stateMachine =  new StateMachine<GameplayFlowState, GameplayFlowTrigger>(GameplayFlowState.Gameplay);
            
            ConfigureStateMachine();
            SubscribeToPresenterEvents();
        }
        
        public async UniTask EnterAsync(CancellationToken token)
        {
            _currentToken = token;
            
            _gameplayPresenter.HideInstantly();
            _pausePresenter.HideInstantly();
            _settingsPresenter.HideInstantly();

            await _gameplayPresenter.EnterAsync(token);
        }
        
        public async UniTask ExitAsync(CancellationToken token)
        {
            await UniTask.WhenAll(
                _gameplayPresenter.ExitAsync(token),
                _pausePresenter.ExitAsync(token),
                _settingsPresenter.ExitAsync(token));
        }
        
        private void ConfigureStateMachine()
        {
            _stateMachine.Configure(GameplayFlowState.Gameplay)
                .OnEntryAsync(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    await _gameplayPresenter.EnterAsync(_currentToken);
                })
                .OnExitAsync(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    await _gameplayPresenter.ExitAsync(_currentToken);
                })
                .Permit(GameplayFlowTrigger.OpenPause, GameplayFlowState.Pause);

            _stateMachine.Configure(GameplayFlowState.Pause)
                .OnEntryAsync(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    await _pausePresenter.EnterAsync(_currentToken);
                })
                .OnExitAsync(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    await _pausePresenter.ExitAsync(_currentToken);
                })
                .Permit(GameplayFlowTrigger.StartGameplay, GameplayFlowState.Gameplay)
                .Permit(GameplayFlowTrigger.OpenSettings, GameplayFlowState.Settings);

            _stateMachine.Configure(GameplayFlowState.Settings)
                .OnEntryAsync(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    await _settingsPresenter.EnterAsync(_currentToken);
                })
                .OnExitAsync(async () =>
                {
                    await UniTask.SwitchToMainThread();
                    await _settingsPresenter.ExitAsync(_currentToken);
                })
                .Permit(GameplayFlowTrigger.OpenPause, GameplayFlowState.Pause);

            _stateMachine.OnTransitionCompleted(transition =>
            {
                Debug.Log($"Gameplay flow: {transition.Source} -> {transition.Destination} by {transition.Trigger}");
            });
        }
        
        private void SubscribeToPresenterEvents()
        {
            _gameplayPresenter.PauseRequested
                .SubscribeAwait(
                    async (_, token) =>
                    {
                        await UniTask.SwitchToMainThread();
                        await FireAsync(GameplayFlowTrigger.OpenPause);
                    },
                    AwaitOperation.Drop)
                .AddTo(_disposables);
            
            _pausePresenter.ResumeRequested
                .SubscribeAwait(
                    async (_, token) =>
                    {
                        await UniTask.SwitchToMainThread();
                        await FireAsync(GameplayFlowTrigger.StartGameplay);
                    },
                    AwaitOperation.Drop)
                .AddTo(_disposables);
            
            _pausePresenter.SettingsRequested
                .SubscribeAwait(
                    async (_, token) =>
                    {
                        await UniTask.SwitchToMainThread();
                        await FireAsync(GameplayFlowTrigger.OpenSettings);
                    },
                    AwaitOperation.Drop)
                .AddTo(_disposables);
            
            _pausePresenter.BackToMenuRequested
                .Subscribe(_ => _backToMenuRequested.Execute(Unit.Default))
                .AddTo(_disposables);

            _settingsPresenter.BackRequested
                .SubscribeAwait(
                    async (_, token) =>
                    {
                        await UniTask.SwitchToMainThread();
                        await FireAsync(GameplayFlowTrigger.OpenPause);
                    },
                    AwaitOperation.Drop)
                .AddTo(_disposables);
        }
        
        private async UniTask FireAsync(GameplayFlowTrigger trigger)
        {
            if (_isTransitioning)
            {
                Debug.Log("Already transitioning");
                return;
            }

            if (!_stateMachine.CanFire(trigger))
            {
                Debug.LogWarning($"Cannot fire {trigger} from {_stateMachine.State}");
                return;
            }

            _isTransitioning = true;

            try
            {
                Debug.Log($"Fired {trigger} from {_stateMachine.State}" );
                await _stateMachine.FireAsync(trigger);
            }
            finally
            {
                _isTransitioning = false;
            }
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
            _backToMenuRequested.Dispose();
            
            _settingsPresenter.Dispose();
            _pausePresenter.Dispose();
            _gameplayPresenter.Dispose();
        }
    }
}