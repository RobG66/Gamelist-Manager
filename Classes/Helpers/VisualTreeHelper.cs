using Avalonia.Controls;

namespace Gamelist_Manager.Classes.Helpers
{
    internal static class VisualTreeHelper
    {
        // Walks up the visual tree to find the nearest ancestor whose DataContext is of type T.
        internal static T? FindAncestorViewModel<T>(Control? start) where T : class
        {
            Control? current = start?.Parent as Control;
            while (current != null)
            {
                if (current.DataContext is T vm)
                    return vm;
                current = current.Parent as Control;
            }
            return null;
        }
    }
}
