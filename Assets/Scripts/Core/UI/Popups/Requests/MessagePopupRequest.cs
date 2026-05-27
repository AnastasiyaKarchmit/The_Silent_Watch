using Core.UI.Popups.Contracts;
using UnityEngine;

namespace Core.UI.Popups.Requests
{
    public sealed class MessagePopupRequest : PopupRequest<PopupClosed>
    {
        public string Title { get; }
        public string Message { get; }
        public string CloseText { get; }

        public MessagePopupRequest(
            string title,
            string message,
            string closeText = "OK")
        {
            Title = title;
            Message = message;
            CloseText = closeText;
        }
    }
}