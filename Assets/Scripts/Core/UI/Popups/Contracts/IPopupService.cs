using System;
using System.Threading;
using Core.UI.Popups.Data;
using Cysharp.Threading.Tasks;

namespace Core.UI.Popups.Contracts
{
    public interface IPopupService
    {
        UniTask<TResult> ShowAsync<TResult>(
            IPopupRequest<TResult> request,
            CancellationToken token = default);
    }
}