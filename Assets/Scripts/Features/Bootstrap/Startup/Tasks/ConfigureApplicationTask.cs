using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Bootstrap.Startup.Tasks
{
    public sealed class ConfigureApplicationTask : IStartupTask
    {
        private const int TargetFrameRate = 60;

        public string Description => "Configuring application";

        public UniTask ExecuteAsync(CancellationToken token)
        {
            Application.targetFrameRate = TargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            return UniTask.CompletedTask;
        }
    }
}