using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Gamelist_Manager.ViewModels;

namespace Gamelist_Manager.Behaviors;

// Prevents the DataGrid from clearing the multi-selection when the user clicks a row
// that is already part of the selection, while persistent selection mode is active.
// Right-clicks on rows are also blocked from resetting the selection so the context
// menu opens without losing the current selection.
public class DataGridPersistentSelectionBehavior : Behavior<DataGrid>
{
    #region Lifecycle
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
            AssociatedObject.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject != null)
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
    }
    #endregion

    private bool IsPersistentSelectionEnabled =>
        AssociatedObject?.DataContext is MainWindowViewModel vm && vm.IsPersistentSelectionEnabled;

    // Runs in the tunnel phase, before the DataGrid's own row-selection logic, so we can
    // mark the event handled and prevent the default selection-clearing behaviour.
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (AssociatedObject == null || !IsPersistentSelectionEnabled) return;

        var properties = e.GetCurrentPoint(AssociatedObject).Properties;

        // Right-click: block the DataGrid from clearing selection, but do nothing else
        // so the context menu still opens normally.
        if (properties.IsRightButtonPressed)
        {
            if (e.Source is Visual rightSource && rightSource.FindAncestorOfType<DataGridCell>() != null)
                e.Handled = true;
            return;
        }

        if (!properties.IsLeftButtonPressed) return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            return;

        if (e.Source is not Visual source || source.FindAncestorOfType<DataGridCell>() == null)
            return;

        if (source.FindAncestorOfType<CheckBox>() != null)
            return;

        var point = e.GetPosition(AssociatedObject);
        var item = GetItemAtPoint(point);

        if (item != null && IsItemInSource(item))
        {
            if (AssociatedObject.SelectedItems != null && AssociatedObject.SelectedItems.Contains(item))
                AssociatedObject.SelectedItems.Remove(item);
            else
                AssociatedObject.SelectedItems?.Add(item);
        }

        // Mark handled so the DataGrid does not process this press and reset the selection.
        e.Handled = true;
    }

    #region Private Methods
    // Returns the data item under the given point by walking up the visual tree.
    // Guards against DataGridRow instances that have a null DataContext, which can
    // occur mid-rebuild when the source cache is being repopulated after a sort.
    private object? GetItemAtPoint(Avalonia.Point point)
    {
        if (AssociatedObject == null) return null;

        var element = AssociatedObject.InputHitTest(point);
        var visual = element as Visual;
        while (visual != null && visual != AssociatedObject)
        {
            if (visual is DataGridRow row && row.DataContext != null)
                return row.DataContext;
            visual = visual.GetVisualParent<Visual>();
        }

        return null;
    }

    // Guards against adding/removing an item that is no longer present in the
    // DataGrid's ItemsSource, which can happen when a click arrives during the
    // brief window after the source cache is cleared and before it is fully
    // repopulated (e.g. during sort-to-top).
    private bool IsItemInSource(object item)
    {
        if (AssociatedObject?.ItemsSource is not System.Collections.IEnumerable source)
            return false;

        foreach (var candidate in source)
        {
            if (ReferenceEquals(candidate, item))
                return true;
        }

        return false;
    }
    #endregion
}
