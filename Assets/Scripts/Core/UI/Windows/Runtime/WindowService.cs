using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;
using Infrastructure.DI;
using VContainer;
using VContainer.Unity;

namespace Core.UI.Windows.Runtime
{
    public sealed class WindowService : IWindowService, IInitializable, IDisposable
    {
        private readonly IWindowFactory _windowFactory;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly LinkedList<WindowInfo> _windows = new();

        private bool _isDisposed;
        private bool _isDestroyingAll;

        public event Action OnBeforeFirstWindowCreated;
        public event Action OnBecameEmpty;

        public IWindowService Parent { get; }
        public bool IsLoadingAnyWindow { get; private set; }

        public WindowService(
            IWindowFactory windowFactory, LifetimeScope lifetimeScope)
        {
            _windowFactory = windowFactory ?? throw new ArgumentNullException(nameof(windowFactory));
            
            if (lifetimeScope != null &&
                lifetimeScope.Parent != null &&
                lifetimeScope.Parent.Container.TryResolve(out IWindowService parentWindowService))
            {
                Parent = parentWindowService;
            }
        }

        public void Initialize()
        {
            if (Parent == null)
                return;

            Parent.OnBeforeFirstWindowCreated += OnParentBeforeFirstWindowCreated;
            Parent.OnBecameEmpty += OnParentBecameEmpty;
        }

        public async UniTask<IWindow> CreateAsync(
            WindowId windowId,
            CancellationToken token = default)
        {
            ThrowIfDisposed();

            await _semaphore.WaitAsync(token);

            try
            {
                IsLoadingAnyWindow = true;

                if (_windows.Count == 0)
                    OnBeforeFirstWindowCreated?.Invoke();

                DeactivateCurrentTopWindow();

                var window = await _windowFactory.CreateAsync(windowId, token);
                window.Destroyed += OnWindowDestroyed;

                _windows.AddLast(new WindowInfo(windowId, window));

                if (!HasParentWindowsOrLoading())
                    ActivateWindow(window);

                return window;
            }
            finally
            {
                IsLoadingAnyWindow = false;
                _semaphore.Release();
            }
        }

        public UniTask<IWindow> GetOrCreateAsync(
            WindowId windowId,
            CancellationToken token = default)
        {
            ThrowIfDisposed();

            return TryFind(windowId, out var window)
                ? UniTask.FromResult(window)
                : CreateAsync(windowId, token);
        }
        
        public async UniTask<TWindow> CreateAsync<TWindow>(
            WindowId windowId,
            CancellationToken token = default)
            where TWindow : class, IWindow
        {
            var window = await CreateAsync(windowId, token);

            if (window is TWindow typedWindow)
                return typedWindow;

            throw new InvalidOperationException(
                $"Window '{windowId}' was created, but it is not of type '{typeof(TWindow).Name}'. " +
                $"Actual type: '{window.GetType().Name}'.");
        }

        public async UniTask<TWindow> GetOrCreateAsync<TWindow>(
            WindowId windowId,
            CancellationToken token = default)
            where TWindow : class, IWindow
        {
            var window = await GetOrCreateAsync(windowId, token);

            if (window is TWindow typedWindow)
                return typedWindow;

            throw new InvalidOperationException(
                $"Window '{windowId}' exists, but it is not of type '{typeof(TWindow).Name}'. " +
                $"Actual type: '{window.GetType().Name}'.");
        }

        public bool TryFind(WindowId windowId, out IWindow window)
        {
            for (var node = _windows.Last; node != null; node = node.Previous)
            {
                if (node.Value.Id != windowId)
                    continue;

                window = node.Value.Window;
                return true;
            }

            window = null;
            return false;
        }

        public IWindow GetTopWindow()
        {
            return _windows.Count == 0
                ? null
                : _windows.Last.Value.Window;
        }

