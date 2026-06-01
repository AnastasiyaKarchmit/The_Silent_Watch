using System.Threading;
using Core.UI.Popups.Contracts;
using Core.UI.Windows.Components;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Popups.UI
{
    public sealed class TextInputPopupWindow : BaseWindow
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_InputField inputField;

        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [SerializeField] private TMP_Text confirmButtonText;
        [SerializeField] private TMP_Text cancelButtonText;

        private UniTaskCompletionSource<TextInputPopupResult> _completionSource;

        public async UniTask<TextInputPopupResult> ShowAndWaitForResultAsync(
            string title,
            string message,
            string placeholder,
            string confirmText,
            string cancelText,
            string initialValue,
            CancellationToken token)
        {
            _completionSource = new UniTaskCompletionSource<TextInputPopupResult>();

            titleText.text = title;
            messageText.text = message;

            inputField.text = initialValue;
            inputField.placeholder.GetComponent<TMP_Text>().text = placeholder;

            confirmButtonText.text = confirmText;
            cancelButtonText.text = cancelText;

            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();

            confirmButton.onClick.AddListener(() =>
            {
                _completionSource.TrySetResult(
                    TextInputPopupResult.Confirm(inputField.text.Trim()));
            });

            cancelButton.onClick.AddListener(() =>
            {
                _completionSource.TrySetResult(TextInputPopupResult.Cancelled);
            });

            await ShowAsync();

            inputField.ActivateInputField();

            await using CancellationTokenRegistration registration = token.Register(() =>
            {
                _completionSource.TrySetResult(TextInputPopupResult.Cancelled);
            });

            TextInputPopupResult result = await _completionSource.Task;

            await HideAsync();

            return result;
        }
    }
}