using GamelistManager.classes;
using System.Data;

public class ChangeTracker
{
    private DataTable _table;
    private readonly Stack<DataTable> _undoStack = new Stack<DataTable>();
    private readonly Stack<DataTable> _redoStack = new Stack<DataTable>();
    private int _maxUndoCount;
    private bool _isBulkOperation = false;
    private bool _isTrackingEnabled = true; // New flag to manage tracking state

    // Event that will be triggered whenever the undo/redo availability changes
    public event EventHandler? UndoRedoStateChanged;

    // Event that will be triggered when tracking is resumed
    public event EventHandler? TrackingResumed;

    public ChangeTracker(DataTable table, int maxUndoCount)
    {
        _table = table;
        _maxUndoCount = maxUndoCount;
        _undoStack.Push(_table.Copy());

        // Subscribe to DataTable events
        SubscribeToEvents();

        // Initially trigger the state changed event to update button states
        OnUndoRedoStateChanged();
    }

    private void Table_RowChanged(object sender, DataRowChangeEventArgs e) => RecordChange();
    private void Table_RowDeleted(object sender, DataRowChangeEventArgs e) => RecordChange();

    public void StartBulkOperation()
    {
        _isBulkOperation = true;
    }

    public void EndBulkOperation()
    {
        _isBulkOperation = false;
        RecordChange();
    }

    private void RecordChange()
    {
        SharedData.IsDataChanged = true;

        if (!_isTrackingEnabled || _isBulkOperation || _maxUndoCount == 0) return;

        _redoStack.Clear();
        _undoStack.Push(_table.Copy());

        // Ensure the undo stack does not exceed the maximum limit
        if (_undoStack.Count > _maxUndoCount)
        {
            _undoStack.TrimExcess();
        }

        // Trigger the event to notify the application
        OnUndoRedoStateChanged();
    }

    public void Undo()
    {
        if (_undoStack.Count > 1)
        {
            _redoStack.Push(_undoStack.Pop());
            UnsubscribeFromEvents();
            RestoreTable(_undoStack.Peek());
            SubscribeToEvents();

            // Trigger the event to notify the application
            OnUndoRedoStateChanged();
        }
    }

    public void Redo()
    {
        if (_redoStack.Count > 0)
        {
            _undoStack.Push(_redoStack.Pop());
            UnsubscribeFromEvents();
            RestoreTable(_undoStack.Peek());
            SubscribeToEvents();

            // Trigger the event to notify the application
            OnUndoRedoStateChanged();
        }
    }

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    private void RestoreTable(DataTable table)
    {
        _table.Clear();
        foreach (DataRow row in table.Rows)
        {
            _table.ImportRow(row);
        }
    }

    private void SubscribeToEvents()
    {
        _table.RowChanged += Table_RowChanged;
        _table.RowDeleted += Table_RowDeleted;
    }

    private void UnsubscribeFromEvents()
    {
        _table.RowChanged -= Table_RowChanged;
        _table.RowDeleted -= Table_RowDeleted;
    }

    // Method to trigger the UndoRedoStateChanged event
    protected virtual void OnUndoRedoStateChanged()
    {
        UndoRedoStateChanged?.Invoke(this, EventArgs.Empty);
    }

    // Method to trigger the TrackingResumed event
    protected virtual void OnTrackingResumed()
    {
        TrackingResumed?.Invoke(this, EventArgs.Empty);
    }

    // Method to reset the ChangeTracker with a new table and maxUndoCount
    public void Reset(DataTable newTable, int newMaxUndoCount)
    {
        // Unsubscribe from events of the current table
        UnsubscribeFromEvents();

        // Update the internal table reference
        _table.Clear();
        _table.Dispose();
        _table = newTable;

        // Update the max undo count
        _maxUndoCount = newMaxUndoCount;

        // Clear the undo and redo stacks
        _undoStack.Clear();
        _redoStack.Clear();

        // Push the initial state of the new table
        _undoStack.Push(_table.Copy());

        // Subscribe to events of the new table
        SubscribeToEvents();

        // Trigger the state changed event to update button states
        OnUndoRedoStateChanged();

    }

    // Method to suspend change tracking
    public void SuspendTracking()
    {
        _isTrackingEnabled = false;
    }

    // Method to resume change tracking
    public void ResumeTracking()
    {
        _isTrackingEnabled = true;
        OnTrackingResumed(); // Trigger the TrackingResumed event
    }
}
