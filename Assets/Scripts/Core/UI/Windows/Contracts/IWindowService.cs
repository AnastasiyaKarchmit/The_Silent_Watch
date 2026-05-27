using System;
using System.Threading;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;

namespace Core.UI.Windows.Contracts
{
    public interface IWindowService
    {
        event Action OnBeforeFirstWindowCreated;
        event Action OnBecameEmpty;

        bool IsLoadingAnyWindow { get; }
        
        public IWindowService Parent { get; }

        UniTask<IWindow> CreateAsync(WindowId windowId, CancellationToken token = default);
        UniTask<TWindow> CreateAsync<TWindow>(WindowId windowId, CancellationToken token = default)
            where TWindow : class, IWindow;
        
        UniTask<IWindow> GetOrCreateAsync(WindowId windowId, CancellationToken token = default);
        UniTask<TWindow> GetOrCreateAsync<TWindow>(WindowId windowId, CancellationToken token = default)
            where TWindow : class, IWindow;

        bool TryFind(WindowId windowId, out IWindow window);
        IWindow GetTopWindow();
        bool Destroy(WindowId windowId);
        void DestroyAll();
    }
}