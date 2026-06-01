using Mirror;
using UnityEngine;

namespace Features
{
    public sealed class TestNetworkManager : NetworkManager
    {
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Transform start = GetStartPosition();

            GameObject player = start != null
                ? Instantiate(playerPrefab, start.position, start.rotation)
                : Instantiate(playerPrefab);

            NetworkServer.AddPlayerForConnection(conn, player);
        }
    }
}