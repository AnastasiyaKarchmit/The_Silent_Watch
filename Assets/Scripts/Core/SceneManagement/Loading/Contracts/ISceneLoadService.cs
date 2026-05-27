using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.SceneManagement.Loading.Contracts
{
    public interface ISceneLoadService
    {
        UniTask LoadAdditiveAsync(
            string sceneName,
            IProgress<float> progress = null,
            CancellationToken token = default);

        UniTask LoadSingleAsync(
            string sceneName,
            IProgress<float> progress = null,
            CancellationToken token = default);

        UniTask UnloadAsync(
            string sceneName,
            IProgress<float> progress = null,
            CancellationToken token = default);

        bool IsLoaded(string sceneName);
        IReadOnlyList<string> GetLoadedSceneNames();
    }
}