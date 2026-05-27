using UnityEngine;

namespace Core.UI.Popups.Data
{
    public readonly struct PopupRequest
    {
        public readonly Sprite Icon;
        public readonly string Title;
        public readonly string AmountText;
        public readonly float Duration;

        public PopupRequest(
            Sprite icon,
            string title,
            string amountText,
            float duration = 2f)
        {
            Icon = icon;
            Title = title;
            AmountText = amountText;
            Duration = duration;
        }
    }
}