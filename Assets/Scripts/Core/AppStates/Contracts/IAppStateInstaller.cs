using VContainer;

namespace Core.AppStates.Contracts
{
    public interface IAppStateInstaller
    {
        void RegisterDependencies(IContainerBuilder builder);
        void CleanupBeforeInstall();
    }
}