        public void DestroyAll()
        {
            if (_windows.Count == 0)
                return;

            _isDestroyingAll = true;

            var windowsToDestroy = _windows
                .Select(info => info.Window)
                .Where(window => window != null)
                .ToArray();

            foreach (var window in windowsToDestroy)
                window.Destroyed -= OnWindowDestroyed;

            _windows.Clear();

            foreach (var window in windowsToDestroy)
                _windowFactory.Release(window);

            _isDestroyingAll = false;

            OnBecameEmpty?.Invoke();
        }
        
        public bool Destroy(WindowId windowId)
        {
            ThrowIfDisposed();

            for (var node = _windows.Last; node != null; node = node.Previous)
            {
                if (node.Value.Id != windowId)
                    continue;

                DestroyNode(node);
                return true;
            }

            return false;
        }

        private void DestroyNode(LinkedListNode<WindowInfo> node)
        {
            var window = node.Value.Window;

            if (window == null)
            {
                _windows.Remove(node);

                if (_windows.Count == 0)
                    OnBecameEmpty?.Invoke();

                return;
            }

            var wasTopWindow = node == _windows.Last;
            var previousNode = node.Previous;

            window.Destroyed -= OnWindowDestroyed;

            _windows.Remove(node);

            if (wasTopWindow)
            {
                DeactivateWindow(window);

                if (!HasParentWindowsOrLoading() && previousNode?.Value.Window != null)
                    ActivateWindow(previousNode.Value.Window);
            }

            _windowFactory.Release(window);

            if (_windows.Count == 0)
                OnBecameEmpty?.Invoke();
        }

        private void OnWindowDestroyed(IWindow window)
        {
            if (_isDestroyingAll)
                return;

            var lastNode = _windows.Last;

            for (var node = _windows.Last; node != null; node = node.Previous)
            {
                if (node.Value.Window != window)
                    continue;

                window.Destroyed -= OnWindowDestroyed;

                var wasTopWindow = node == lastNode;
                var previousNode = node.Previous;

                _windows.Remove(node);

                if (wasTopWindow && !HasParentWindowsOrLoading())
                {
                    DeactivateWindow(window);

                    if (previousNode?.Value.Window != null)
                        ActivateWindow(previousNode.Value.Window);
                }

                break;
            }

            if (_windows.Count == 0)
                OnBecameEmpty?.Invoke();
        }

        private void DeactivateCurrentTopWindow()
        {
            var topWindow = GetTopWindow();

            if (topWindow == null || !topWindow.IsActive)
                return;

            DeactivateWindow(topWindow);
        }

        private static void ActivateWindow(IWindow window)
        {
            if (window is IWindowEventsHandler activeHandler)
                activeHandler.OnActivated();
        }

        private static void DeactivateWindow(IWindow window)
        {
            if (window is IWindowEventsHandler inactiveHandler)
                inactiveHandler.OnDeactivated();
        }

        private void OnParentBeforeFirstWindowCreated()
        {
            var topWindow = GetTopWindow();

            if (topWindow != null)
                DeactivateWindow(topWindow);
        }

        private void OnParentBecameEmpty()
        {
            var topWindow = GetTopWindow();

            if (topWindow != null)
                ActivateWindow(topWindow);
        }

        private bool HasParentWindowsOrLoading()
        {
            return Parent != null &&
                   (Parent.GetTopWindow() != null || Parent.IsLoadingAnyWindow);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(WindowService));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            DestroyAll();

            if (Parent != null)
            {
                Parent.OnBeforeFirstWindowCreated -= OnParentBeforeFirstWindowCreated;
                Parent.OnBecameEmpty -= OnParentBecameEmpty;
            }

            _semaphore.Dispose();
        }

        private readonly struct WindowInfo
        {
            public WindowId Id { get; }
            public IWindow Window { get; }

            public WindowInfo(WindowId id, IWindow window)
            {
                Id = id;
                Window = window;
            }
        }
    }
}