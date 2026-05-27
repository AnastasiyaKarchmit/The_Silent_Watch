using System;
using Core.UI.Views;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Gameplay.States.PauseState
{
    public class PauseView : BaseView
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button backToMenuButton;

        private readonly CompositeDisposable _disposables = new();
        private readonly TimeSpan _buttonThrottle = TimeSpan.FromMilliseconds(500);

        public void Initialize(
            ReactiveCommand<Unit> resumeCommand,
            ReactiveCommand<Unit> settingsCommand,
            ReactiveCommand<Unit> backToMenuCommand)
        {
            _disposables.Clear();

            if (resumeButton != null)
            {
                Observable.FromEvent(
                        handler => resumeButton.onClick.AddListener(handler.Invoke),
                        handler => resumeButton.onClick.RemoveListener(handler.Invoke))
                    .ThrottleFirst(_buttonThrottle)
                    .Subscribe(_ => resumeCommand.Execute(Unit.Default))
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

            if (backToMenuButton != null)
            {
                Observable.FromEvent(
                        handler => backToMenuButton.onClick.AddListener(handler.Invoke),
                        handler => backToMenuButton.onClick.RemoveListener(handler.Invoke))
                    .ThrottleFirst(_buttonThrottle)
                    .Subscribe(_ => backToMenuCommand.Execute(Unit.Default))
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
