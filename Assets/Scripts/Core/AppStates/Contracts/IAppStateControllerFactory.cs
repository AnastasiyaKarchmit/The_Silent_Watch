using Core.AppStates.Contracts.State;
using Core.AppStates.Data;
using VContainer;

namespace Core.AppStates.Contracts
{
    public interface IAppStateControllerFactory
    {
        IAppStateController Create(AppStateId stateId, IObjectResolver resolver);
    }
}