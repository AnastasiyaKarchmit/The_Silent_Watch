using Mirror;
using UnityEngine;

namespace Core.Networking
{
    public sealed class MirrorConnectionService
    {
        private readonly NetworkManager _networkManager;

        public MirrorConnectionService(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public void Connect(string host, ushort port)
        {
            if (NetworkClient.active || NetworkServer.active)
            {
                Debug.LogWarning("[MirrorConnectionService] Mirror is already active.");
                return;
            }

            _networkManager.networkAddress = host;

            if (_networkManager.transport is PortTransport portTransport)
            {
                portTransport.Port = port;
            }
            else
            {
                Debug.LogWarning("[MirrorConnectionService] Transport is not PortTransport.");
            }

            Debug.Log($"[MirrorConnectionService] Connecting to {host}:{port}");
            _networkManager.StartClient();
        }

        public void Disconnect()
        {
            if (NetworkClient.active)
                _networkManager.StopClient();
        }
    }
}