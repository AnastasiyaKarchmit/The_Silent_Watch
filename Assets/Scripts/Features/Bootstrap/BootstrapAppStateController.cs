using System;
using System.Threading;
using Core.AppStates.Contracts.State;
using Core.AppStates.Data;
using Cysharp.Threading.Tasks;
using Features.Shared;
using UnityEngine;

namespace Features.Bootstrap
{
    public sealed class BootstrapAppStateController : IAppStateController
    {
        private readonly BootstrapPresenter _presenter;

        public BootstrapAppStateController(BootstrapPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        public UniTask EnterAsync(object payload, CancellationToken token)
        {
            if (RuntimeMode.IsDedicatedServer)
            {
                Debug.Log("[Bootstrap] Dedicated server detected. Skipping Bootstrap UI.");
                return UniTask.CompletedTask;
            }
            
            return _presenter.EnterAsync(token);
        }

        public async UniTask<AppStateExitResult> RunAsync(CancellationToken token)
        {
            if (!RuntimeMode.IsDedicatedServer)
            {
                await _presenter.RunAsync(token);
            }

#if UNITY_SERVER
            return AppStateExitResult.SwitchTo(AppStateId.Gameplay);
#else
            return AppStateExitResult.SwitchTo(AppStateId.MainMenu);
#endif
        }

        public UniTask ExitAsync(CancellationToken token)
        {
            return !RuntimeMode.IsDedicatedServer ? 
                _presenter.ExitAsync(token) : UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _presenter.Dispose();
        }
    }
}