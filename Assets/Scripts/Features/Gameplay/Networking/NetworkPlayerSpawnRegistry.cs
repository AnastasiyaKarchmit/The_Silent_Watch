using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Features.Gameplay.Networking.Contracts;
using UnityEngine;

namespace Features.Gameplay.Networking
{
    public sealed class NetworkPlayerSpawnRegistry
    {
        private INetworkPlayerSpawner _currentSpawner;

        public bool HasSpawner => _currentSpawner is { IsReady: true };

        public INetworkPlayerSpawner CurrentSpawner
        {
            get
            {
                if (_currentSpawner == null)
                    throw new InvalidOperationException("Network player spawner is not registered.");

                return _currentSpawner;
            }
        }

        public void Register(INetworkPlayerSpawner spawner)
        {
            _currentSpawner = spawner;
            Debug.Log($"[NetworkPlayerSpawnRegistry] Registered spawner: {spawner}");
        }

        public void Unregister(INetworkPlayerSpawner spawner)
        {
            if (_currentSpawner != spawner)
                return;

            _currentSpawner = null;
            Debug.Log("[NetworkPlayerSpawnRegistry] Unregistered spawner.");
        }

        public async UniTask<bool> WaitForSpawnerAsync(
            TimeSpan timeout,
            CancellationToken token = default)
        {
            float timer = 0f;

            while (!HasSpawner)
            {
                token.ThrowIfCancellationRequested();

                timer += Time.unscaledDeltaTime;

                if (timer >= timeout.TotalSeconds)
                    return false;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            return true;
        }
    }
}