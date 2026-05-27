using Core.UI.Windows.Contracts;

namespace Core.UI.Windows.Extensions
{
    public static class WindowServiceExtensions
    {
        public static IWindowService GetRootWindowService(this IWindowService windowService)
        {
            if (windowService == null)
                return null;

            var current = windowService;

            while (current.Parent != null)
                current = current.Parent;

            return current;
        }
    }
}