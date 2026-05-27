using System;
using System.Collections.Generic;
using System.Threading;
using Core.SceneManagement.Loading.Contracts;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.SceneManagement.Loading.Runtime
{
    public class SceneLoadService : ISceneLoadService
    {
        public async UniTask LoadAdditiveAsync(
            string sceneName,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            if (IsLoaded(sceneName))
            {
                progress?.Report(1f);
                return;
            }

            await LoadAsync(sceneName, LoadSceneMode.Additive, progress, token);
        }

        public async UniTask LoadSingleAsync(
            string sceneName,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            await LoadAsync(sceneName, LoadSceneMode.Single, progress, token);
        }

        public async UniTask UnloadAsync(
            string sceneName,
            IProgress<float> progress = null,
            CancellationToken token = default)
        {
            var scene = SceneManager.GetSceneByName(sceneName);

            if (!scene.IsValid() || !scene.isLoaded)
            {
                progress?.Report(1f);
                return;
            }

            var operation = SceneManager.UnloadSceneAsync(scene);

            if (operation == null)
            {
                progress?.Report(1f);
                return;
            }

            await TrackOperationAsync(operation, progress, token);
        }

        public bool IsLoaded(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        public IReadOnlyList<string> GetLoadedSceneNames()
        {
            var result = new List<string>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (scene.IsValid() && scene.isLoaded)
                    result.Add(scene.name);
            }

            return result;
        }

        private static async UniTask LoadAsync(
            string sceneName,
            LoadSceneMode mode,
            IProgress<float> progress,
            CancellationToken token)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);

            if (operation == null)
                throw new InvalidOperationException($"Cannot load scene '{sceneName}'. Check Build Settings.");

            await TrackOperationAsync(operation, progress, token);
        }

        private static async UniTask TrackOperationAsync(
            AsyncOperation operation,
            IProgress<float> progress,
            CancellationToken token)
        {
            while (!operation.isDone)
            {
                token.ThrowIfCancellationRequested();

                progress?.Report(Mathf.Clamp01(operation.progress / 0.9f));

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            progress?.Report(1f);
        }
    }
}