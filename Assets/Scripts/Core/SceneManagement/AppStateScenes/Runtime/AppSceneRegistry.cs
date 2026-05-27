using System.Collections.Generic;
using Core.AppStates.Data;
using Core.SceneManagement.AppStateScenes.Configs;
using Core.SceneManagement.AppStateScenes.Contracts;
using Core.SceneManagement.AppStateScenes.Data;

namespace Core.SceneManagement.AppStateScenes.Runtime
{
    public enum SharedSceneId
    {
        PopupLayer,
        LoadingScreen,
        DynamicBackground
    }

    public sealed class AppSceneRegistry : IAppSceneRegistry
    {
        private readonly AppSceneDatabase _database;

        public AppSceneRegistry(AppSceneDatabase database)
        {
            _database = database;
        }

        public IReadOnlyList<string> GetPersistentScenes()
        {
            return _database.PersistentScenes;
        }

        public AppSceneSet GetSceneSet(AppStateId stateId)
        {
            return _database.GetSceneSet(stateId);
        }
    }
}