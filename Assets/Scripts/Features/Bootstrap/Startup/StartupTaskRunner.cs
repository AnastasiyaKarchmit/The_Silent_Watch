using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Features.Bootstrap.Startup
{
    public sealed class StartupTaskRunner
    {
        private readonly IReadOnlyList<IStartupTask> _tasks;

        public StartupTaskRunner(IReadOnlyList<IStartupTask> tasks)
        {
            _tasks = tasks;
        }

        public async UniTask RunAsync(
            IProgress<float> progress,
            Action<string> statusChanged,
            CancellationToken token)
        {
            if (_tasks.Count == 0)
            {
                progress?.Report(1f);
                return;
            }

            for (var i = 0; i < _tasks.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                var task = _tasks[i];

                statusChanged?.Invoke(task.Description);

                await task.ExecuteAsync(token);

                progress?.Report((i + 1f) / _tasks.Count);
            }
        }
    }
}