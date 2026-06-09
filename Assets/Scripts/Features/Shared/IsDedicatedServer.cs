namespace Features.Shared
{
    public static class RuntimeMode
    {
        public static bool IsDedicatedServer
        {
            get
            {
#if UNITY_EDITOR
                return false;
#elif UNITY_SERVER
                return true;
#else
                return Application.isBatchMode ||
                       Application.platform == RuntimePlatform.LinuxServer ||
                       Application.platform == RuntimePlatform.WindowsServer ||
                       Application.platform == RuntimePlatform.OSXServer;
#endif
            }
        }
    }
}