using System.Windows;
using System.Windows.Media;

public static class VisualTreeHelperExtensions
{
    // Gets all visual children of a given parent filtered by type
    public static IEnumerable<T> GetAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            yield break;

        var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var grandChild in GetAllVisualChildren<T>(child))
            {
                yield return grandChild;
            }
        }
    }
}
