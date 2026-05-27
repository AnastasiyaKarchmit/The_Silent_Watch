using System;
using System.Threading;
using Core.UI.Extensions;
using Core.UI.Windows.Config;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

namespace Core.UI.Windows.Runtime
{
     public sealed class AddressableWindowFactory : IWindowFactory
    {
        private readonly WindowServiceConfig _config;
        private readonly IObjectResolver _resolver;
        private readonly LifetimeScope _lifetimeScope;

        private RectTransform _uiRoot;

        public AddressableWindowFactory(
            WindowServiceConfig config,
            IObjectResolver resolver,
            LifetimeScope lifetimeScope)
        {
            _config = config;
            _resolver = resolver;
            _lifetimeScope = lifetimeScope;
        }

        public async UniTask<IWindow> CreateAsync(
            WindowId windowId,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            var uiRoot = GetOrCreateUIRoot();
            var reference = _config.GetWindowReference(windowId);

            var handle = Addressables.InstantiateAsync(reference, uiRoot);
            var instance = await handle.ToUniTask(cancellationToken: token);

            _resolver.InjectGameObject(instance);

            var window = instance.GetComponent<IWindow>();

            if (window == null)
            {
                Addressables.ReleaseInstance(instance);
                throw new InvalidOperationException(
                    $"Addressable window '{windowId}' does not have a component implementing {nameof(IWindow)}.");
            }

            window.RootRectTransform.StretchToParent();

            CreateInputBlocker(window.RootRectTransform);

            return window;
        }

        public void Release(IWindow window)
        {
            if (window?.RootRectTransform == null)
                return;

            Addressables.ReleaseInstance(window.RootRectTransform.gameObject);
        }

        private RectTransform GetOrCreateUIRoot()
        {
            if (_uiRoot != null)
                return _uiRoot;

            if (_config.WindowContainerPrefab == null)
                throw new InvalidOperationException("Window container prefab is not assigned.");

            _uiRoot = UnityEngine.Object.Instantiate(
                _config.WindowContainerPrefab,
                _lifetimeScope.transform);

            _uiRoot.StretchToParent();

            return _uiRoot;
        }

        private void CreateInputBlocker(RectTransform windowRoot)
        {
            if (_config.InputBlockerPrefab == null)
                return;

            var blocker = UnityEngine.Object.Instantiate(
                _config.InputBlockerPrefab,
                windowRoot);

            var blockerRect = blocker.transform as RectTransform;

            if (blockerRect != null)
                blockerRect.StretchToParent();

            blocker.transform.SetAsFirstSibling();
        }
    }
}