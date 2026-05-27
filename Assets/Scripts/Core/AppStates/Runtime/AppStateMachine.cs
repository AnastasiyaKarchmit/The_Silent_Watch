using System;
using System.Threading;
using Core.AppStates.Contracts;
using Core.AppStates.Contracts.State;
using Core.AppStates.Data;
using Core.SceneManagement.AppStateScenes.Contracts;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace Core.AppStates.Runtime
{
    public sealed class AppStateMachine : IAppStateMachine, IStartable, IDisposable
    {
        private readonly IAppSceneCoordinator _sceneCoordinator;
        private readonly IAppStateControllerFactory _stateControllerFactory;
        private readonly IAppStateScopeBuilder _scopeBuilder;
        private readonly IAppTransition _transition;
        private readonly LifetimeScope _rootScope;

        private readonly SemaphoreSlim _transitionLock = new(1, 1);
        private readonly CancellationTokenSource _lifetimeCts = new();

        private IAppStateController _currentStateController;
        private LifetimeScope _currentStateScope;
        private bool _isDisposed;

        public AppStateId? CurrentState { get; private set; }

        public AppStateMachine(
            LifetimeScope rootScope,
            IAppSceneCoordinator sceneCoordinator,
            IAppStateControllerFactory stateControllerFactory,
            IAppStateScopeBuilder scopeBuilder,
            IAppTransition transition)
        {
            _rootScope = rootScope ?? throw new ArgumentNullException(nameof(rootScope));
            _sceneCoordinator = sceneCoordinator ?? throw new ArgumentNullException(nameof(sceneCoordinator));
            _stateControllerFactory = stateControllerFactory ?? throw new ArgumentNullException(nameof(stateControllerFactory));
            _transition = transition ?? throw new ArgumentNullException(nameof(transition));
            _scopeBuilder = scopeBuilder ?? throw new ArgumentNullException(nameof(scopeBuilder));        }

        public void Start()
        {
            StartAsync(_lifetimeCts.Token).Forget(exception =>
            {
                if (exception is OperationCanceledException)
                    return;

                Debug.LogException(exception);
            });
        }

        private async UniTask StartAsync(CancellationToken token)
        {
            await _sceneCoordinator.InitializePersistentScenesAsync(token: token);
            await SwitchToAsync(AppStateId.Bootstrap, token: token);
        }

        public async UniTask SwitchToAsync(
            AppStateId stateId,
            object payload = null,
            CancellationToken token = default)
        {
            ThrowIfDisposed();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                _lifetimeCts.Token);

            CancellationToken linkedToken = linkedCts.Token;

            await _transitionLock.WaitAsync(linkedToken);

            try
            {
                var nextState = stateId;
                var nextPayload = payload;
                AppStateSwitchOptions nextSwitchOptions = null;

                while (!linkedToken.IsCancellationRequested)
                {
                    AppStateExitResult result = await RunStateAsync(
                        nextState,
                        nextPayload,
                        nextSwitchOptions,
                        linkedToken);

                    if (!result.HasNextState)
                        break;

                    nextState = result.NextState.Value;
                    nextPayload = result.Payload;
                    nextSwitchOptions = result.SwitchOptions;
                }
            }
            catch (OperationCanceledException) when (linkedToken.IsCancellationRequested)
            {
            }
            finally
            {
                try
                {
                    _transitionLock.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private async UniTask<AppStateExitResult> RunStateAsync(
            AppStateId stateId,
            object payload,
            AppStateSwitchOptions switchOptions,
            CancellationToken token)
        {
            if (CurrentState == stateId && _currentStateController != null)
            {
                Debug.LogWarning($"App state '{stateId}' is already active.");
                return AppStateExitResult.None;
            }

            await ExitCurrentStateAsync(token);

            try
            {
                if (switchOptions?.OnBeforeLoadAsync != null)
                    await switchOptions.OnBeforeLoadAsync(token);

                await _sceneCoordinator.LoadStateScenesAsync(
                    stateId,
                    switchOptions?.Progress,
                    token);

                _currentStateScope = _scopeBuilder.BuildScope(
                    _rootScope,
                    _sceneCoordinator.CurrentStateScenes);

                _currentStateController = _stateControllerFactory.Create(
                    stateId,
                    _currentStateScope.Container);

                CurrentState = stateId;

                await _currentStateController.EnterAsync(payload, token);

                if (switchOptions?.OnAfterEnterAsync != null)
                    await switchOptions.OnAfterEnterAsync(token);

                await _transition.HideAsync(token);

                var result = await _currentStateController.RunAsync(token);

                await _transition.ShowAsync(token);

                await ExitCurrentStateAsync(token);

                return result;
            }
            catch
            {
                await SafeExitCurrentStateAsync(token);
                throw;
            }
        }

        private async UniTask ExitCurrentStateAsync(CancellationToken token)
        {
            if (_currentStateController == null)
                return;

            IAppStateController stateToExit = _currentStateController;
            LifetimeScope scopeToDispose = _currentStateScope;

            _currentStateController = null;
            _currentStateScope = null;
            CurrentState = null;

            try
            {
                await stateToExit.ExitAsync(token);
            }
            finally
            {
                try
                {
                    stateToExit.Dispose();
                }
                finally
                {
                    scopeToDispose?.Dispose();
                }
            }
        }

        private async UniTask SafeExitCurrentStateAsync(CancellationToken token)
        {
            if (_currentStateController == null)
                return;

            try
            {
                await ExitCurrentStateAsync(token);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AppStateMachine));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            _lifetimeCts.Cancel();

            try
            {
                _currentStateController?.Dispose();
                _currentStateScope?.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            _currentStateController = null;
            _currentStateScope = null;
            CurrentState = null;

            _lifetimeCts.Dispose();
            
            _transitionLock.Dispose();
        }
    }
}