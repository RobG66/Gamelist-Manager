using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
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
    #endregion

    #region Lifecycle
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
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
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            AssociatedObject.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
            AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
            AssociatedObject.RemoveHandler(InputElement.PointerCaptureLostEvent, OnPointerCaptureLost);
        }
    }
    #endregion

    #region Pointer Handlers
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (AssociatedObject == null) return;

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
            var neighbor = GetNeighborItem(direction: -1);
            if (neighbor != null)
            {
                AssociatedObject.ScrollIntoView(neighbor, null);
                AddToSelection(neighbor);
            }
        }
        else if (point.Y > gridHeight)
        {
            var neighbor = GetNeighborItem(direction: 1);
            if (neighbor != null)
            {
                AssociatedObject.ScrollIntoView(neighbor, null);
                AddToSelection(neighbor);
            }
        }
        else
        {
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

        if (_capturedPointer != null && AssociatedObject != null)
        {
            _capturedPointer.Capture(null);
            _capturedPointer = null;
        }
    }

    // Returns the item immediately before (direction=-1) or after (direction=1)
    // the tail of the current selection path.
    private object? GetNeighborItem(int direction)
    {
        if (AssociatedObject?.ItemsSource == null) return null;

        var items = AssociatedObject.ItemsSource.Cast<object>().ToList();
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
