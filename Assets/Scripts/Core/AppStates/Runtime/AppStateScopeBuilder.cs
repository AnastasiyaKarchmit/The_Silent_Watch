using System.Collections.Generic;
using System.Linq;
using Core.AppStates.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace Core.AppStates.Runtime
{
    public interface IAppStateScopeBuilder
    {
        LifetimeScope BuildScope(
            LifetimeScope parentScope,
            IReadOnlyList<string> sceneNames,
            bool cleanupBeforeInstall = true);
    }

    public sealed class AppStateScopeBuilder : IAppStateScopeBuilder
    {
        public LifetimeScope BuildScope(
            LifetimeScope parentScope,
            IReadOnlyList<string> sceneNames,
            bool cleanupBeforeInstall = true)
        {
            var loadedScenes = FindLoadedScenes(sceneNames);
            var installers = FindInstallers(sceneNames);

            if (cleanupBeforeInstall)
            {
                foreach (var installer in installers)
                    installer.CleanupBeforeInstall();
            }

            var stateScope = parentScope.CreateChild(builder =>
            {
                foreach (var installer in installers)
                    installer.RegisterDependencies(builder);
            });

            MoveScopeToOwnerScene(stateScope, loadedScenes);
            
            return stateScope;
        }
        
        private static IReadOnlyList<Scene> FindLoadedScenes(IReadOnlyList<string> sceneNames)
        {
            var result = new List<Scene>();

            foreach (var sceneName in sceneNames)
            {
                var scene = SceneManager.GetSceneByName(sceneName);

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Debug.LogWarning($"Scene '{sceneName}' is not loaded. Cannot use it for app state scope.");
                    continue;
                }

                result.Add(scene);
            }

            return result;
        }

        private static IReadOnlyList<IAppStateInstaller> FindInstallers(
            IReadOnlyList<string> sceneNames)
        {
            var result = new List<IAppStateInstaller>();

            foreach (var sceneName in sceneNames)
            {
                var scene = SceneManager.GetSceneByName(sceneName);

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Debug.LogWarning($"Scene '{sceneName}' is not loaded. Cannot search app state installers.");
                    continue;
                }

                foreach (var rootObject in scene.GetRootGameObjects())
                {
                    var installers = rootObject.GetComponentsInChildren<IAppStateInstaller>(true);
                    result.AddRange(installers);
                }
            }

            return result
                .Distinct()
                .ToArray();
        }
        
        private static void MoveScopeToOwnerScene(
            LifetimeScope stateScope,
            IReadOnlyList<Scene> loadedScenes)
        {
            if (stateScope == null)
                return;

            if (loadedScenes == null || loadedScenes.Count == 0)
            {
                Debug.LogWarning("Cannot move app state scope because no loaded owner scene was found.");
                return;
            }

            var ownerScene = loadedScenes[0];

            if (!ownerScene.IsValid() || !ownerScene.isLoaded)
            {
                Debug.LogWarning($"Cannot move app state scope to scene '{ownerScene.name}' because it is not valid or not loaded.");
                return;
            }

            var scopeGameObject = stateScope.gameObject;
            scopeGameObject.name = $"AppStateScope ({ownerScene.name})";

            // MoveGameObjectToScene works reliably for root GameObjects.
            scopeGameObject.transform.SetParent(null, true);

            SceneManager.MoveGameObjectToScene(scopeGameObject, ownerScene);
        }
        
    }
}