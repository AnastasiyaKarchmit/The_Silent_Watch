using System.Collections;
using kcp2k;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features
{
    public sealed class MirrorClientConnectPanel : MonoBehaviour
    {
        [Header("Mirror")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private KcpTransport kcpTransport;

        [Header("UI")]
        [SerializeField] private TMP_InputField hostInput;
        [SerializeField] private TMP_InputField portInput;
        [SerializeField] private Button connectButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Connection")]
        [SerializeField] private float timeoutSeconds = 15f;

        private Coroutine _connectTimeoutRoutine;

        private void Awake()
        {
            if (networkManager == null)
            {
                networkManager = FindFirstObjectByType<NetworkManager>();
                kcpTransport = networkManager.GetComponent<KcpTransport>();
            }
            
            connectButton.onClick.AddListener(Connect);
        }

        private void OnDestroy()
        {
            connectButton.onClick.RemoveListener(Connect);
        }

        private void Connect()
        {
            string host = hostInput.text.Trim();

            if (string.IsNullOrWhiteSpace(host))
            {
                SetStatus("Host is empty.");
                return;
            }

            if (!ushort.TryParse(portInput.text.Trim(), out ushort port))
            {
                SetStatus("Port is invalid.");
                return;
            }

            networkManager.networkAddress = host;
            kcpTransport.Port = port;

            SetStatus($"Connecting to {host}:{port}...");

            networkManager.StartClient();

            if (_connectTimeoutRoutine != null)
                StopCoroutine(_connectTimeoutRoutine);

            _connectTimeoutRoutine = StartCoroutine(ConnectionTimeoutRoutine());
        }

        private IEnumerator ConnectionTimeoutRoutine()
        {
            float timer = 0f;

            while (timer < timeoutSeconds)
            {
                if (NetworkClient.isConnected)
                {
                    SetStatus("Connected.");
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            SetStatus("Connection timed out.");
            networkManager.StopClient();
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;

            Debug.Log($"[ConnectPanel] {message}");
        }
    }
}