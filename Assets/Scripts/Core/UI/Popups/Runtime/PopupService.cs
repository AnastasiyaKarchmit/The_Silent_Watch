using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.UI.Popups.Contracts;
using Core.UI.Popups.Runtime.Handlers;
using Core.UI.Popups.Runtime.Handlers.Core;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace Core.UI.Popups.Runtime
{
    public sealed class PopupService : IPopupService, IDisposable
    {
        private readonly IReadOnlyList<IPopupHandler> _handlers;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private bool _isDisposed;

        public IPopupService Parent { get; }

        public PopupService(
            IEnumerable<IPopupHandler> handlers,
            LifetimeScope lifetimeScope)
        {
            _handlers = handlers?.ToArray()
                        ?? throw new ArgumentNullException(nameof(handlers));

            if (lifetimeScope != null &&
                lifetimeScope.Parent != null &&
                lifetimeScope.Parent.Container.TryResolve(out IPopupService parentPopupService))
            {
                Parent = parentPopupService;
            }
        }

        public async UniTask<TResult> ShowAsync<TResult>(
            IPopupRequest<TResult> request,
            CancellationToken token = default)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PopupService));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            IPopupHandler handler = FindLocalHandler(request.GetType());

            if (handler == null)
            {
                if (Parent != null)
                    return await Parent.ShowAsync(request, token);

                throw new InvalidOperationException(
                    $"No popup handler registered for request type '{request.GetType().Name}'.");
            }

            await _semaphore.WaitAsync(token);

            try
            {
                object result = await handler.HandleAsync(request, token);

                return result is TResult typedResult
                    ? typedResult
                    : default;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private IPopupHandler FindLocalHandler(Type requestType)
        {
            IPopupHandler exactHandler = _handlers
                .FirstOrDefault(handler => handler.RequestType == requestType);

            if (exactHandler != null)
                return exactHandler;

            return _handlers.FirstOrDefault(
                handler => handler.RequestType.IsAssignableFrom(requestType));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _semaphore.Dispose();
        }
    }
}