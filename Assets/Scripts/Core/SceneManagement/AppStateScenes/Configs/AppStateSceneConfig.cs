using System;
using System.Collections.Generic;
using System.Linq;
using Core.AppStates.Data;
using UnityEngine;

namespace Core.SceneManagement.AppStateScenes.Configs
{
    [Serializable]
    public sealed class AppStateSceneConfig
    {
        [SerializeField] private AppStateId stateId;
        [SerializeField] private SceneReference mainScene;
        [SerializeField] private List<SceneReference> additionalScenes = new();

        public AppStateId StateId => stateId;

        public string MainSceneName => mainScene?.SceneName;

        public IReadOnlyList<string> AdditionalSceneNames =>
            additionalScenes
                .Where(scene => scene != null)
                .Select(scene => scene.SceneName)
                .Where(sceneName => !string.IsNullOrWhiteSpace(sceneName))
                .ToArray();

#if UNITY_EDITOR
        public void RefreshSceneNames()
        {
            mainScene?.RefreshSceneName();

            foreach (var scene in additionalScenes)
                scene?.RefreshSceneName();
        }
#endif
    }
}