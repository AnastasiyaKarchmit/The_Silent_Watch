using System;
using System.Collections.Generic;
using Core.Application;
using Core.Save.SaveStorage;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Core.Save
{
    public sealed class SaveSystem : ISaveSystem, IStartable, IDisposable
    {
        private const string SaveFileName = "save.json";

        private readonly ISaveStorage _storage;
        private readonly IAppLifecycleService _appLifecycleService;

        private readonly List<ISaveDataProvider> _providers = new();
        private readonly HashSet<ISaveDataProvider> _loadedProviders = new();

        private PersistentData _data;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _isSaving;

        public PersistentData Data => _data;
        public bool IsLoaded => _isLoaded;

        public SaveSystem(
            ISaveStorage storage,
            IAppLifecycleService appLifecycleService)
        {
            _storage = storage;
            _appLifecycleService = appLifecycleService;
        }

        public void Start()
        {
            _appLifecycleService.ApplicationFocusChanged += OnApplicationFocusChanged;
            _appLifecycleService.ApplicationPauseChanged += OnApplicationPauseChanged;
            _appLifecycleService.ApplicationQuitRequested += OnApplicationQuitRequested;

            LoadAsync().Forget();
        }

        public async UniTask LoadAsync()
        {
            if (_isLoaded || _isLoading)
                return;

            _isLoading = true;

            try
            {
                _data = await _storage.LoadAsync(SaveFileName, new PersistentData());
                
                // Data is now available, so any provider registering from this point
                // can immediately load from it.
                _isLoaded = true;

                ISaveDataProvider[] providersSnapshot = _providers.ToArray();

                if (providersSnapshot is { Length: > 0 })
                    foreach (ISaveDataProvider provider in providersSnapshot)
                        await LoadProviderAsync(provider);
            }
            finally
            {
                _isLoading = false;
            }
        }

        public void Register(ISaveDataProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            if (_providers.Contains(provider))
                return;

            _providers.Add(provider);

            if (_isLoaded)
                LoadProviderAsync(provider).Forget();
        }

        public void Unregister(ISaveDataProvider provider)
        {
            if (provider == null)
                return;

            _providers.Remove(provider);
            _loadedProviders.Remove(provider);
        }

        public async UniTask SaveAsync()
        {
            if (!_isLoaded)
                return;

            if (_isSaving)
                return;

            _isSaving = true;

            try
            {
                foreach (ISaveDataProvider provider in _providers)
                    provider.Save(_data);

                await _storage.SaveAsync(SaveFileName, _data);
            }
            finally
            {
                _isSaving = false;
            }
        }

        public async UniTask ResetAsync()
        {
            _data = new PersistentData();
            _loadedProviders.Clear();

            foreach (ISaveDataProvider provider in _providers)
                await LoadProviderAsync(provider);

            await SaveAsync();
        }

        private async UniTask LoadProviderAsync(ISaveDataProvider provider)
        {
            if (!_loadedProviders.Add(provider))
                return;

            await provider.LoadAsync(_data);
        }

        private void OnApplicationFocusChanged(bool isFocused)
        {
            if (!isFocused)
                SaveAsync().Forget();
        }

        private void OnApplicationPauseChanged(bool isPaused)
        {
            if (isPaused)
                SaveAsync().Forget();
        }

        private void OnApplicationQuitRequested()
        {
            SaveAsync().Forget();
        }

        public void Dispose()
        {
            _appLifecycleService.ApplicationFocusChanged -= OnApplicationFocusChanged;
            _appLifecycleService.ApplicationPauseChanged -= OnApplicationPauseChanged;
            _appLifecycleService.ApplicationQuitRequested -= OnApplicationQuitRequested;
        }
    }
}