using GamelistManager.classes;
using System.Data;

public class ChangeTracker
{
    // Private fields for managing the data table, undo/redo stacks, and settings
    private DataTable? _table;
    private readonly LinkedList<DataTable> _undoStack = new();
    private readonly LinkedList<DataTable> _redoStack = new();
    private int _maxUndoCount;
    private bool _isBulkOperation;
    private bool _isTrackingPaused;

    // Public property to control whether tracking is enabled or disabled
    public bool IsTrackingEnabled { get; private set; } = false;

    // Events to notify subscribers about changes in undo/redo state and tracking resumption
    public event EventHandler? UndoRedoStateChanged;
    public event EventHandler? TrackingResumed;

    // Starts tracking changes on a new DataTable with a specified max undo count
    public void StartTracking(DataTable table, int maxUndoCount)
    {
        // Set up the new table and max undo count for this tracking session
        _table = table;
        _maxUndoCount = maxUndoCount;

        // Stop any previous tracking session and reset stacks
        StopTracking();

        // Save the initial state of the table to allow "Undo" back to this starting point
        _undoStack.AddLast(_table.Copy());

        // Subscribe to row change and delete events to detect changes in the table
        SubscribeToTableEvents();

        // Enable tracking and reset pause state
        IsTrackingEnabled = true;
        _isTrackingPaused = false;

        // Trigger the event to notify any listeners about the new undo/redo state
        OnUndoRedoStateChanged();
    }

    // Stops tracking by unsubscribing from events, clearing stacks, and disabling tracking
    public void StopTracking()
    {
        // Remove event subscriptions to stop monitoring changes
        UnsubscribeFromTableEvents();

        // Clear undo and redo stacks to reset tracking state
        _undoStack.Clear();
        _redoStack.Clear();

        // Disable tracking and reset pause state
        IsTrackingEnabled = false;
        _isTrackingPaused = false;

        // Trigger the state change event to update any UI or logic depending on the undo/redo availability
        OnUndoRedoStateChanged();
    }

    // Temporarily pauses change tracking; useful during batch updates
    public void PauseTracking() => _isTrackingPaused = true;

    // Resumes change tracking if it was paused, and notifies subscribers
    public void ResumeTracking()
    {
        _isTrackingPaused = false;
        TrackingResumed?.Invoke(this, EventArgs.Empty);
    }

    // Event handler for row changed or deleted events - records changes to the undo stack
    private void TableRowChangedOrDeleted(object? sender, DataRowChangeEventArgs e) => RecordChange();

    // Marks the start of a bulk operation, temporarily suspending change recording
    public void StartBulkOperation() => _isBulkOperation = true;

    // Ends a bulk operation and records the final change after the batch update
    public void EndBulkOperation()
    {
        _isBulkOperation = false;
        RecordChange();
    }

    // Records a change by saving the current table state to the undo stack, if tracking is enabled
    private void RecordChange()
    {
        // Do nothing if tracking is paused, in bulk mode, or undo functionality is disabled
        if (!IsTrackingEnabled || _isTrackingPaused || _isBulkOperation || _maxUndoCount == 0) return;

        // Mark that data has changed
        SharedData.IsDataChanged = true;

        // Clear the redo stack to invalidate any "redo" history after a new change
        _redoStack.Clear();

        // Save a copy of the current table state to the undo stack
        _undoStack.AddLast(_table!.Copy());

        // Trim the undo stack to ensure it doesn't exceed the specified max undo count
        // -1 because there is always 1 item in the undo stack
        if ((_undoStack.Count - 1) > _maxUndoCount)
        {
            _undoStack.RemoveFirst(); // Remove the oldest item
        }

        // Notify subscribers that the undo/redo state has changed
        OnUndoRedoStateChanged();
    }

    // Undoes the last change by reverting to the previous state in the undo stack
    public void Undo()
    {
        // Ensure there’s at least one previous state to revert to
        if (_undoStack.Count <= 1) return;

        // Move the current state to the redo stack, then remove the last state from the undo stack
        _redoStack.AddLast(_undoStack.Last!.Value);
        _undoStack.RemoveLast();

        // Restore the table to the previous state and re-subscribe to events
        RestoreTable(_undoStack.Last!.Value);

        // Notify subscribers that the undo/redo state has changed
        OnUndoRedoStateChanged();
    }

    // Redoes the last undone change by restoring the next state in the redo stack
    public void Redo()
    {
        // Ensure there’s a state to redo to
        if (_redoStack.Count == 0) return;

        // Move the state from redo stack back to undo stack, making it the current state
        _undoStack.AddLast(_redoStack.Last!.Value);
        _redoStack.RemoveLast();

        // Restore the table to this redone state and re-subscribe to events
        RestoreTable(_undoStack.Last!.Value);

        // Notify subscribers that the undo/redo state has changed
        OnUndoRedoStateChanged();
    }

    // Properties to expose the count of undo and redo actions available
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    // Restores the DataTable to a specific state by clearing and re-importing rows
    private void RestoreTable(DataTable table)
    {
        // Temporarily disable event subscriptions and data table notifications
        UnsubscribeFromTableEvents();
        _table!.BeginLoadData();

        // Use Merge to efficiently replace the current table's data with the new state
        _table.Clear();
        _table.Merge(table, preserveChanges: false);

        // End load data mode and re-enable event subscriptions
        _table.EndLoadData();
        SubscribeToTableEvents();
    }

    // Subscribes to DataTable events for row changes and deletions
    private void SubscribeToTableEvents()
    {
        _table!.RowChanged += TableRowChangedOrDeleted;
        _table.RowDeleted += TableRowChangedOrDeleted;
    }

    // Unsubscribes from DataTable events to stop monitoring changes
    private void UnsubscribeFromTableEvents()
    {
        if (_table != null)
        {
            _table.RowChanged -= TableRowChangedOrDeleted;
            _table.RowDeleted -= TableRowChangedOrDeleted;
        }
    }

    // Triggers the UndoRedoStateChanged event to update subscribers on undo/redo availability
    protected virtual void OnUndoRedoStateChanged() => UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
}
