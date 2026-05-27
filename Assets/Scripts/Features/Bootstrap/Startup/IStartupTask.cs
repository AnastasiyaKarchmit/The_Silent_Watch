using System.Threading;
using Cysharp.Threading.Tasks;

namespace Features.Bootstrap.Startup
{
    public interface IStartupTask
    {
        string Description { get; }

        UniTask ExecuteAsync(CancellationToken token);
    }
}