#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;
namespace Core.UI.Network
{
    [InitializeOnLoad]
    public static class BackendDevLauncher
    {
        private const string BackendRelativePath = "SilentWatch.Backend";
        private static Process _process;
        private const bool UseLocalBackendInEditor = false;

        static BackendDevLauncher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.quitting += StopBackend;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!UseLocalBackendInEditor)
                return;
            
            if (state == PlayModeStateChange.EnteredPlayMode)
                StartBackend();
        }

        private static void StartBackend()
        {
            if (_process != null && !_process.HasExited)
                return;
            
            if (IsBackendAlreadyRunning())
            {
                Debug.Log("[BackendDevLauncher] Backend already running on port 5227. Skip auto-start.");
                return;
            }

            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;

            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                Debug.LogError("[BackendDevLauncher] Could not resolve project root.");
                return;
            }

            string backendPath = Path.Combine(projectRoot, BackendRelativePath);

            if (!Directory.Exists(backendPath))
            {
                Debug.LogError($"[BackendDevLauncher] Backend folder not found: {backendPath}");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --no-build --urls http://localhost:5227",
                WorkingDirectory = backendPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            _process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    Debug.Log($"[Backend] {args.Data}");
            };

            _process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    Debug.LogError($"[Backend] {args.Data}");
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            Debug.Log("[BackendDevLauncher] Backend started.");
        }

        private static void StopBackend()
        {
            if (_process == null)
                return;

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                    Debug.Log("[BackendDevLauncher] Backend stopped.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BackendDevLauncher] Failed to stop backend: {exception.Message}");
            }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }
        
        private static bool IsBackendAlreadyRunning()
        {
            try
            {
                using var client = new System.Net.Sockets.TcpClient();
                var result = client.BeginConnect("127.0.0.1", 5227, null, null);
                bool connected = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(300));

                if (!connected)
                    return false;

                client.EndConnect(result);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif