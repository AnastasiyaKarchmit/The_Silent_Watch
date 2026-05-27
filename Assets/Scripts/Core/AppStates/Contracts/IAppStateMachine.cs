using System.Threading;
using Core.AppStates.Data;
using Cysharp.Threading.Tasks;

namespace Core.AppStates.Contracts
{
    public interface IAppStateMachine
    {
        AppStateId? CurrentState { get; }

        UniTask SwitchToAsync(
            AppStateId stateId,
            object payload = null,
            CancellationToken token = default);
    }
}