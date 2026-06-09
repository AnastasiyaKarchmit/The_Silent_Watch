using System;
using System.Threading;
using Core.Input.Contracts;
using Cysharp.Threading.Tasks;
using Mirror;
using UnityEngine;

namespace Features.Gameplay.Networking
{
    public sealed class GameplayNetworkSessionService
    {
        private readonly IInputService _inputService;
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _localPlayerTimeout = TimeSpan.FromSeconds(10);

        private bool _addPlayerRequested;

        public GameplayNetworkSessionService(IInputService inputService)
        {
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        }

        public async UniTask EnterGameplayAsync(CancellationToken token)
        {
            if (NetworkServer.active && !NetworkClient.active)
            {
                Debug.Log("[GameplayNetworkSessionService] Server-only mode. Skip client join.");
                return;
            }

            await WaitForClientConnectionAsync(token);

            if (!NetworkClient.ready)
            {
                Debug.Log("[GameplayNetworkSessionService] Sending Ready.");
                NetworkClient.Ready();
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);

            if (NetworkClient.localPlayer == null && !_addPlayerRequested)
            {
                _addPlayerRequested = true;

                Debug.Log("[GameplayNetworkSessionService] Sending AddPlayer.");
                NetworkClient.AddPlayer();
            }

            await WaitForLocalPlayerAsync(token);
            InitializeLocalPlayerInput();
        }

        public UniTask ExitGameplayAsync(CancellationToken token)
        {
            _addPlayerRequested = false;

            if (NetworkClient.active)
            {
                Debug.Log("[GameplayNetworkSessionService] Stopping client.");
                NetworkManager.singleton.StopClient();
            }

            return UniTask.CompletedTask;
        }

        private async UniTask WaitForClientConnectionAsync(CancellationToken token)
        {
            float timer = 0f;

            while (!NetworkClient.active || !NetworkClient.isConnected)
            {
                token.ThrowIfCancellationRequested();

                timer += Time.unscaledDeltaTime;

                if (timer >= _connectionTimeout.TotalSeconds)
                    throw new TimeoutException("Mirror client connection timed out before gameplay started.");

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            Debug.Log("[GameplayNetworkSessionService] Client connected.");
        }

        private async UniTask WaitForLocalPlayerAsync(CancellationToken token)
        {
            float timer = 0f;

            while (NetworkClient.localPlayer == null)
            {
                token.ThrowIfCancellationRequested();

                timer += Time.unscaledDeltaTime;

                if (timer >= _localPlayerTimeout.TotalSeconds)
                    throw new TimeoutException("Local network player was not spawned in time.");

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            Debug.Log($"[GameplayNetworkSessionService] Local player ready: {NetworkClient.localPlayer.netId}");
        }

        private void InitializeLocalPlayerInput()
        {
            if (NetworkClient.localPlayer == null)
                return;

            ServerAuthPredictedPlayerController controller =
                NetworkClient.localPlayer.GetComponent<ServerAuthPredictedPlayerController>();

            if (controller == null)
            {
                Debug.LogWarning("[GameplayNetworkSessionService] Local player has no predicted movement controller.");
                return;
            }

            controller.InitializeLocalInput(_inputService);
        }
    }
}