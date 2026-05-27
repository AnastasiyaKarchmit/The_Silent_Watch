using System;
using System.Collections.Generic;
using System.Linq;
using Core.AppStates;
using Core.AppStates.Data;
using Core.SceneManagement.AppStateScenes.Data;
using UnityEngine;

namespace Core.SceneManagement.AppStateScenes.Configs
{
    [CreateAssetMenu(
        fileName = "AppSceneDatabase",
        menuName = "Game Template/App Scene Database")]
    public sealed class AppSceneDatabase : ScriptableObject
    {
        [Header("Scenes that stay loaded during the whole application")]
        [SerializeField] private List<SceneReference> persistentScenes = new();

        [Header("Scenes used by each app state")]
        [SerializeField] private List<AppStateSceneConfig> stateSceneConfigs = new();

        public IReadOnlyList<string> PersistentScenes =>
            persistentScenes
                .Where(scene => scene != null)
                .Select(scene => scene.SceneName)
                .Where(sceneName => !string.IsNullOrWhiteSpace(sceneName))
                .Distinct()
                .ToArray();

        public AppSceneSet GetSceneSet(AppStateId stateId)
        {
            var config = stateSceneConfigs.FirstOrDefault(scene => scene.StateId == stateId);

            if (config == null)
                throw new InvalidOperationException(
                    $"Scene config for app state '{stateId}' was not found in {nameof(AppSceneDatabase)}.");

            if (string.IsNullOrWhiteSpace(config.MainSceneName))
                throw new InvalidOperationException(
                    $"Main scene for app state '{stateId}' is empty in {nameof(AppSceneDatabase)}.");

            return new AppSceneSet(config.MainSceneName, config.AdditionalSceneNames);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RefreshSceneNames();
            ValidateDuplicateStates();
            ValidateDuplicateSceneNamesInsideConfigs();
        }

        private void RefreshSceneNames()
        {
            foreach (var scene in persistentScenes)
                scene?.RefreshSceneName();

            foreach (var config in stateSceneConfigs)
                config?.RefreshSceneNames();
        }

        private void ValidateDuplicateStates()
        {
            var duplicates = stateSceneConfigs
                .Where(config => config != null)
                .GroupBy(config => config.StateId)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            foreach (var duplicate in duplicates)
            {
                Debug.LogWarning(
                    $"Duplicate scene config found for app state '{duplicate}' in {name}.",
                    this);
            }
        }

        private void ValidateDuplicateSceneNamesInsideConfigs()
        {
            foreach (var config in stateSceneConfigs)
            {
                if (config == null)
                    continue;

                var allScenes = new[] { config.MainSceneName }
                    .Concat(config.AdditionalSceneNames)
                    .Where(scene => !string.IsNullOrWhiteSpace(scene))
                    .ToArray();

                var duplicates = allScenes
                    .GroupBy(scene => scene)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key);

                foreach (var duplicate in duplicates)
                {
                    Debug.LogWarning(
                        $"Scene '{duplicate}' is assigned more than once for app state '{config.StateId}' in {name}.",
                        this);
                }
            }
        }
#endif
    }
}