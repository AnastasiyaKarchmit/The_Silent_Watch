using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.Gameplay.Networking.Contracts
{
    public interface INetworkPlayerSpawner
    {
        bool IsReady { get; }
        Scene SpawnScene { get; }

        GameObject CreatePlayer(
            GameObject playerPrefab,
            NetworkConnectionToClient connection);
    }
}