using System;
using Cysharp.Threading.Tasks;
using Features.Gameplay.Networking;
using Features.Gameplay.Networking.Contracts;
using Mirror;
using UnityEngine;
using VContainer;

namespace Features
{
    public sealed class SilentWatchNetworkManager : NetworkManager
    {
        private NetworkPlayerSpawnRegistry _spawnRegistry;

        [Inject]
        public void Construct(NetworkPlayerSpawnRegistry spawnRegistry)
        {
            _spawnRegistry = spawnRegistry;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("[NetworkManager] Server started.");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            OnServerAddPlayerAsync(conn).Forget();
        }

        private async UniTaskVoid OnServerAddPlayerAsync(NetworkConnectionToClient conn)
        {
            Debug.Log($"[NetworkManager] OnServerAddPlayer requested for connection {conn.connectionId}");

            bool spawnerReady = await _spawnRegistry.WaitForSpawnerAsync(
                TimeSpan.FromSeconds(10));

            if (!spawnerReady)
            {
                Debug.LogError("[NetworkManager] Player spawner was not registered within timeout.");
                return;
            }

            INetworkPlayerSpawner spawner = _spawnRegistry.CurrentSpawner;

            GameObject player = spawner.CreatePlayer(playerPrefab, conn);

            NetworkServer.AddPlayerForConnection(conn, player);

            Debug.Log(
                $"[NetworkManager] Spawned player for connection {conn.connectionId} " +
                $"at {player.transform.position}, scene={player.scene.name}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Debug.LogWarning($"[NetworkManager] Server disconnected client: {conn.connectionId}");
            base.OnServerDisconnect(conn);
        }
    }
}