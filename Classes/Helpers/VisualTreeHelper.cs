using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class VisualTreeHelper
    {
        public static IEnumerable<T> GetVisualChildren<T>(Control parent) where T : class
        {
            if (parent == null)
                yield break;

            var queue = new Queue<Control>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // Check if current control is of desired type
                if (current is T typedControl)
                {
                    yield return typedControl;
                }

                // Add visual children to queue
                foreach (var child in current.GetVisualChildren().OfType<Control>())
                {
                    queue.Enqueue(child);
                }
            }
        }

        public static IEnumerable<T> GetLogicalChildren<T>(Control parent) where T : class
        {
            if (parent == null)
                yield break;

            var queue = new Queue<Control>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // Check if current control is of desired type
                if (current is T typedControl)
                {
                    yield return typedControl;
                }

                // Add logical children to queue
                foreach (var child in current.GetLogicalChildren().OfType<Control>())
                {
                    queue.Enqueue(child);
                }
            }
        }

        public static IEnumerable<T> GetAllChildren<T>(Control parent) where T : class
        {
            var visual = GetVisualChildren<T>(parent);
            var logical = GetLogicalChildren<T>(parent);
            
            // Combine and remove duplicates
            return visual.Concat(logical).Distinct();
        }

        public static T? FindParent<T>(Control child) where T : class
        {
            if (child == null)
                return null;

            var parent = child.Parent as Control;
            
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                    
                parent = parent.Parent as Control;
            }

            return null;
        }

        public static T? FindChild<T>(Control parent) where T : class
        {
            return GetVisualChildren<T>(parent).FirstOrDefault();
        }

        public static T? FindChildByName<T>(Control parent, string name) where T : Control
        {
            return GetVisualChildren<T>(parent).FirstOrDefault(c => c.Name == name);
        }
    }
}
