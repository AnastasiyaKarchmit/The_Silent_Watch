using System.Collections.Generic;
using Core.AppStates.Data;
using Core.SceneManagement.AppStateScenes.Data;

namespace Core.SceneManagement.AppStateScenes.Contracts
{
    public interface IAppSceneRegistry
    {
        AppSceneSet GetSceneSet(AppStateId stateId);
        IReadOnlyList<string> GetPersistentScenes();
    }
}