using System.Collections.Generic;
using Core.AppStates.Components;
using Features.Bootstrap.Startup;
using Features.Bootstrap.Startup.Tasks;
using VContainer;

namespace Features.Bootstrap
{
    public sealed class BootstrapAppStateInstaller : AppStateInstaller
    {
        public override void RegisterDependencies(IContainerBuilder builder)
        {
            builder.Register<ConfigureApplicationTask>(Lifetime.Singleton);
            builder.Register<InitializeTweeningTask>(Lifetime.Singleton);

            builder.Register<IReadOnlyList<IStartupTask>>(resolver => new IStartupTask[]
            {
                resolver.Resolve<ConfigureApplicationTask>(),
                resolver.Resolve<InitializeTweeningTask>()
            }, Lifetime.Singleton);

            builder.Register<StartupTaskRunner>(Lifetime.Singleton);
            builder.Register<BootstrapModel>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
            builder.Register<BootstrapPresenter>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
            builder.Register<BootstrapAppStateController>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
        }
        
        
    }
}