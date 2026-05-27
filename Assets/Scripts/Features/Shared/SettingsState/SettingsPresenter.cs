using System;
using System.Threading;
using Core.Input.Contracts;
using Core.Input.Runtime;
using Core.Patterns.MVP;
using Core.Save;
using Core.Settings;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Features.Shared.SettingsState
{
    public sealed class SettingsPresenter : IPresenter
    {
        private readonly ISettingsService _settingsService;
        private readonly ISaveSystem _saveSystem;
        private readonly IWindowService _windowService;
        private readonly IInputService _inputService;

        private readonly CompositeDisposable _disposables = new();
        private readonly CompositeDisposable _screenDisposables = new();

        private readonly ReactiveCommand<float> _masterVolumeChanged  = new();
        private readonly ReactiveCommand<float> _musicVolumeChanged = new();
        private readonly ReactiveCommand<float> _sfxVolumeChanged = new();
        private readonly ReactiveCommand<Unit> _resetClicked = new();
        private readonly ReactiveCommand<Unit> _backClicked = new();
        
        private readonly TimeSpan _inputThrottle = TimeSpan.FromMilliseconds(300);

        private SettingsView _view;

        public Observable<Unit> BackRequested => _backClicked;

        public SettingsPresenter(
            ISettingsService settingsService,
            ISaveSystem saveSystem,
            IWindowService windowService,
            IInputService inputService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _saveSystem = saveSystem ?? throw new ArgumentNullException(nameof(saveSystem));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));

            SubscribeToCommands();
        }

        public async UniTask EnterAsync(CancellationToken token = default)
        {
            _inputService.SetMode(InputMode.UIOnly);

            _view = await _windowService.GetOrCreateAsync<SettingsView>(
                WindowId.Settings,
                token);

            token.ThrowIfCancellationRequested();

            SettingsViewCommands commands = new(
                _masterVolumeChanged,
                _musicVolumeChanged,
                _sfxVolumeChanged,
                _resetClicked,
                _backClicked);

            _view.Initialize(_settingsService.GetValues(), commands);
            
            await _view.ShowAsync();
            
            SubscribeToInput();
        }

        public async UniTask ExitAsync(CancellationToken token = default)
        {
            _screenDisposables.Clear();
            
            if (_view != null)
                await _view.HideAsync();

            _view = null;
            
            await _saveSystem.SaveAsync();
        }

        public void HideInstantly()
        {
            _screenDisposables.Clear();
            _view?.HideInstantly();
        }

        public void Dispose()
        {
            Debug.Log("SettingsPresenter::Dispose");
            _screenDisposables.Dispose();
            _disposables.Dispose();

            _masterVolumeChanged.Dispose();
            _musicVolumeChanged.Dispose();
            _sfxVolumeChanged.Dispose();
            _resetClicked.Dispose();
            _backClicked.Dispose();
        }

        private void SubscribeToCommands()
        {
            _masterVolumeChanged
                .Subscribe(value => _settingsService.SetMasterVolume(value))
                .AddTo(_disposables);
            
            _musicVolumeChanged
                .Subscribe(value => _settingsService.SetMusicVolume(value))
                .AddTo(_disposables);

            _sfxVolumeChanged
                .Subscribe(value => _settingsService.SetSfxVolume(value))
                .AddTo(_disposables);

            _resetClicked
                .Subscribe(_ =>
                {
                    _settingsService.ResetToDefaults();
                    _view?.SetValues(_settingsService.GetValues());
                })
                .AddTo(_disposables);
        }
        
        private void SubscribeToInput()
        {
            _screenDisposables.Clear();

            _inputService.UI.Cancel.Performed
                .Where(pressed => pressed)
                .ThrottleFirst(_inputThrottle)
                .Subscribe(_ =>
                {
                    Debug.Log("Settings Presenter registered cancel clicked");
                    _backClicked.Execute(Unit.Default);
                })
                .AddTo(_screenDisposables);
        }
    }
}