using System;
using System.Text;
using System.Threading;
using Core.Application;
using Cysharp.Threading.Tasks;
using Features.MainMenu.Networking.Rooms.Contracts;
using Features.MainMenu.Networking.Rooms.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Features.MainMenu.Networking.Rooms.Runtime
{
    public sealed class RoomSessionService : IRoomSessionService
    {
        private readonly BackendConnectionConfig _config;
        private readonly IAppLifecycleService _appLifecycleService;

        private bool _endRoomRequestSent;

        public RoomSessionData CurrentSession { get; private set; }

        public RoomSessionService(
            BackendConnectionConfig config,
            IAppLifecycleService appLifecycleService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _appLifecycleService = appLifecycleService ?? throw new ArgumentNullException(nameof(appLifecycleService));

            _appLifecycleService.ApplicationQuitRequested += OnApplicationQuitRequested;
        }

        public async UniTask<RoomSessionData> CreateRoomAsync(
            string playerName,
            CancellationToken cancellationToken)
        {
            var request = new CreateRoomRequest
            {
                playerName = NormalizePlayerName(playerName)
            };

            CreateRoomResponse response =
                await PostAsync<CreateRoomRequest, CreateRoomResponse>(
                    "/rooms",
                    request,
                    cancellationToken);

            CurrentSession = new RoomSessionData
            {
                RoomCode = response.roomCode,
                PlayerId = response.playerId,
                Status = response.status
            };

            return CurrentSession;
        }

        public async UniTask<RoomSessionData> JoinRoomAsync(
            string roomCode,
            string playerName,
            CancellationToken cancellationToken)
        {
            roomCode = NormalizeRoomCode(roomCode);

            var request = new JoinRoomRequest
            {
                playerName = NormalizePlayerName(playerName)
            };

            JoinRoomResponse response =
                await PostAsync<JoinRoomRequest, JoinRoomResponse>(
                    $"/rooms/{roomCode}/join",
                    request,
                    cancellationToken);

            CurrentSession = new RoomSessionData
            {
                RoomCode = response.roomCode,
                PlayerId = response.playerId,
                Status = response.status
            };

            return CurrentSession;
        }

        public async UniTask<RoomSessionData> SetReadyAsync(
            bool isReady,
            CancellationToken cancellationToken)
        {
            EnsureSessionExists();

            var request = new ReadyRequest
            {
                playerId = CurrentSession.PlayerId,
                isReady = isReady
            };

            RoomStatusResponse response =
                await PostAsync<ReadyRequest, RoomStatusResponse>(
                    $"/rooms/{CurrentSession.RoomCode}/ready",
                    request,
                    cancellationToken);

            ApplyStatus(response);
            return CurrentSession;
        }

        public async UniTask<RoomSessionData> StartRoomAsync(
            CancellationToken cancellationToken)
        {
            EnsureSessionExists();

            var request = new StartRoomRequest
            {
                playerId = CurrentSession.PlayerId
            };

            RoomStatusResponse response =
                await PostAsync<StartRoomRequest, RoomStatusResponse>(
                    $"/rooms/{CurrentSession.RoomCode}/start",
                    request,
                    cancellationToken);

            ApplyStatus(response);
            return CurrentSession;
        }

        public async UniTask<RoomSessionData> RefreshStatusAsync(
            CancellationToken cancellationToken)
        {
            EnsureSessionExists();

            RoomStatusResponse response =
                await GetAsync<RoomStatusResponse>(
                    $"/rooms/{CurrentSession.RoomCode}/status",
                    cancellationToken);

            ApplyStatus(response);
            return CurrentSession;
        }

        public async UniTask<RoomSessionData> WaitForServerReadyAsync(
            CancellationToken cancellationToken)
        {
            EnsureSessionExists();

            while (!cancellationToken.IsCancellationRequested)
            {
                RoomSessionData session = await RefreshStatusAsync(cancellationToken);

                if (session.HasServerEndpoint)
                    return session;

                if (session.Status == "Ended")
                {
                    throw new InvalidOperationException(
                        "The room was cancelled or is no longer available.");
                }

                if (session.Status == "Failed")
                {
                    string error = string.IsNullOrWhiteSpace(session.Error)
                        ? "Server creation failed."
                        : session.Error;

                    throw new InvalidOperationException(error);
                }

                await UniTask.Delay(
                    TimeSpan.FromSeconds(2),
                    cancellationToken: cancellationToken);
            }

            throw new OperationCanceledException();
        }

        public async UniTask EndRoomAsync(CancellationToken cancellationToken)
        {
            if (_endRoomRequestSent)
                return;

            if (CurrentSession == null || string.IsNullOrWhiteSpace(CurrentSession.RoomCode))
                return;

            _endRoomRequestSent = true;

            await PostAsync(
                $"/rooms/{CurrentSession.RoomCode}/ended",
                new EndRoomRequest(),
                cancellationToken);

            CurrentSession.Status = "Ended";
        }
        
        private void OnApplicationQuitRequested()
        {
            EndRoomOnQuitAsync().Forget();
        }

        private async UniTaskVoid EndRoomOnQuitAsync()
        {
            if (CurrentSession == null)
                return;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await EndRoomAsync(cts.Token);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[RoomSessionService] Failed to end room on quit: {exception.Message}");
            }
        }
        
        private async UniTask<TResponse> GetAsync<TResponse>(
            string path,
            CancellationToken cancellationToken)
        {
            string url = $"{_config.BaseUrl}{path}";

            using UnityWebRequest request = UnityWebRequest.Get(url);

            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(
                    $"GET {url} failed: {request.responseCode} {request.error}\n{request.downloadHandler.text}");

            return JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
        }

        private async UniTask<TResponse> PostAsync<TRequest, TResponse>(
            string path,
            TRequest body,
            CancellationToken cancellationToken)
        {
            string url = $"{_config.BaseUrl}{path}";
            string json = JsonUtility.ToJson(body);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(
                    $"POST {url} failed: {request.responseCode} {request.error}\n{request.downloadHandler.text}");

            return JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
        }

        private async UniTask PostAsync<TRequest>(
            string path,
            TRequest body,
            CancellationToken cancellationToken)
        {
            string url = $"{_config.BaseUrl}{path}";
            string json = JsonUtility.ToJson(body);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(
                    $"POST {url} failed: {request.responseCode} {request.error}\n{request.downloadHandler.text}");
        }
        
        private void ApplyStatus(RoomStatusResponse response)
        {
            EnsureSessionExists();

            CurrentSession.Status = response.status;
            CurrentSession.Host = response.host;
            CurrentSession.Port = response.port > 0
                ? (ushort)response.port
                : default;
            CurrentSession.Error = response.error;
        }
        
        private void EnsureSessionExists()
        {
            if (CurrentSession == null)
                throw new InvalidOperationException("Room session has not been created or joined yet.");
        }
        
        public void Dispose()
        {
            _appLifecycleService.ApplicationQuitRequested -= OnApplicationQuitRequested;
        }

        private static string NormalizePlayerName(string playerName)
        {
            return string.IsNullOrWhiteSpace(playerName)
                ? "Player"
                : playerName.Trim();
        }

        private static string NormalizeRoomCode(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
                throw new ArgumentException("Room code is empty.");

            return roomCode.Trim().ToUpperInvariant();
        }
        
        #region Requests

        [Serializable]
        private sealed class CreateRoomRequest
        {
            public string playerName;
        }

        [Serializable]
        private sealed class JoinRoomRequest
        {
            public string playerName;
        }

        [Serializable]
        private sealed class ReadyRequest
        {
            public string playerId;
            public bool isReady;
        }

        [Serializable]
        private sealed class StartRoomRequest
        {
            public string playerId;
        }
        
        [Serializable]
        private sealed class EndRoomRequest
        {
        }
        #endregion

        #region Responses

        [Serializable]
        private sealed class CreateRoomResponse
        {
            public string roomCode;
            public string playerId;
            public string status;
        }

        [Serializable]
        private sealed class JoinRoomResponse
        {
            public string roomCode;
            public string playerId;
            public string status;
        }

        [Serializable]
        private sealed class RoomStatusResponse
        {
            public string status;
            public string host;
            public int port;
            public string error;
        }

        [Serializable]
        private sealed class EmptyResponse
        {
        }
        #endregion
    }
}