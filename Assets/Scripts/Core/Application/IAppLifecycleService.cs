using System;

namespace Core.Application
{
    public interface IAppLifecycleService
    {
        event Action<bool> ApplicationFocusChanged;
        event Action<bool> ApplicationPauseChanged;
        event Action ApplicationQuitRequested;
    }
}