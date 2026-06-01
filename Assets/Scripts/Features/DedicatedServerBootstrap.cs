using System;
using Mirror;
using UnityEngine;

namespace Features
{
    public sealed class DedicatedServerBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private ushort fallbackPort = 7777;

        private void Awake()
        {
            if (networkManager == null)
                networkManager = FindFirstObjectByType<NetworkManager>();

            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            if (!ShouldStartDedicatedServer())
            {
                Debug.Log("[DedicatedServer] Not a dedicated server build. Skipping auto-start.");
                return;
            }

            ushort port = ResolveServerPort();
            ApplyPort(port);

            Debug.Log($"[DedicatedServer] Starting Mirror server on port {port}");

            networkManager.StartServer();

            Debug.Log($"[DedicatedServer] Server active: {NetworkServer.active}");
        }

        private bool ShouldStartDedicatedServer()
        {
#if UNITY_EDITOR
            return false;
#elif UNITY_SERVER
            return true;
#else
            return Application.isBatchMode;
#endif
        }

        private ushort ResolveServerPort()
        {
            string edgegapPort = Environment.GetEnvironmentVariable("ARBITRIUM_PORT_GAMEPORT_INTERNAL");

            if (ushort.TryParse(edgegapPort, out ushort parsedEdgegapPort))
                return parsedEdgegapPort;

            return fallbackPort;
        }

        private void ApplyPort(ushort port)
        {
            if (networkManager.transport is PortTransport portTransport)
            {
                portTransport.Port = port;
                Debug.Log($"[DedicatedServer] Applied port {port} to {portTransport.GetType().Name}");
                return;
            }

            Debug.LogWarning(
                $"[DedicatedServer] NetworkManager transport does not inherit PortTransport. " +
                $"Current transport: {networkManager.transport?.GetType().Name ?? "null"}");
        }
    }
}