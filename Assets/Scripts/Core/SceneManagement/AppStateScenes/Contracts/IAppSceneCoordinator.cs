using System;
using System.Collections.Generic;
using System.Threading;
using Core.AppStates.Data;
using Cysharp.Threading.Tasks;

namespace Core.SceneManagement.AppStateScenes.Contracts
{
    public interface IAppSceneCoordinator
    {
        IReadOnlyList<string> CurrentStateScenes { get; }

        UniTask InitializePersistentScenesAsync(
            IProgress<float> progress = null,
            CancellationToken token = default);

        UniTask LoadStateScenesAsync(
            AppStateId stateId,
            IProgress<float> progress = null,
            CancellationToken token = default);
    }
}