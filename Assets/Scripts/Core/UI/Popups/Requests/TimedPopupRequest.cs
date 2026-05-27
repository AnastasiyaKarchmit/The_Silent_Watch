using Core.UI.Popups.Contracts;
using UnityEngine;

namespace Core.UI.Popups.Requests
{
    public sealed class TimedPopupRequest : PopupRequest<PopupClosed>
    {
        public Sprite Icon { get; }
        public string Title { get; }
        public string Description { get; }
        public string AmountText { get; }
        public float Duration { get; }

        public TimedPopupRequest(
            Sprite icon,
            string title,
            string description,
            string amountText,
            float duration = 2f)
        {
            Icon = icon;
            Title = title;
            Description = description;
            AmountText = amountText;
            Duration = duration;
        }
    }
}