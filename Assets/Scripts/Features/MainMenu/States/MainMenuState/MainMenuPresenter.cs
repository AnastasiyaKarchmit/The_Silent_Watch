using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Audio.Contracts;
using Core.Input.Contracts;
using Core.Input.Runtime;
using Core.Networking;
using Core.Patterns.MVP;
using Core.UI.Popups.Contracts;
using Core.UI.Popups.Requests;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;
using Features.MainMenu.Networking.Rooms.Contracts;
using Features.MainMenu.Networking.Rooms.Data;
using R3;
using UnityEngine;

namespace Features.MainMenu.States.MainMenuState
{
    public sealed class MainMenuPresenter : IPresenter
    {
         private readonly MainMenuModel _model;
        private readonly IWindowService _windowService;
        private readonly IInputService _inputService;
        private readonly IUISoundPlayer _uiSoundPlayer;
        private readonly IPopupService _popupService;
        private readonly IRoomSessionService _roomSessionService;
        private readonly MirrorConnectionService _mirrorConnectionService;


        private readonly ReactiveCommand<Unit> _createRoomClickedCommand = new();
        private readonly ReactiveCommand<Unit> _joinRoomClickedCommand = new();
        private readonly ReactiveCommand<Unit> _playRequestedCommand = new();
        private readonly ReactiveCommand<Unit> _settingsCommand = new();
        private readonly ReactiveCommand<Unit> _quitClickedCommand = new();
        private readonly ReactiveCommand<Unit> _quitRequestedCommand = new();

        private readonly CompositeDisposable _disposables = new();
        
        private MainMenuView _view;
        private CancellationTokenSource _roomFlowCts;

        private bool _isHandlingCreateRoom;
        private bool _isHandlingJoinRoom;
        private bool _isHandlingQuitClick;

        public Observable<Unit> PlayRequested => _playRequestedCommand;
        public Observable<Unit> SettingsRequested => _settingsCommand;
        public Observable<Unit> QuitRequested => _quitRequestedCommand;

