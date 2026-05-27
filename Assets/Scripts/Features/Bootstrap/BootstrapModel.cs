using System;
using System.Threading;
using Core.Patterns.MVP;
using Cysharp.Threading.Tasks;
using Features.Bootstrap.Startup;

namespace Features.Bootstrap
{
    public sealed class BootstrapModel : IModel
    {
        private readonly StartupTaskRunner _startupTaskRunner;

        public BootstrapModel(StartupTaskRunner startupTaskRunner)
        {
            _startupTaskRunner = startupTaskRunner;
        }

        public UniTask RunStartupTasksAsync(
            IProgress<float> progress,
            Action<string> statusChanged,
            CancellationToken token)
        {
            return _startupTaskRunner.RunAsync(progress, statusChanged, token);
        }

        public void Dispose()
        {
            // No unmanaged/runtime resources yet.
        }
    }
}