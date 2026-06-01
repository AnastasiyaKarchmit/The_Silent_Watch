using System;

namespace Core.UI.Popups.Contracts
{
    public interface IPopupRequest
    {
        Type ResultType { get; }
    }

    public interface IPopupRequest<TResult> : IPopupRequest
    {
    }

    public abstract class PopupRequest<TResult> : IPopupRequest<TResult>
    {
        public Type ResultType => typeof(TResult);
    }

    public readonly struct PopupClosed
    {
        public static readonly PopupClosed Value = new();
    }
    
    public readonly struct TextInputPopupResult
    {
        public bool Confirmed { get; }
        public string Text { get; }

        public TextInputPopupResult(bool confirmed, string text)
        {
            Confirmed = confirmed;
            Text = text;
        }

        public static TextInputPopupResult Cancelled =>
            new(false, string.Empty);

        public static TextInputPopupResult Confirm(string text) =>
            new(true, text);
    }
    
}