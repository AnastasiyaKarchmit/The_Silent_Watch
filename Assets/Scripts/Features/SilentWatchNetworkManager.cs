using Mirror;
using UnityEngine;

namespace Features
{
    public sealed class SilentWatchNetworkManager : NetworkManager
    {
        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("[Mirror] Server started.");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            Debug.Log("[Mirror] Server stopped.");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("[Mirror] Client started.");
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            Debug.Log("[Mirror] Client stopped.");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("[Mirror] Client connected successfully.");
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Debug.LogWarning("[Mirror] Client disconnected.");
        }

        public override void OnClientError(TransportError error, string reason)
        {
            base.OnClientError(error, reason);
            Debug.LogError($"[Mirror] Client error: {error}. Reason: {reason}");
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            Debug.Log($"[Mirror] Server: client connected. ConnectionId={conn.connectionId}");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Debug.LogWarning($"[Mirror] Server: client disconnected. ConnectionId={conn.connectionId}");
            base.OnServerDisconnect(conn);
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Transform start = GetStartPosition();

            GameObject player = start != null
                ? Instantiate(playerPrefab, start.position, start.rotation)
                : Instantiate(playerPrefab);

            NetworkServer.AddPlayerForConnection(conn, player);

            Debug.Log($"[Mirror] Server: player spawned for connection {conn.connectionId}");
        }
    }
}