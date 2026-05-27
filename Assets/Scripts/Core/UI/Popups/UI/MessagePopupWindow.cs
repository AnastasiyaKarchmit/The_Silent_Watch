using System.Threading;
using Core.Audio.Contracts;
using Core.UI.Popups.Contracts;
using Core.UI.Windows.Components;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.UI.Popups.UI
{
    [DisallowMultipleComponent]
    public sealed class MessagePopupWindow : BaseWindow
    {
        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        [Header("Button Text")]
        [SerializeField] private TMP_Text closeButtonText;

        private readonly ReactiveCommand<Unit> _closedCommand = new();

        private IUISoundPlayer _uiSoundPlayer;
        private UniTaskCompletionSource<PopupClosed> _resultSource;
        private CancellationTokenRegistration _cancellationRegistration;
        private bool _isWaitingForResult;

        public Observable<Unit> Closed => _closedCommand;

        [Inject]
        public void Construct(IUISoundPlayer uiSoundPlayer)
        {
            _uiSoundPlayer = uiSoundPlayer;
        }

        private void Awake()
        {
            SubscribeButtons();
            HideInstantly();
        }

        public async UniTask<PopupClosed> ShowAndWaitForCloseAsync(
            string title,
            string message,
            string closeText,
            CancellationToken token = default)
        {
            CancelCurrentResult();

            _isWaitingForResult = true;
            _resultSource = new UniTaskCompletionSource<PopupClosed>();

            SetContent(title, message, closeText);

            if (token.CanBeCanceled)
            {
                _cancellationRegistration = token.Register(() =>
                {
                    _resultSource?.TrySetCanceled(token);
                });
            }

            try
            {
                await ShowAsync();

                PopupClosed result = await _resultSource.Task;

                await HideAsync();

                return result;
            }
            finally
            {
                _isWaitingForResult = false;
                _cancellationRegistration.Dispose();
                _cancellationRegistration = default;
                _resultSource = null;
            }
        }

        public void SetContent(
            string title,
            string message,
            string closeText)
        {
            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (closeButtonText != null)
                closeButtonText.text = string.IsNullOrWhiteSpace(closeText)
                    ? "OK"
                    : closeText;
        }

        private void SubscribeButtons()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void UnsubscribeButtons()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnCloseClicked()
        {
            _uiSoundPlayer?.PlayButtonClick();
            Complete();
        }

        private void Complete()
        {
            if (!_isWaitingForResult)
                return;

            _closedCommand.Execute(Unit.Default);
            _resultSource?.TrySetResult(PopupClosed.Value);
        }

        private void CancelCurrentResult()
        {
            if (_resultSource == null)
                return;

            _resultSource.TrySetCanceled();
            _resultSource = null;
        }

        public override void HideInstantly()
        {
            CancelCurrentResult();
            base.HideInstantly();
        }

        protected override void OnDestroy()
        {
            CancelCurrentResult();

            _cancellationRegistration.Dispose();
            UnsubscribeButtons();

            _closedCommand.Dispose();

            base.OnDestroy();
        }
    }
}