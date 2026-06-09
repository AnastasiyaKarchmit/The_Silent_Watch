using System;
using System.Threading;
using Core.AppStates.Contracts.State;
using Core.AppStates.Data;
using Cysharp.Threading.Tasks;
using Features.Gameplay.Networking;
using Features.Shared;
using R3;
using UnityEngine;

namespace Features.Gameplay
{
    public class GameplayAppStateController : IAppStateController
    {
        private readonly GameplayFlowController _flowController;
        private readonly GameplayNetworkSessionService _networkSessionService;
        
        private readonly CompositeDisposable _disposables = new();
        private UniTaskCompletionSource<AppStateExitResult> _completionSource;
        
        private bool _isDedicatedServer;
        
        public GameplayAppStateController(GameplayFlowController flowController,
            GameplayNetworkSessionService networkSessionService)
        {
            _flowController = flowController ?? throw new ArgumentNullException(nameof(flowController));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
        }

        public async UniTask EnterAsync(object payload, CancellationToken token)
        {
            _isDedicatedServer = RuntimeMode.IsDedicatedServer;
            _completionSource = new UniTaskCompletionSource<AppStateExitResult>();
            
            if (_isDedicatedServer)
            {
                Debug.Log("[GameplayAppState] Dedicated server mode. Skipping Gameplay UI flow.");
                return;
            }
            
            _flowController.BackToMenuRequested
                .Subscribe(_ =>
                {
                    _completionSource.TrySetResult(
                        AppStateExitResult.SwitchTo(AppStateId.MainMenu));
                })
                .AddTo(_disposables);

            await _flowController.EnterAsync(token);
            await _networkSessionService.EnterGameplayAsync(token);
        }

        public async UniTask<AppStateExitResult> RunAsync(CancellationToken token)
        {
            if (_isDedicatedServer)
            {
                await UniTask.WaitUntilCanceled(token);
                throw new OperationCanceledException(token);
            }
            
            await using var registration = token.Register(() =>
            {
                _completionSource.TrySetCanceled(token);
            });

            return await _completionSource.Task;
        }

        public async UniTask ExitAsync(CancellationToken token)
        {
            _disposables.Clear();

            if (!_isDedicatedServer)
            {
                await _flowController.ExitAsync(token);
                await _networkSessionService.ExitGameplayAsync(token);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _flowController.Dispose();
        }
    }
}