        public MainMenuPresenter(
            MainMenuModel model,
            IWindowService windowService,
            IInputService inputService,
            IUISoundPlayer uiSoundPlayer, IPopupService popupService,
            IRoomSessionService roomSessionService,
            MirrorConnectionService mirrorConnectionService)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _uiSoundPlayer = uiSoundPlayer ?? throw new ArgumentNullException(nameof(uiSoundPlayer));
            _popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));
            _roomSessionService = roomSessionService ?? throw new ArgumentNullException(nameof(roomSessionService));
            _mirrorConnectionService = mirrorConnectionService ?? throw new ArgumentNullException(nameof(mirrorConnectionService));

            SubscribeToEvents();
        }

        public async UniTask EnterAsync(CancellationToken token = default)
        {
            _inputService.SetMode(InputMode.UIOnly);
            
            _view = await _windowService.GetOrCreateAsync<MainMenuView>(
                WindowId.MainMenu,
                token);

            token.ThrowIfCancellationRequested();

            _view.Initialize(
                _createRoomClickedCommand,
                _joinRoomClickedCommand,
                _settingsCommand,
                _quitClickedCommand);


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
            _createRoomClickedCommand
                .Subscribe(_ =>
                {
                    _uiSoundPlayer.PlayButtonClick();
                    HandleCreateRoomClickedAsync().Forget();
                })
                .AddTo(_disposables);

            _joinRoomClickedCommand
                .Subscribe(_ =>
                {
                    _uiSoundPlayer.PlayButtonClick();
                    HandleJoinRoomClickedAsync().Forget();
                })
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
        
        private async UniTask HandleCreateRoomClickedAsync()
        {
            if (_isHandlingCreateRoom)
                return;

            _isHandlingCreateRoom = true;
            ResetRoomFlow();

            try
            {
                CancellationToken token = _roomFlowCts.Token;

                RoomSessionData session =
                    await RunWithBlockingPopupAsync(
                        title: "Creating room",
                        message:
                        "Creating your room...\n\n" +
                        "The backend may need a moment to wake up. Please wait.",
                        cancelText: "Cancel",
                        operation: ct => _roomSessionService.CreateRoomAsync(
                            playerName: "Host",
                            cancellationToken: ct),
                        token: token);

                RoomSessionData readySession =
                    await WaitForServerReadyWithPopupAsync(
                        title: "Room created",
                        message:
                        $"Room code: {session.RoomCode}\n\n" +
                        "Give this code to the second player.\n\n" +
                        "Waiting for the second player. When they join, the server will be created automatically. " +
                        "This can take a little time.",
                        cancelText: "Cancel room",
                        endRoomOnCancel: true,
                        token: token);

                ConnectAndEnterGameplay(readySession);
            }
            catch (OperationCanceledException)
            {
                // Player cancelled.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                await ShowErrorPopupAsync(
                    title: "Room creation failed",
                    message: exception.Message);
            }
            finally
            {
                _isHandlingCreateRoom = false;
            }
        }
        
        private async UniTask HandleJoinRoomClickedAsync()
        {
            if (_isHandlingJoinRoom)
                return;

            _isHandlingJoinRoom = true;
            ResetRoomFlow();

            try
            {
                CancellationToken token = _roomFlowCts.Token;

                TextInputPopupResult inputResult = await _popupService.ShowAsync(
                    new TextInputPopupRequest(
                        title: "Join room",
                        message: "Enter the room code from the host.",
                        placeholder: "Room code",
                        confirmText: "Join",
                        cancelText: "Cancel"),
                    token);

                if (!inputResult.Confirmed || string.IsNullOrWhiteSpace(inputResult.Text))
                    return;

                await RunWithBlockingPopupAsync(
                    title: "Joining room",
                    message:
                    "Checking room code...\n\n" +
                    "Please wait.",
                    cancelText: "Cancel",
                    operation: ct => _roomSessionService.JoinRoomAsync(
                        inputResult.Text,
                        playerName: "Client",
                        cancellationToken: ct),
                    token: token);

                RoomSessionData readySession =
                    await WaitForServerReadyWithPopupAsync(
                        title: "Room code accepted",
                        message:
                        "You joined the room.\n\n" +
                        "The server is being created. You will enter the game automatically when everything is ready.",
                        cancelText: "Leave room",
                        endRoomOnCancel: true,
                        token: token);

                ConnectAndEnterGameplay(readySession);
            }
            catch (OperationCanceledException)
            {
                // Player cancelled.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                await ShowErrorPopupAsync(
                    title: "Join failed",
                    message: GetFriendlyJoinError(exception));
            }
            finally
            {
                _isHandlingJoinRoom = false;
            }
        }
        
        private void ConnectAndEnterGameplay(RoomSessionData session)
        {
            if (!session.HasServerEndpoint)
            {
                Debug.LogError("[MainMenuPresenter] Server endpoint is missing.");
                return;
            }

            _mirrorConnectionService.Connect(session.Host, session.Port);

            _playRequestedCommand.Execute(Unit.Default);
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

        private void ResetRoomFlow()
        {
            CancelRoomFlow();
            _roomFlowCts = new CancellationTokenSource();
        }

        private void CancelRoomFlow()
        {
            if (_roomFlowCts == null)
                return;

            _roomFlowCts.Cancel();
            _roomFlowCts.Dispose();
            _roomFlowCts = null;
        }
        
        private async UniTask<RoomSessionData> WaitForServerReadyWithPopupAsync(
            string title,
            string message,
            string cancelText,
            bool endRoomOnCancel,
            CancellationToken token)
        {
            using var popupCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            Task<bool> popupTask = ShowBlockingMessagePopupAsync(
                    title,
                    message,
                    cancelText,
                    popupCts.Token)
                .AsTask();

            Task<RoomSessionData> serverReadyTask =
                _roomSessionService.WaitForServerReadyAsync(token).AsTask();

            try
            {
                Task completedTask = await Task.WhenAny(popupTask, serverReadyTask);

                if (completedTask == popupTask)
                {
                    bool closedByUser = await popupTask;

                    if (closedByUser && endRoomOnCancel)
                        await TryEndRoomAsync();

                    throw new OperationCanceledException();
                }

                return await serverReadyTask;
            }
            finally
            {
                popupCts.Cancel();
                await SuppressPopupTaskAsync(popupTask);
            }
        }

        private async UniTask<bool> ShowBlockingMessagePopupAsync(
            string title,
            string message,
            string closeText,
            CancellationToken token)
        {
            try
            {
                await _popupService.ShowAsync(
                    new MessagePopupRequest(
                        title: title,
                        message: message,
                        closeText: closeText),
                    token);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private async UniTask SuppressPopupTaskAsync(Task<bool> popupTask)
        {
            try
            {
                await popupTask;
            }
            catch (OperationCanceledException)
            {
                // Popup was closed by token. Expected.
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MainMenuPresenter] Popup task finished with error: {exception.Message}");
            }
        }

        private async UniTask<T> RunWithBlockingPopupAsync<T>(
            string title,
            string message,
            string cancelText,
            Func<CancellationToken, UniTask<T>> operation,
            CancellationToken token)
        {
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            using var popupCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            Task<bool> popupTask = ShowBlockingMessagePopupAsync(
                    title,
                    message,
                    cancelText,
                    popupCts.Token)
                .AsTask();

            Task<T> operationTask = operation(operationCts.Token).AsTask();

            try
            {
                Task completedTask = await Task.WhenAny(popupTask, operationTask);

                if (completedTask == popupTask)
                {
                    bool closedByUser = await popupTask;

                    if (closedByUser)
                        operationCts.Cancel();

                    throw new OperationCanceledException();
                }

                return await operationTask;
            }
            finally
            {
                popupCts.Cancel();
                await SuppressPopupTaskAsync(popupTask);
            }
        }
        
        private async UniTask TryEndRoomAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _roomSessionService.EndRoomAsync(cts.Token);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MainMenuPresenter] Failed to end room: {exception.Message}");
            }
        }

        private async UniTask ShowErrorPopupAsync(string title, string message)
        {
            await _popupService.ShowAsync(
                new MessagePopupRequest(
                    title: title,
                    message: message,
                    closeText: "Close"));
        }

        private string GetFriendlyJoinError(Exception exception)
        {
            string message = exception.Message;

            if (message.Contains("409") ||
                message.Contains("not joinable", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Room is full", StringComparison.OrdinalIgnoreCase))
            {
                return "This room was cancelled, already started, or is no longer available.";
            }

            return message;
        }
        
        
        public void Dispose()
        {
            CancelRoomFlow();

            _createRoomClickedCommand.Dispose();
            _joinRoomClickedCommand.Dispose();
            _playRequestedCommand.Dispose();
            _settingsCommand.Dispose();
            _quitClickedCommand.Dispose();
            _quitRequestedCommand.Dispose();

            _disposables.Dispose();
        }
    }
}