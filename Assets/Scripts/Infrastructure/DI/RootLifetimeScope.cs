using System;
using Core.Application;
using Core.AppStates.Contracts;
using Core.AppStates.Runtime;
using Core.Audio.Configs;
using Core.Audio.Contracts;
using Core.Audio.Runtime;
using Core.Input.Runtime;
using Core.Save;
using Core.Save.JSON;
using Core.Save.SaveStorage;
using Core.SceneManagement.AppStateScenes.Configs;
using Core.SceneManagement.AppStateScenes.Contracts;
using Core.SceneManagement.AppStateScenes.Runtime;
using Core.SceneManagement.Loading.Contracts;
using Core.SceneManagement.Loading.Runtime;
using Core.Settings;
using Core.UI.Popups.Contracts;
using Core.UI.Popups.Runtime;
using Core.UI.Popups.Runtime.Handlers;
using Core.UI.Popups.Runtime.Handlers.Core;
using Core.UI.Windows.Config;
using Core.UI.Windows.Runtime;
using Infrastructure.Factories;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using VContainer;
using VContainer.Unity;

namespace Infrastructure.DI
{
    public class RootLifetimeScope : LifetimeScope
    {
        [Header("References")]
        [SerializeField] private AppSceneDatabase appSceneDatabase;
        [SerializeField] private WindowServiceConfig windowServiceConfig;
        [SerializeField] private GameObject eventSystemPrefab;
        [SerializeField] private AppLifecycleService appLifecycleService;
        [SerializeField] private AudioServiceConfig audioServiceConfig;
        [SerializeField] private AudioDatabase audioDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            RegisterSceneManagement(builder);
            RegisterAppStateSystem(builder);
            RegisterConfigs(builder);
            RegisterAudioSystem(builder);
            RegisterServices(builder);
            RegisterSaveSystem(builder);
            RegisterPopups(builder);
        }

        private void RegisterSceneManagement(IContainerBuilder builder)
        {
            builder.RegisterInstance(appSceneDatabase);

            builder.Register<ISceneLoadService, SceneLoadService>(Lifetime.Singleton);

            builder.Register<IAppSceneRegistry, AppSceneRegistry>(Lifetime.Singleton);
            builder.Register<IAppSceneCoordinator, AppSceneCoordinator>(Lifetime.Singleton);
        }

        private void RegisterAppStateSystem(IContainerBuilder builder)
        {
            builder.Register<IAppTransition, AppTransition>(Lifetime.Singleton);

            builder.Register<IAppStateControllerFactory, AppStateControllerFactory>(Lifetime.Singleton);

            builder.Register<IAppStateScopeBuilder, AppStateScopeBuilder>(Lifetime.Singleton);

            builder.RegisterEntryPoint<AppStateMachine>();
        }
        
        private void RegisterInputService(IContainerBuilder builder)
        {
            GameObject eventSystemInstance = Instantiate(eventSystemPrefab);
            DontDestroyOnLoad(eventSystemInstance);

            EventSystem eventSystem = eventSystemInstance.GetComponent<EventSystem>();
            InputSystemUIInputModule uiInputModule =
                eventSystemInstance.GetComponent<InputSystemUIInputModule>();

            if (eventSystem == null)
                throw new InvalidOperationException(
                    $"{nameof(eventSystemPrefab)} must have an {nameof(EventSystem)} component.");

            if (uiInputModule == null)
                throw new InvalidOperationException(
                    $"{nameof(eventSystemPrefab)} must have an {nameof(InputSystemUIInputModule)} component.");

            builder.RegisterInstance(eventSystem);
            builder.RegisterInstance(uiInputModule);

            builder.Register<InputService>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();
            
            builder.Register<InputGate>(Lifetime.Singleton);
        }

        private void RegisterConfigs(IContainerBuilder builder)
        {
            builder.RegisterInstance(windowServiceConfig);
        }
        
        private void RegisterServices(IContainerBuilder builder)
        {
            builder.Register<AddressableWindowFactory>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.Register<WindowService>(Lifetime.Scoped).AsImplementedInterfaces();
            builder.RegisterComponent(appLifecycleService)
                .As<IAppLifecycleService>();
            builder.RegisterEntryPoint<SettingsService>()
                .As<ISettingsService>()
                .AsSelf();
            RegisterInputService(builder);
        }

        private void RegisterSaveSystem(IContainerBuilder builder)
        {
            builder.Register<IJsonService, JsonService>(Lifetime.Singleton);

#if USE_PLAYER_PREFS_SAVE
            builder.Register<ISaveStorage, PlayerPrefsSaveStorage>(Lifetime.Singleton);
#else
            builder.Register<ISaveStorage, FileSaveStorage>(Lifetime.Singleton);
#endif
            
            builder.RegisterEntryPoint<SaveSystem>().As<ISaveSystem>();
        }
        private void RegisterAudioSystem(IContainerBuilder builder)
        {
            builder.RegisterInstance(audioServiceConfig);
            builder.RegisterInstance<IAudioDatabase>(audioDatabase);
            
            builder.Register<UISoundPlayer>(Lifetime.Singleton)
                .As<IUISoundPlayer>();

            builder.RegisterEntryPoint<AudioService>()
                .As<IAudioService>()
                .AsSelf();
        }

        private void RegisterPopups(IContainerBuilder builder)
        {
            builder.Register<PopupService>(Lifetime.Scoped)
                .As<IPopupService>()
                .AsSelf();
            
            builder.Register<TimedPopupHandler>(Lifetime.Scoped)
                .As<IPopupHandler>()
                .AsSelf();;
            
            builder.Register<ConfirmationPopupHandler>(Lifetime.Scoped)
                .As<IPopupHandler>()
                .AsSelf();;
            
            builder.Register<MessagePopupHandler>(Lifetime.Scoped)
                .As<IPopupHandler>()
                .AsSelf();
        }
    }
}