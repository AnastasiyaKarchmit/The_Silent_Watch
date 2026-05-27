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
    public sealed class MessagePopupHandler :
        PopupHandler<MessagePopupRequest, PopupClosed>
    {
        private readonly IWindowService _windowService;

        public MessagePopupHandler(IWindowService windowService)
        {
            _windowService = windowService;
        }

        protected override async UniTask<PopupClosed> HandleAsync(
            MessagePopupRequest request,
            CancellationToken token)
        {
            MessagePopupWindow window =
                await _windowService.GetOrCreateAsync<MessagePopupWindow>(
                    WindowId.MessagePopup,
                    token);

            token.ThrowIfCancellationRequested();

            window.RootRectTransform.SetAsLastSibling();

            return await window.ShowAndWaitForCloseAsync(
                request.Title,
                request.Message,
                request.CloseText,
                token);
        }
    }
}