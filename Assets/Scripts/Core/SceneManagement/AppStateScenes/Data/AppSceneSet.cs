using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.SceneManagement.AppStateScenes.Data
{
    public readonly struct AppSceneSet
    {
        public string MainScene { get; }
        public IReadOnlyList<string> AdditionalScenes { get; }

        public AppSceneSet(string mainScene, IEnumerable<string> additionalScenes = null)
        {
            MainScene = mainScene;
            AdditionalScenes = additionalScenes?.ToArray() ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> AllScenes =>
            new[] { MainScene }
                .Concat(AdditionalScenes)
                .Where(scene => !string.IsNullOrWhiteSpace(scene))
                .Distinct()
                .ToArray();
    }
}