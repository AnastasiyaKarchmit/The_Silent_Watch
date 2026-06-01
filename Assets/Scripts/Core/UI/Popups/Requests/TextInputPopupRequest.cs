using Core.UI.Popups.Contracts;

namespace Core.UI.Popups.Requests
{
    public sealed class TextInputPopupRequest : PopupRequest<TextInputPopupResult>
    {
        public string Title { get; }
        public string Message { get; }
        public string Placeholder { get; }
        public string ConfirmText { get; }
        public string CancelText { get; }
        public string InitialValue { get; }

        public TextInputPopupRequest(
            string title,
            string message,
            string placeholder = "",
            string confirmText = "Confirm",
            string cancelText = "Cancel",
            string initialValue = "")
        {
            Title = title;
            Message = message;
            Placeholder = placeholder;
            ConfirmText = confirmText;
            CancelText = cancelText;
            InitialValue = initialValue;
        }
    }
}