using System.Threading;
using Core.UI.Windows.Data;
using Cysharp.Threading.Tasks;

namespace Core.UI.Windows.Contracts
{
    public interface IWindowFactory
    {
        UniTask<IWindow> CreateAsync(WindowId windowId, CancellationToken token = default);
        void Release(IWindow window);
    }
}