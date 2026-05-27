using Core.AppStates.Components;
using Features.Gameplay.States.GameplayState;
using Features.Gameplay.States.PauseState;
using Features.Shared.SettingsState;
using VContainer;

namespace Features.Gameplay
{
    public class GameplayStateInstaller : AppStateInstaller
    {
        public override void RegisterDependencies(IContainerBuilder builder)
        {
            builder.Register<GameplayModel>(Lifetime.Singleton);
            builder.Register<GameplayPresenter>(Lifetime.Singleton);
            builder.Register<PausePresenter>(Lifetime.Singleton);
            builder.Register<SettingsPresenter>(Lifetime.Singleton);
            builder.Register<GameplayFlowController>(Lifetime.Singleton);
            builder.Register<GameplayAppStateController>(Lifetime.Singleton);
        }
    }
}