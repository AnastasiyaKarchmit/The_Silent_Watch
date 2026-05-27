using Core.UI.Popups.Contracts;

namespace Core.UI.Popups.Requests
{
    public sealed class ConfirmationPopupRequest : PopupRequest<bool>
    {
        public string Title { get; }
        public string Message { get; }
        public string YesText { get; }
        public string NoText { get; }

        public ConfirmationPopupRequest(
            string title,
            string message,
            string yesText = "Yes",
            string noText = "No")
        {
            Title = title;
            Message = message;
            YesText = yesText;
            NoText = noText;
        }
    }
}