using System;
using System.Threading;
using Core.AppStates.Contracts.State;
using Core.AppStates.Data;
using Core.SceneManagement.Loading.Contracts;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Core.UI.Windows.Extensions;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Features.MainMenu
{
    public sealed class MainMenuAppStateController : IAppStateController
    {
        private readonly MainMenuFlowController _flowController;
        private readonly IWindowService _windowService;
        private readonly MainMenuAudioController _audioController;

        private readonly CompositeDisposable _disposables = new();

        private UniTaskCompletionSource<AppStateExitResult> _completionSource;
        private CancellationTokenSource _stateCts;

        private bool _isSwitchRequested;

        public MainMenuAppStateController(
            MainMenuFlowController flowController,
            MainMenuAudioController audioController,
            IWindowService windowService)
        {
            _flowController = flowController ?? throw new ArgumentNullException(nameof(flowController));
            _audioController = audioController ?? throw new ArgumentNullException(nameof(audioController));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        }

        public async UniTask EnterAsync(object payload, CancellationToken token)
        {
            _completionSource = new UniTaskCompletionSource<AppStateExitResult>();
            _stateCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            _flowController.PlayRequested
                .Subscribe(_ =>
                {
                    RequestGameplayAsync(_stateCts.Token).Forget(exception =>
                    {
                        if (exception is OperationCanceledException)
                            return;

                        Debug.LogException(exception);
                        _completionSource.TrySetException(exception);
                    });
                })
                .AddTo(_disposables);

            await _audioController.EnterAsync(token);
            await _flowController.EnterAsync(token);
        }

        public async UniTask<AppStateExitResult> RunAsync(CancellationToken token)
        {
            await using var registration = token.Register(() =>
            {
                _completionSource.TrySetCanceled(token);
            });

            return await _completionSource.Task;
        }

        public async UniTask ExitAsync(CancellationToken token)
        {
            _stateCts?.Cancel();

            _disposables.Clear();

            await UniTask.WhenAll(
                _flowController.ExitAsync(token),
                _audioController.ExitAsync(token));
        }

        public void Dispose()
        {
            _stateCts?.Cancel();
            _stateCts?.Dispose();
            _stateCts = null;

            _disposables.Dispose();
            _flowController.Dispose();
            _audioController.Dispose();
        }

        private async UniTask RequestGameplayAsync(CancellationToken token)
        {
            if (_isSwitchRequested)
                return;

            _isSwitchRequested = true;

            var rootWindowService = _windowService.GetRootWindowService();

            var loadingScreen = await rootWindowService.GetOrCreateAsync<ILoadingScreen>(
                WindowId.LoadingScreen,
                token);

            loadingScreen.SetProgress(0f);

            await loadingScreen.ShowAsync();

            var progress = new Progress<float>(loadingScreen.SetProgress);

            var switchOptions = new AppStateSwitchOptions(
                progress: progress,
                onAfterEnterAsync: async _ =>
                {
                    loadingScreen.SetProgress(1f);
                    await loadingScreen.HideAsync();
                });

            _completionSource.TrySetResult(
                AppStateExitResult.SwitchTo(
                    AppStateId.Gameplay,
                    payload: null,
                    switchOptions: switchOptions));
        }
    }
}