using System;
using System.Threading;
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
                    await _roomSessionService.CreateRoomAsync(
                        playerName: "Host",
                        cancellationToken: token);

                UniTask<PopupClosed> popupTask = _popupService.ShowAsync(
                    new MessagePopupRequest(
                        title: "Room created",
                        message:
                        $"Room code: {session.RoomCode}\n\n" +
                        "Give this code to the second player. " +
                        "The game will start when they join.",
                        closeText: "Cancel"),
                    token);

                UniTask<RoomSessionData> serverReadyTask =
                    _roomSessionService.WaitForServerReadyAsync(token);

                var result = await UniTask.WhenAny(popupTask, serverReadyTask);

                if (result.winArgumentIndex == 0)
                {
                    CancelRoomFlow();
                    return;
                }

                RoomSessionData readySession = result.result2;
                ConnectAndEnterGameplay(readySession);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                await _popupService.ShowAsync(
                    new MessagePopupRequest(
                        "Room creation failed",
                        exception.Message,
                        "Close"));
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

                await _roomSessionService.JoinRoomAsync(
                    inputResult.Text,
                    playerName: "Client",
                    cancellationToken: token);

                await _popupService.ShowAsync(
                    new TimedPopupRequest(
                        icon: null,
                        title: "Joined room",
                        description: "Waiting for server...",
                        amountText: string.Empty,
                        duration: 1.5f),
                    token);

                RoomSessionData readySession =
                    await _roomSessionService.WaitForServerReadyAsync(token);

                ConnectAndEnterGameplay(readySession);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                await _popupService.ShowAsync(
                    new MessagePopupRequest(
                        "Join failed",
                        exception.Message,
                        "Close"));
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
        
        public void Dispose()
        {
            _createRoomClickedCommand.Dispose();
            _playRequestedCommand.Dispose();
            _settingsCommand.Dispose();
            _quitRequestedCommand.Dispose();
            _disposables.Dispose();
        }
    }
}