using System.Threading;
using Core.UI.Windows.Contracts;
using Cysharp.Threading.Tasks;

namespace Core.SceneManagement.Loading.Contracts
{
    public interface ILoadingScreen : IWindow
    {
        void SetProgress(float progress);
        void ShowInstantly();
        void HideInstantly();
    }
}