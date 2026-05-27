using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.AppStates.Contracts
{
    public interface IAppTransition
    {
        UniTask ShowAsync(CancellationToken token = default);
        UniTask HideAsync(CancellationToken token = default);
    }
}