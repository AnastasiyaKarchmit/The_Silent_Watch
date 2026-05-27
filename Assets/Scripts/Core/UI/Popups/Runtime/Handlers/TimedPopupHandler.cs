using System.Threading;
using Core.UI.Popups.Contracts;
using Core.UI.Popups.Requests;
using Core.UI.Popups.Runtime.Handlers.Core;
using Core.UI.Popups.UI;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;

namespace Core.UI.Popups.Runtime.Handlers
{
    public sealed class TimedPopupHandler :
        PopupHandler<TimedPopupRequest, PopupClosed>
    {
        private readonly IWindowService _windowService;

        public TimedPopupHandler(IWindowService windowService)
        {
            _windowService = windowService;
        }

        protected override async UniTask<PopupClosed> HandleAsync(
            TimedPopupRequest request,
            CancellationToken token)
        {
            TimedPopupWindow window =
                await _windowService.GetOrCreateAsync<TimedPopupWindow>(
                    WindowId.PickupPopup,
                    token);
            
            token.ThrowIfCancellationRequested();
            
            window.RootRectTransform.SetAsLastSibling();
            
            await window.ShowTemporaryAsync(
                request.Icon,
                request.Title,
                request.Description,
                request.AmountText,
                request.Duration,
                token);

            return PopupClosed.Value;
        }
    }
}