using System;
using UnityEngine;

namespace Core.Application
{
    public sealed class AppLifecycleService : MonoBehaviour, IAppLifecycleService
    {
        public event Action<bool> ApplicationFocusChanged;
        public event Action<bool> ApplicationPauseChanged;
        public event Action ApplicationQuitRequested;

        private void OnApplicationFocus(bool hasFocus)
        {
            ApplicationFocusChanged?.Invoke(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            ApplicationPauseChanged?.Invoke(pauseStatus);
        }

        private void OnApplicationQuit()
        {
            ApplicationQuitRequested?.Invoke();
        }
    }
}