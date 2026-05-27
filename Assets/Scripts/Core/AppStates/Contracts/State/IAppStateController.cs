using System;
using System.Threading;
using Core.AppStates.Data;
using Cysharp.Threading.Tasks;

namespace Core.AppStates.Contracts.State
{
    public interface IAppStateController : IDisposable
    {
        UniTask EnterAsync(object payload, CancellationToken token);
        UniTask<AppStateExitResult> RunAsync(CancellationToken token);
        UniTask ExitAsync(CancellationToken token);
    }
}