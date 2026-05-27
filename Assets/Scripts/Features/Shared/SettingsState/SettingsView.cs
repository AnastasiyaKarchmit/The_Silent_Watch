using System;
using Core.Settings;
using Core.UI.Views;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Shared.SettingsState
{
    public sealed class SettingsView : BaseView
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;

        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text sfxVolumeText;
        [SerializeField] private TMP_Text titleText;

        private readonly CompositeDisposable _disposables = new();
        private readonly TimeSpan _buttonThrottle = TimeSpan.FromMilliseconds(500);

        public void Initialize(SettingsValues values, SettingsViewCommands commands)
        {
            _disposables.Clear();

            if (titleText != null)
                titleText.text = "Settings";

            SetValues(values);
            SetupListeners(commands);
        }

        public void SetValues(SettingsValues values)
        {
            if (musicVolumeSlider != null)
                musicVolumeSlider.SetValueWithoutNotify(values.MusicVolume);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(values.SfxVolume);
            
            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(values.MasterVolume);

            UpdateVolumeTexts(values);
        }

        public override UniTask HideAsync()
        {
            _disposables.Clear();
            return base.HideAsync();
        }

        protected override void OnDestroy()
        {
            _disposables.Dispose();
            base.OnDestroy();
        }

        private void SetupListeners(SettingsViewCommands commands)
        {
            if (masterVolumeSlider != null)
            {
                Observable.FromEvent<float>(
                        handler => masterVolumeSlider.onValueChanged.AddListener(handler.Invoke),
                        handler => masterVolumeSlider.onValueChanged.RemoveListener(handler.Invoke))
                    .Subscribe(value =>
                    {
                        UpdateMasterVolumeText(value);
                        commands.MasterVolumeCommand.Execute(value);
                    })
                    .AddTo(_disposables);
            }
            
            if (musicVolumeSlider != null)
            {
                Observable.FromEvent<float>(
                        handler => musicVolumeSlider.onValueChanged.AddListener(handler.Invoke),
                        handler => musicVolumeSlider.onValueChanged.RemoveListener(handler.Invoke))
                    .Subscribe(value =>
                    {
                        UpdateMusicVolumeText(value);
                        commands.MusicVolumeChanged.Execute(value);
                    })
                    .AddTo(_disposables);
            }

            if (sfxVolumeSlider != null)
            {
                Observable.FromEvent<float>(
                        handler => sfxVolumeSlider.onValueChanged.AddListener(handler.Invoke),
                        handler => sfxVolumeSlider.onValueChanged.RemoveListener(handler.Invoke))
                    .Subscribe(value =>
                    {
                        UpdateSfxVolumeText(value);
                        commands.SfxVolumeChanged.Execute(value);
                    })
                    .AddTo(_disposables);
            }

            if (resetButton != null)
            {
                Observable.FromEvent(
                        handler => resetButton.onClick.AddListener(handler.Invoke),
                        handler => resetButton.onClick.RemoveListener(handler.Invoke))
                    .ThrottleFirst(_buttonThrottle)
                    .Subscribe(_ => commands.ResetClicked.Execute(Unit.Default))
                    .AddTo(_disposables);
            }

            if (backButton != null)
            {
                Observable.FromEvent(
                        handler => backButton.onClick.AddListener(handler.Invoke),
                        handler => backButton.onClick.RemoveListener(handler.Invoke))
                    .ThrottleFirst(_buttonThrottle)
                    .Subscribe(_ => commands.BackClicked.Execute(Unit.Default))
                    .AddTo(_disposables);
            }
        }

        private void UpdateVolumeTexts(SettingsValues values)
        {
            UpdateMasterVolumeText(values.MasterVolume);
            UpdateMusicVolumeText(values.MusicVolume);
            UpdateSfxVolumeText(values.SfxVolume);
        }

        private void UpdateMasterVolumeText(float value)
        {
            if (masterVolumeText != null)
                masterVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%"; 
        }
        
        private void UpdateMusicVolumeText(float value)
        {
            if (musicVolumeText != null)
                musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void UpdateSfxVolumeText(float value)
        {
            if (sfxVolumeText != null)
                sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }
}