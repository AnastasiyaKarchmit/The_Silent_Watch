using System;
using UnityEditor;
using UnityEngine;

namespace Core.SceneManagement.AppStateScenes.Configs
{
    [Serializable]
    public sealed class SceneReference
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif

        [SerializeField] private string sceneName;

        public string SceneName => sceneName;

#if UNITY_EDITOR
        public SceneAsset SceneAsset => sceneAsset;

        public void RefreshSceneName()
        {
            sceneName = sceneAsset != null ? sceneAsset.name : string.Empty;
        }
#endif
    }
}