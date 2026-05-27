using System;
using Core.UI.Views;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.MainMenu.States.MainMenuState
{
    public sealed class MainMenuView : BaseView
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text titleText;

        private readonly CompositeDisposable _disposables = new();
        private readonly TimeSpan _buttonThrottle = TimeSpan.FromMilliseconds(500);

        public void Initialize(
            ReactiveCommand<Unit> playCommand,
            ReactiveCommand<Unit> settingsCommand,
            ReactiveCommand<Unit> quitCommand)
        {
            _disposables.Clear();

            if (titleText != null)
                titleText.text = "Main Menu";

            if (playButton != null)
            {
                Observable.FromEvent(
                        handler => playButton.onClick.AddListener(handler.Invoke),
                        handler => playButton.onClick.RemoveListener(handler.Invoke))
                    .ThrottleFirst(_buttonThrottle)
                    .Subscribe(_ => playCommand.Execute(Unit.Default))
                    .AddTo(_disposables);
            }

            if (settingsButton != null)
            {
                Observable.FromEvent(
                        handler => settingsButton.onClick.AddListener(handler.Invoke),
                        handler => settingsButton.onClick.RemoveListener(handler.Invoke))
                    .ThrottleFirst(_buttonThrottle)
                    .Subscribe(_ => settingsCommand.Execute(Unit.Default))
                    .AddTo(_disposables);
            }
            
            if (quitButton != null)
            {
                Observable.FromEvent(
                        handler => quitButton.onClick.AddListener(handler.Invoke),
                        handler => quitButton.onClick.RemoveListener(handler.Invoke))
                    .ThrottleFirst(_buttonThrottle)
                    .Subscribe(_ => quitCommand.Execute(Unit.Default))
                    .AddTo(_disposables);
            }
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
    }
}