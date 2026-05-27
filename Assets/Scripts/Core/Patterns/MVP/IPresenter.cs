using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Patterns.MVP
{
    public interface IPresenter : IDisposable
    {
        UniTask EnterAsync(CancellationToken token = default);
        UniTask ExitAsync(CancellationToken token = default);
    }
}