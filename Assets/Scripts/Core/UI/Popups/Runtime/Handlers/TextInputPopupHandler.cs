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
    public sealed class TextInputPopupHandler :
        PopupHandler<TextInputPopupRequest, TextInputPopupResult>
    {
        private readonly IWindowService _windowService;

        public TextInputPopupHandler(IWindowService windowService)
        {
            _windowService = windowService;
        }

        protected override async UniTask<TextInputPopupResult> HandleAsync(
            TextInputPopupRequest request,
            CancellationToken token)
        {
            TextInputPopupWindow window =
                await _windowService.GetOrCreateAsync<TextInputPopupWindow>(
                    WindowId.TextInputPopup,
                    token);

            token.ThrowIfCancellationRequested();

            window.RootRectTransform.SetAsLastSibling();

            return await window.ShowAndWaitForResultAsync(
                request.Title,
                request.Message,
                request.Placeholder,
                request.ConfirmText,
                request.CancelText,
                request.InitialValue,
                token);
        }
    }
}