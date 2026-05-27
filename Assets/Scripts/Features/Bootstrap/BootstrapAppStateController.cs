using System;
using System.Threading;
using Core.AppStates.Contracts.State;
using Core.AppStates.Data;
using Cysharp.Threading.Tasks;

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
            return _presenter.EnterAsync(token);
        }

        public async UniTask<AppStateExitResult> RunAsync(CancellationToken token)
        {
            await _presenter.RunAsync(token);
            return AppStateExitResult.SwitchTo(AppStateId.MainMenu);
        }

        public UniTask ExitAsync(CancellationToken token)
        {
            return _presenter.ExitAsync(token);
        }

        public void Dispose()
        {
            _presenter.Dispose();
        }
    }
}