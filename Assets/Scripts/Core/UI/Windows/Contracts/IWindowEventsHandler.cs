namespace Core.UI.Windows.Contracts
{
    public interface IWindowEventsHandler
    {
        public void OnActivated();
        public void OnDeactivated();
    }
}