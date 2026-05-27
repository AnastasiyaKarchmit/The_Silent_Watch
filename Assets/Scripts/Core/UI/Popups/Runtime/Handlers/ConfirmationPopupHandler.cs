using System.Threading;
using Core.UI.Popups.Requests;
using Core.UI.Popups.Runtime.Handlers.Core;
using Core.UI.Popups.UI;
using Core.UI.Windows.Contracts;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;

namespace Core.UI.Popups.Runtime.Handlers
{
    public sealed class ConfirmationPopupHandler :
        PopupHandler<ConfirmationPopupRequest, bool>
    {
        private readonly IWindowService _windowService;

        public ConfirmationPopupHandler(IWindowService windowService)
        {
            _windowService = windowService;
        }

        protected override async UniTask<bool> HandleAsync(
            ConfirmationPopupRequest request,
            CancellationToken token)
        {
            ConfirmationPopupWindow window =
                await _windowService.GetOrCreateAsync<ConfirmationPopupWindow>(
                    WindowId.ConfirmationPopup,
                    token);

            token.ThrowIfCancellationRequested();

            window.RootRectTransform.SetAsLastSibling();

            return await window.ShowAndWaitForResultAsync(
                request.Title,
                request.Message,
                request.YesText,
                request.NoText,
                token);
        }
    }
}