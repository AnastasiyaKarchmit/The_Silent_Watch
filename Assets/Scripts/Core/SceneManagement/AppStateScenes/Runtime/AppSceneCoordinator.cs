using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.AppStates.Data;
using Core.SceneManagement.AppStateScenes.Contracts;
using Core.SceneManagement.Loading.Contracts;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.SceneManagement.AppStateScenes.Runtime
{
    public class AppSceneCoordinator : IAppSceneCoordinator
    {
        private readonly ISceneLoadService _sceneLoader;
        private readonly IAppSceneRegistry _sceneRegistry;

        private readonly HashSet<string> _persistentScenes = new();
        private readonly HashSet<string> _currentStateScenes = new();

        public IReadOnlyList<string> CurrentStateScenes => _currentStateScenes.ToArray();

        public AppSceneCoordinator(
            ISceneLoadService sceneLoader,
            IAppSceneRegistry sceneRegistry)
        {
            _sceneLoader = sceneLoader;
            _sceneRegistry = sceneRegistry;
        }

        public async UniTask InitializePersistentScenesAsync(
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            var scenes = _sceneRegistry.GetPersistentScenes();

            await LoadSceneGroupAsync(scenes, progress, token);

            foreach (var scene in scenes)
                _persistentScenes.Add(scene);
        }

        public async UniTask LoadStateScenesAsync(
            AppStateId stateId,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            var sceneSet = _sceneRegistry.GetSceneSet(stateId);
            var requiredScenes = sceneSet.AllScenes.ToHashSet();

            var scenesToLoad = requiredScenes
                .Where(scene => !_sceneLoader.IsLoaded(scene))
                .ToArray();

            var scenesToUnload = _sceneLoader.GetLoadedSceneNames()
                .Where(scene => !_persistentScenes.Contains(scene))
                .Where(scene => !requiredScenes.Contains(scene))
                .ToArray();

            var loadProgress = CreateProgressSegment(progress, 0f, 0.9f);
            var unloadProgress = CreateProgressSegment(progress, 0.9f, 1f);

            await LoadSceneGroupAsync(scenesToLoad, loadProgress, token);
            await UnloadSceneGroupAsync(scenesToUnload, unloadProgress, token);

            SetActiveScene(sceneSet.MainScene);

            _currentStateScenes.Clear();

            foreach (var scene in requiredScenes)
                _currentStateScenes.Add(scene);

            progress?.Report(1f);
        }

        private async UniTask LoadSceneGroupAsync(
            IReadOnlyList<string> scenes,
            IProgress<float> progress,
            CancellationToken token)
        {
            if (scenes == null || scenes.Count == 0)
            {
                progress?.Report(1f);
                return;
            }

            var values = new float[scenes.Count];

            var tasks = scenes.Select((scene, index) =>
            {
                var sceneProgress = new Progress<float>(value =>
                {
                    values[index] = value;
                    progress?.Report(values.Average());
                });

                return _sceneLoader.LoadAdditiveAsync(scene, sceneProgress, token);
            });

            await UniTask.WhenAll(tasks);

            progress?.Report(1f);
        }

        private async UniTask UnloadSceneGroupAsync(
            IReadOnlyList<string> scenes,
            IProgress<float> progress,
            CancellationToken token)
        {
            if (scenes == null || scenes.Count == 0)
            {
                progress?.Report(1f);
                return;
            }

            var values = new float[scenes.Count];

            var tasks = scenes.Select((scene, index) =>
            {
                var sceneProgress = new Progress<float>(value =>
                {
                    values[index] = value;
                    progress?.Report(values.Average());
                });

                return _sceneLoader.UnloadAsync(scene, sceneProgress, token);
            });

            await UniTask.WhenAll(tasks);

            progress?.Report(1f);
        }

        private static IProgress<float> CreateProgressSegment(
            IProgress<float> target,
            float from,
            float to)
        {
            if (target == null)
                return null;

            return new Progress<float>(value =>
            {
                var progress = Mathf.Lerp(from, to, Mathf.Clamp01(value));
                target.Report(progress);
            });
        }

        private static void SetActiveScene(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError($"Cannot set active scene. Scene '{sceneName}' is not loaded.");
                return;
            }

            SceneManager.SetActiveScene(scene);
        }
    }
}