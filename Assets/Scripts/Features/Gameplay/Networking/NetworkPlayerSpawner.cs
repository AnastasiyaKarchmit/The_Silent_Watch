using System;
using Features.Gameplay.Networking.Contracts;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace Features.Gameplay.Networking
{
    public sealed class NetworkPlayerSpawner : MonoBehaviour, INetworkPlayerSpawner, IDisposable
    {
        [SerializeField] private Transform[] spawnPoints;

         private NetworkPlayerSpawnRegistry _registry;
        private int _nextSpawnIndex;

        public bool IsReady => spawnPoints != null && spawnPoints.Length > 0;
        public Scene SpawnScene => gameObject.scene;

        [Inject]
        public void Construct(NetworkPlayerSpawnRegistry registry)
        {
            _registry = registry;
            _registry.Register(this);
        }
        
        [Server]
        public GameObject CreatePlayer(
            GameObject playerPrefab,
            NetworkConnectionToClient connection)
        {
            Transform spawnPoint = GetNextSpawnPoint();

            Vector3 position = spawnPoint != null
                ? spawnPoint.position
                : Vector3.zero;

            Quaternion rotation = spawnPoint != null
                ? spawnPoint.rotation
                : Quaternion.identity;

            Debug.Log(
                $"[NetworkPlayerSpawner] Creating player for conn={connection.connectionId} " +
                $"at {position}, scene={SpawnScene.name}");

            GameObject player = Instantiate(playerPrefab, position, rotation);

            player.name = $"Player_{connection.connectionId}";

            SceneManager.MoveGameObjectToScene(player, SpawnScene);

            Debug.Log(
                $"[NetworkPlayerSpawner] Player moved to scene={player.scene.name}, " +
                $"position={player.transform.position}");

            return player;
        }

        private Transform GetNextSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[NetworkPlayerSpawner] No spawn points assigned.");
                return null;
            }

            Transform spawnPoint = spawnPoints[_nextSpawnIndex % spawnPoints.Length];
            _nextSpawnIndex++;

            Debug.Log($"[NetworkPlayerSpawner] Selected spawn point: {spawnPoint.name}");

            return spawnPoint;
        }

        public void Dispose()
        {
            if (_registry != null)
                _registry.Unregister(this);
        }
    }
}