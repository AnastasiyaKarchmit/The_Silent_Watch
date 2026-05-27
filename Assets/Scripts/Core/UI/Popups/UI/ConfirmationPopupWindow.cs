using System.Threading;
using Core.Audio.Contracts;
using Core.UI.Windows.Components;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.UI.Popups.UI
{
    [DisallowMultipleComponent]
    public sealed class ConfirmationPopupWindow : BaseWindow
    {
        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;

        [Header("Buttons")]
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private Button closeButton;

        [Header("Button Text")]
        [SerializeField] private TMP_Text yesButtonText;
        [SerializeField] private TMP_Text noButtonText;

        private IUISoundPlayer _uiSoundPlayer;
        private UniTaskCompletionSource<bool> _resultSource;
        private CancellationTokenRegistration _cancellationRegistration;
        private bool _isWaitingForResult;

        [Inject]
        public void Construct(IUISoundPlayer soundPlayer)
        {
            _uiSoundPlayer = soundPlayer;
        }
        
        private void Awake()
        {
            SubscribeButtons();
            HideInstantly();
        }

        public async UniTask<bool> ShowAndWaitForResultAsync(
            string title,
            string message,
            string yesText,
            string noText,
            CancellationToken token = default)
        {
            CancelCurrentResult();

            _isWaitingForResult = true;
            _resultSource = new UniTaskCompletionSource<bool>();

            SetContent(title, message, yesText, noText);

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

                bool result = await _resultSource.Task;

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
            string yesText,
            string noText)
        {
            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (yesButtonText != null)
                yesButtonText.text = string.IsNullOrWhiteSpace(yesText) ? "Yes" : yesText;

            if (noButtonText != null)
                noButtonText.text = string.IsNullOrWhiteSpace(noText) ? "No" : noText;
        }

        private void SubscribeButtons()
        {
            if (yesButton != null)
                yesButton.onClick.AddListener(OnYesClicked);

            if (noButton != null)
                noButton.onClick.AddListener(OnNoClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void UnsubscribeButtons()
        {
            if (yesButton != null)
                yesButton.onClick.RemoveListener(OnYesClicked);

            if (noButton != null)
                noButton.onClick.RemoveListener(OnNoClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnYesClicked()
        {
            _uiSoundPlayer.PlayButtonClick();
            Complete(true);
        }

        private void OnNoClicked()
        {
            _uiSoundPlayer.PlayButtonClick();
            Complete(false);
        }

        private void OnCloseClicked()
        {
            _uiSoundPlayer.PlayButtonClick();
            Complete(false);
        }

        private void Complete(bool result)
        {
            if (!_isWaitingForResult)
                return;

            _resultSource?.TrySetResult(result);
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

            base.OnDestroy();
        }
    }
}