using System;
using System.Threading;
using Core.Input.Contracts;
using Core.Input.Runtime;
using Core.Patterns.MVP;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Bootstrap
{
   public sealed class BootstrapPresenter : IPresenter
    {
        private readonly BootstrapModel _model;
        private readonly IWindowService _windowService;
        private readonly IInputService _inputService;

        private BootstrapView _view;

        public BootstrapPresenter(
            BootstrapModel model,
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

            _view = await _windowService.GetOrCreateAsync<BootstrapView>(
                WindowId.BootstrapLoadingScreen,
                token);
            
            token.ThrowIfCancellationRequested();

            _view.SetVersion(Application.version);
            _view.SetProgress(0f);
            _view.SetStatus("Starting...");

            await _view.ShowAsync();
        }

        public async UniTask RunAsync(CancellationToken token)
        {
            var progress = new Progress<float>(value =>
            {
                _view?.SetProgress(value);
            });

            await _model.RunStartupTasksAsync(
                progress,
                status => _view?.SetStatus(status),
                token);

            _view?.SetLoadingCompleted();

            await UniTask.Delay(500, cancellationToken: token);
        }

        public async UniTask ExitAsync(CancellationToken token = default)
        {

            if (_view != null)
                await _view.HideAsync();
        }

        public void Dispose()
        {
            // No unmanaged/runtime resources yet.
        }
    }
}