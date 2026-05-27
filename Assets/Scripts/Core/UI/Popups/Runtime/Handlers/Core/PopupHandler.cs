using System;
using System.Threading;
using Core.UI.Popups.Contracts;
using Cysharp.Threading.Tasks;

namespace Core.UI.Popups.Runtime.Handlers.Core
{
    public interface IPopupHandler
    {
        Type RequestType { get; }

        UniTask<object> HandleAsync(
            IPopupRequest request,
            CancellationToken token);
    }

    public abstract class PopupHandler<TRequest, TResult> : IPopupHandler
        where TRequest : class, IPopupRequest<TResult>
    {
        public Type RequestType => typeof(TRequest);

        public async UniTask<object> HandleAsync(
            IPopupRequest request,
            CancellationToken token)
        {
            return await HandleAsync((TRequest)request, token);
        }

        protected abstract UniTask<TResult> HandleAsync(
            TRequest request,
            CancellationToken token);
    }
}