using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Gamelist_Manager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gamelist_Manager.Behaviors;

public class DataGridDragSelectionBehavior : Behavior<DataGrid>
{
    #region Fields
    private bool _isDragging;
    private readonly List<object> _selectionPath = new();
    private IPointer? _capturedPointer;

    // Deferred drag: if the press lands on the sole selected item, wait for movement
    // beyond the threshold before starting drag selection, so a second click can still
    // enter cell-edit mode.
    private bool _isPendingDrag;
    private Point _pendingDragOrigin;
    private object? _pendingDragItem;
    private const double DragThreshold = 4.0;

    // Continuous edge-scroll: fires at a dynamic interval while the pointer is held outside the grid.
    // The further past the boundary the pointer is, the faster the scroll.
    private DispatcherTimer? _scrollTimer;
    private int _scrollDirection;
    private double _scrollOvershoot;
    private const int ScrollIntervalMinMs = 20;   // fastest (pointer far outside)
    private const int ScrollIntervalMaxMs = 150;  // slowest (pointer just past boundary)
    private const double ScrollAccelZone = 50.0; // pixels of overshoot over which speed ramps up

    #endregion

    #region Lifecycle
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            AssociatedObject.AddHandler(InputElement.PointerPressedEvent, OnPointerPressedPersistent, RoutingStrategies.Tunnel);
            AssociatedObject.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
            AssociatedObject.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, handledEventsToo: true);
            AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
            AssociatedObject.AddHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost, handledEventsToo: true);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject != null)
        {
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressedPersistent);
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            AssociatedObject.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
            AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
            AssociatedObject.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
        }
    }
    #endregion

    private bool IsPersistentSelectionEnabled =>
        AssociatedObject?.DataContext is MainWindowViewModel vm && vm.IsPersistentSelectionEnabled;

    // Runs in the tunnel phase, before the DataGrid's own row-selection logic, so we can
    // mark the event handled and prevent the default selection-clearing behaviour.
    private void OnPointerPressedPersistent(object? sender, PointerPressedEventArgs e)
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

        if (item != null)
        {
            if (AssociatedObject.SelectedItems != null && AssociatedObject.SelectedItems.Contains(item))
                AssociatedObject.SelectedItems.Remove(item);
            else
                AssociatedObject.SelectedItems?.Add(item);
        }

        // Start drag state so OnPointerMoved can extend the selection while the button is held.
        _isDragging = true;
        _selectionPath.Clear();
        if (item != null)
            _selectionPath.Add(item);
        _capturedPointer = e.Pointer;
        e.Pointer.Capture(AssociatedObject);

        // Mark handled so the DataGrid does not process this press and reset the selection.
        e.Handled = true;
    }

    #region Pointer Handlers
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (AssociatedObject == null) return;

        // The tunnel handler already handled this press for persistent selection mode.
        if (IsPersistentSelectionEnabled) return;

        var properties = e.GetCurrentPoint(AssociatedObject).Properties;
        if (!properties.IsLeftButtonPressed) return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            return;

        if (e.Source is not Visual source || source.FindAncestorOfType<DataGridCell>() == null)
            return;

        if (source.FindAncestorOfType<CheckBox>() != null)
            return;

        var point = e.GetPosition(AssociatedObject);
        var item = GetItemAtPoint(point);

        if (item != null && AssociatedObject.SelectedItems?.Count == 1 && AssociatedObject.SelectedItems.Contains(item))
        {
            _isPendingDrag = true;
            _pendingDragOrigin = point;
            _pendingDragItem = item;
            return;
        }

        _isDragging = true;
        _selectionPath.Clear();

        if (item != null)
        {
            _selectionPath.Add(item);
            AssociatedObject.SelectedItems?.Clear();
            AssociatedObject.SelectedItems?.Add(item);
        }

        _capturedPointer = e.Pointer;
        e.Pointer.Capture(AssociatedObject);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject == null || (!_isDragging && !_isPendingDrag)) return;

        var properties = e.GetCurrentPoint(AssociatedObject).Properties;
        if (!properties.IsLeftButtonPressed)
        {
            StopDragging();
            return;
        }

        var point = e.GetPosition(AssociatedObject);

        if (_isPendingDrag && _pendingDragItem != null)
        {
            var delta = point - _pendingDragOrigin;
            if (Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold)
                return;

            // Threshold crossed — promote to a full drag.
            _isPendingDrag = false;
            _isDragging = true;
            _selectionPath.Clear();
            _selectionPath.Add(_pendingDragItem);
            _pendingDragItem = null;
            _capturedPointer = e.Pointer;
            e.Pointer.Capture(AssociatedObject);
        }

        if (!_isDragging) return;

        var gridHeight = AssociatedObject.Bounds.Height;

        if (point.Y < 0)
        {
            StartScrollTimer(-1, -point.Y);
        }
        else if (point.Y > gridHeight)
        {
            StartScrollTimer(1, point.Y - gridHeight);
        }
        else
        {
            StopScrollTimer();
            SelectItemAtPoint(point);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) => StopDragging();
    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) => StopDragging();
    #endregion

    #region Selection Helpers
    private void StopDragging()
    {
        _isDragging = false;
        _isPendingDrag = false;
        _pendingDragItem = null;
        _selectionPath.Clear();
        StopScrollTimer();

        if (_capturedPointer != null && AssociatedObject != null)
        {
            _capturedPointer.Capture(null);
            _capturedPointer = null;
        }
    }

    private void StartScrollTimer(int direction, double overshoot)
    {
        _scrollOvershoot = overshoot;

        if (_scrollTimer != null && _scrollDirection == direction)
        {
            // Already running in the right direction — just update the interval.
            _scrollTimer.Interval = TimeSpan.FromMilliseconds(IntervalForOvershoot(overshoot));
            return;
        }

        StopScrollTimer();
        _scrollDirection = direction;
        _scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(IntervalForOvershoot(overshoot)) };
        _scrollTimer.Tick += OnScrollTick;
        _scrollTimer.Start();
    }

    // Maps overshoot distance to an interval: near boundary = slow, far = fast.
    private static int IntervalForOvershoot(double overshoot)
    {
        var t = Math.Clamp(overshoot / ScrollAccelZone, 0.0, 1.0);
        return (int)(ScrollIntervalMaxMs - t * (ScrollIntervalMaxMs - ScrollIntervalMinMs));
    }

    private void StopScrollTimer()
    {
        if (_scrollTimer == null) return;
        _scrollTimer.Stop();
        _scrollTimer.Tick -= OnScrollTick;
        _scrollTimer = null;
    }

    private void OnScrollTick(object? sender, EventArgs e)
    {
        if (AssociatedObject == null || !_isDragging) { StopScrollTimer(); return; }

        var neighbor = GetNeighborItem(_scrollDirection);
        if (neighbor == null) return;

        AssociatedObject.ScrollIntoView(neighbor, null);
        AddToSelection(neighbor);
    }

    // Returns the item immediately before (direction=-1) or after (direction=1) the tail of
    // the current selection path, using the DataGrid's sorted CollectionView to determine order.
    private object? GetNeighborItem(int direction)
    {
        if (AssociatedObject == null) return null;

        // CollectionView is the DataGrid's internal sorted/filtered view — it always reflects
        // the current display order, regardless of what is bound to ItemsSource.
        var view = AssociatedObject.CollectionView;
        if (view == null) return null;

        var items = view.Cast<object>().ToList();
        if (items.Count == 0) return null;

        var tail = _selectionPath.LastOrDefault();
        var tailIndex = tail != null ? items.IndexOf(tail) : -1;
        if (tailIndex < 0) return null;

        var neighborIndex = tailIndex + direction;
        if (neighborIndex < 0 || neighborIndex >= items.Count) return null;

        return items[neighborIndex];
    }

    private void AddToSelection(object item)
    {
        if (AssociatedObject == null) return;
        if (item == _selectionPath.LastOrDefault()) return;

        var existingIndex = _selectionPath.IndexOf(item);
        if (existingIndex >= 0)
        {
            // Reversing — deselect everything after this item.
            for (var i = _selectionPath.Count - 1; i > existingIndex; i--)
            {
                AssociatedObject.SelectedItems?.Remove(_selectionPath[i]);
                _selectionPath.RemoveAt(i);
            }
        }
        else
        {
            _selectionPath.Add(item);
            AssociatedObject.SelectedItems?.Add(item);
        }
    }

    private void SelectItemAtPoint(Point point)
    {
        var item = GetItemAtPoint(point);
        if (item != null) AddToSelection(item);
    }

    private object? GetItemAtPoint(Point point)
    {
        if (AssociatedObject == null) return null;

        var element = AssociatedObject.InputHitTest(point);
        var visual = element as Visual;
        while (visual != null && visual != AssociatedObject)
        {
            if (visual is DataGridRow row)
                return row.DataContext;
            visual = visual.GetVisualParent<Visual>();
        }

        return null;
    }
    #endregion
}
