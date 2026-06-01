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
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text titleText;

        private readonly CompositeDisposable _disposables = new();
        private readonly TimeSpan _buttonThrottle = TimeSpan.FromMilliseconds(500);

        public void Initialize(
            ReactiveCommand<Unit> createRoomCommand,
            ReactiveCommand<Unit> joinRoomCommand,
            ReactiveCommand<Unit> settingsCommand,
            ReactiveCommand<Unit> quitCommand)
        {
            _disposables.Clear();

            if (titleText != null)
                titleText.text = "The Silent Watch";

            BindButton(createRoomButton, createRoomCommand);
            BindButton(joinRoomButton, joinRoomCommand);
            BindButton(settingsButton, settingsCommand);
            BindButton(quitButton, quitCommand);
        }

        private void BindButton(Button button, ReactiveCommand<Unit> command)
        {
            if (button == null)
                return;

            Observable.FromEvent(
                    handler => button.onClick.AddListener(handler.Invoke),
                    handler => button.onClick.RemoveListener(handler.Invoke))
                .ThrottleFirst(_buttonThrottle)
                .Subscribe(_ => command.Execute(Unit.Default))
                .AddTo(_disposables);
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