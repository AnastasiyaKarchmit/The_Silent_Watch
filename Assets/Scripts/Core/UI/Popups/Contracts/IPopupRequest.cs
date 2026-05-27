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
}