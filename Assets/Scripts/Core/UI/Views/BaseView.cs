using Core.UI.Windows.Components;
using R3;

namespace Core.UI.Views
{
    public abstract class BaseView : BaseWindow
    {
        protected readonly CompositeDisposable ViewDisposables = new();

        protected virtual void OnDisable()
        {
            ViewDisposables.Clear();
        }

        protected override void OnDestroy()
        {
            ViewDisposables.Dispose();
            base.OnDestroy();
        }
    }
}