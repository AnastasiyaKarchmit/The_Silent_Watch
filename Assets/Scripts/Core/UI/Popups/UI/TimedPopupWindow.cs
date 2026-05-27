using System;
using System.Threading;
using Core.Audio.Contracts;
using Core.Settings;
using Core.UI.Windows.Components;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.UI.Popups.UI
{
    [DisallowMultipleComponent]
    public sealed class TimedPopupWindow : BaseWindow
    {
        [Header("Toast Content")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text amountText;

        [Header("Toast Settings")]
        [SerializeField, Min(0f)] private float defaultDuration = 2f;

        private CanvasGroup _canvasGroup;
        private CancellationTokenSource _temporaryShowCts;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
        
        public void SetContent(
            Sprite icon,
            string title,
            string description,
            string amount)
        {
            if (iconImage != null)
            {
                bool hasIcon = icon != null;

                iconImage.enabled = hasIcon;
                iconImage.sprite = hasIcon ? icon : null;
            }

            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (descriptionText != null)
                descriptionText.text = description ?? string.Empty;

            if (amountText != null)
                amountText.text = amount ?? string.Empty;
        }

        public async UniTask ShowTemporaryAsync(
            Sprite icon,
            string title,
            string description,
            string amount,
            float duration,
            CancellationToken token = default)
        {
            CancelTemporaryShow();

            _temporaryShowCts = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                this.GetCancellationTokenOnDestroy());

            CancellationToken linkedToken = _temporaryShowCts.Token;

            SetContent(icon, title, description, amount);

            try
            {
                await ShowAsync();

                await UniTask.Delay(
                    TimeSpan.FromSeconds(duration > 0f ? duration : defaultDuration),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    linkedToken);

                await HideAsync();
            }
            catch (OperationCanceledException)
            {
                // Another popup started, object was destroyed, or external token was canceled.
            }
        }

        public override async UniTask ShowAsync()
        {
            await base.ShowAsync();
            MakeNonBlocking();
        }

        public override void ShowInstantly()
        {
            base.ShowInstantly();
            MakeNonBlocking();
        }

        public override void HideInstantly()
        {
            CancelTemporaryShow();
            base.HideInstantly();
        }

        public override async UniTask HideAsync()
        {
            CancelTemporaryShow();
            await base.HideAsync();
        }

        private void MakeNonBlocking()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
                return;

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void CancelTemporaryShow()
        {
            if (_temporaryShowCts == null)
                return;

            if (!_temporaryShowCts.IsCancellationRequested)
                _temporaryShowCts.Cancel();

            _temporaryShowCts.Dispose();
            _temporaryShowCts = null;
        }

        protected override void OnDestroy()
        {
            CancelTemporaryShow();
            base.OnDestroy();
        }
    }
}