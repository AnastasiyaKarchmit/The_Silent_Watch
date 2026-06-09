using Core.AppStates.Components;
using Features.Gameplay.Networking;
using Features.Gameplay.Networking.Contracts;
using Features.Gameplay.States.GameplayState;
using Features.Gameplay.States.PauseState;
using Features.Shared.SettingsState;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Features.Gameplay
{
    public class GameplayStateInstaller : AppStateInstaller
    {
        [SerializeField] private NetworkPlayerSpawner networkPlayerSpawner;
        
        public override void RegisterDependencies(IContainerBuilder builder)
        {
            builder.Register<GameplayModel>(Lifetime.Singleton);
            builder.Register<GameplayPresenter>(Lifetime.Singleton);
            builder.Register<PausePresenter>(Lifetime.Singleton);
            builder.Register<SettingsPresenter>(Lifetime.Singleton);
            builder.Register<GameplayFlowController>(Lifetime.Singleton);
            builder.Register<GameplayAppStateController>(Lifetime.Singleton);
            
            RegisterNetwork(builder);
        }

        
        private void RegisterNetwork(IContainerBuilder builder)
        {
            builder.Register<GameplayNetworkSessionService>(Lifetime.Singleton);
            builder.RegisterComponent(networkPlayerSpawner)
                .As<INetworkPlayerSpawner>()
                .AsSelf();;
        }
    }
}