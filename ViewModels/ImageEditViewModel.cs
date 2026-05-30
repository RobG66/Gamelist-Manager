using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public enum EditTool { None, RemoveBackground, Crop, Resize }

public partial class ImageEditViewModel : ViewModelBase, IDisposable
{
    #region Private Fields

    private readonly string _imagePath;
    private SKBitmap? _originalBitmap;
    private SKBitmap? _workingBitmap;
    private SKBitmap? _previewBitmap;
    private CancellationTokenSource? _cts;
    private bool _isSaving;
    private bool _isLoaded;
    private bool _updatingDimensions;
    private bool _hasUnsavedKeep;

    private record CropSnapshot(int X, int Y, int W, int H);
    private readonly Stack<CropSnapshot> _undoStack = new();
    private readonly Stack<CropSnapshot> _redoStack = new();

    #endregion

    #region Observable Properties

    [ObservableProperty] private Bitmap? _displayImage;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _imageSizeText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemoveBackgroundTool))]
    [NotifyPropertyChangedFor(nameof(IsCropTool))]
    [NotifyPropertyChangedFor(nameof(IsResizeTool))]
    [NotifyPropertyChangedFor(nameof(IsNoTool))]
    private EditTool _selectedTool = EditTool.None;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private bool _canApply;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _canCancel;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _canSave;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReloadCommand))]
    private bool _canReload;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    private bool _canUndo;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RedoCommand))]
    private bool _canRedo;

    // Remove Background
    [ObservableProperty] private double _tolerance = 20;
    [ObservableProperty] private double _edgeThreshold = 0.15;
    [ObservableProperty] private int _selectedModeIndex = -1;
    [ObservableProperty] private bool _removeEnclosed;
    [ObservableProperty] private bool _invertColors;
    [ObservableProperty] private bool _isFreeformMode;
    [ObservableProperty] private bool _isCircleMode;

    // Crop
    [ObservableProperty] private int _cropX;
    [ObservableProperty] private int _cropY;
    [ObservableProperty] private int _cropW;
    [ObservableProperty] private int _cropH;
    [ObservableProperty] private int _autoCropPadding = 4;

    // Resize
    [ObservableProperty] private bool _maintainAspectRatio = true;
    [ObservableProperty] private int _resizeWidth;
    [ObservableProperty] private int _resizeHeight;

    #endregion

    #region Computed Properties

    public bool IsNoTool               => SelectedTool == EditTool.None;
    public bool IsRemoveBackgroundTool => SelectedTool == EditTool.RemoveBackground;
    public bool IsCropTool             => SelectedTool == EditTool.Crop;
    public bool IsResizeTool           => SelectedTool == EditTool.Resize;

    public int SourceWidth  => _workingBitmap?.Width  ?? 0;
    public int SourceHeight => _workingBitmap?.Height ?? 0;

    #endregion

    #region Events

    public event Action<string?>? CloseRequested;

    #endregion

    public ImageEditViewModel(string imagePath)
    {
        _imagePath = imagePath;
    }

    #region Public Methods

    public async Task LoadAsync()
    {
        IsBusy = true;
        _originalBitmap = await Task.Run(() => SKBitmap.Decode(_imagePath));
        if (_originalBitmap == null) { CloseRequested?.Invoke(null); return; }

        _workingBitmap = _originalBitmap.Copy();
        _isLoaded = true;
        UpdateSizeText(_workingBitmap);
        SelectedTool = EditTool.Crop;   // show crop by default
        IsBusy = false;
    }

    /// <summary>Live drag — only update rect + status, never touch DisplayImage.</summary>
    public void SetCropRectLive(int x, int y, int w, int h)
    {
        CropX = x;
        CropY = y;
        CropW = w;
        CropH = h;
        StatusText = $"Crop: {w} × {h}  at ({x}, {y})";
    }

    /// <summary>Drag released — push undo state only.</summary>
    public void CommitCropRect(int oldX, int oldY, int oldW, int oldH)
    {
        _undoStack.Push(new CropSnapshot(oldX, oldY, oldW, oldH));
        _redoStack.Clear();
        UpdateUndoRedoState();
    }

    public void ResetCropToFull()
    {
        if (_workingBitmap == null) return;
        CropX = 0;
        CropY = 0;
        CropW = _workingBitmap.Width;
        CropH = _workingBitmap.Height;
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateUndoRedoState();
        StatusText = $"Crop reset to {_workingBitmap.Width} × {_workingBitmap.Height}";
    }

    #endregion

    #region Commands

    /// <summary>Apply — for crop, executes the crop immediately here.
    /// For other tools, commits the precomputed preview bitmap.</summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        if (SelectedTool == EditTool.Crop)
        {
            ApplyCropNow();
            return;
        }

        if (_previewBitmap == null) return;

        var old = _workingBitmap;
        _workingBitmap = _previewBitmap.Copy();
        _previewBitmap?.Dispose();
        _previewBitmap = null;
        DisposeDeferred(old);

        FinishApply();
    }

    private void ApplyCropNow()
    {
        if (_workingBitmap == null) return;

        int x = Math.Max(0, CropX);
        int y = Math.Max(0, CropY);
        int w = Math.Min(CropW, _workingBitmap.Width  - x);
        int h = Math.Min(CropH, _workingBitmap.Height - y);
        if (w <= 0 || h <= 0) return;

        var info    = new SKImageInfo(w, h, _workingBitmap.ColorType, _workingBitmap.AlphaType);
        var cropped = new SKBitmap(info);
        using var canvas = new SKCanvas(cropped);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(_workingBitmap,
            new SKRect(x, y, x + w, y + h),
            new SKRect(0, 0, w, h));

        var old = _workingBitmap;
        _workingBitmap = cropped;
        DisposeDeferred(old);

        FinishApply();
    }

    private void FinishApply()
    {
        _hasUnsavedKeep = true;
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateUndoRedoState();

        SelectedTool = EditTool.None;
        StatusText = "Changes applied — pick another tool or save.";
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        DisposeDeferred(_previewBitmap);
        _previewBitmap = null;

        SelectedTool = EditTool.None;
        StatusText = "Changes cancelled.";
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_isSaving) return;
        _isSaving = true;
        CanSave = false;

        // If crop tool is still active with unsaved rect, apply it first
        SKBitmap? toSave;
        if (SelectedTool == EditTool.Crop)
        {
            ApplyCropNow();
            toSave = _workingBitmap;
        }
        else
        {
            toSave = _previewBitmap ?? _workingBitmap;
        }

        if (toSave == null) { CloseRequested?.Invoke(null); return; }

        var savedPath = Path.ChangeExtension(_imagePath, ".png");
        try
        {
            var capture = toSave;
            await Task.Run(() => ImageHelper.SaveBitmapAsPng(capture, savedPath));
            CloseRequested?.Invoke(savedPath);
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
            _isSaving = false;
            CanSave = true;
        }
    }

    [RelayCommand]
    private void CloseWindow() => CloseRequested?.Invoke(null);

    [RelayCommand(CanExecute = nameof(CanReload))]
    private void Reload()
    {
        if (_originalBitmap == null) return;

        DisposeDeferred(_previewBitmap);
        _previewBitmap = null;

        var old = _workingBitmap;
        _workingBitmap = _originalBitmap.Copy();
        DisposeDeferred(old);

        _hasUnsavedKeep = false;
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateUndoRedoState();

        SelectedTool = EditTool.None;
        ShowWorkingBitmap();
        UpdateSizeText(_workingBitmap);
        UpdateButtonStates();
        StatusText = "Reverted to original.";
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push(new CropSnapshot(CropX, CropY, CropW, CropH));
        var snap = _undoStack.Pop();
        SetCropSnapshot(snap);
        UpdateUndoRedoState();
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push(new CropSnapshot(CropX, CropY, CropW, CropH));
        var snap = _redoStack.Pop();
        SetCropSnapshot(snap);
        UpdateUndoRedoState();
    }

    [RelayCommand]
    private unsafe void AutoCrop()
    {
        if (_workingBitmap == null) return;

        int w  = _workingBitmap.Width;
        int h  = _workingBitmap.Height;
        int top = -1, bottom = -1, left = -1, right = -1;

        byte* p  = (byte*)_workingBitmap.GetPixels();
        int   rb = _workingBitmap.RowBytes;

        for (int y = 0; y < h && top < 0; y++)
        {
            byte* row = p + y * rb;
            for (int x = 0; x < w; x++)
                if (row[x * 4 + 3] > 0) { top = y; break; }
        }
        if (top < 0) return;

        for (int y = h - 1; y >= top && bottom < 0; y--)
        {
            byte* row = p + y * rb;
            for (int x = 0; x < w; x++)
                if (row[x * 4 + 3] > 0) { bottom = y; break; }
        }
        for (int x = 0; x < w && left < 0; x++)
            for (int y = top; y <= bottom; y++)
                if ((p + y * rb)[x * 4 + 3] > 0) { left = x; break; }

        for (int x = w - 1; x >= left && right < 0; x--)
            for (int y = top; y <= bottom; y++)
                if ((p + y * rb)[x * 4 + 3] > 0) { right = x; break; }

        if (left < 0 || top < 0) return;

        int pad = AutoCropPadding;
        int nx = Math.Max(0, left - pad);
        int ny = Math.Max(0, top  - pad);
        int nr = Math.Min(w - 1, right  + pad);
        int nb = Math.Min(h - 1, bottom + pad);
        int nw = nr - nx + 1;
        int nh = nb - ny + 1;
        if (nw <= 0 || nh <= 0) return;

        PushCropSnapshot();
        CropX = nx;
        CropY = ny;
        CropW = nw;
        CropH = nh;
        StatusText = $"Auto-crop: {nw} × {nh} — click Apply to confirm";
    }

    #endregion

    #region Preview (Remove Background + Resize only)

    private async Task UpdatePreviewAsync()
    {
        if (!_isLoaded || _workingBitmap == null) return;
        if (SelectedTool == EditTool.None || SelectedTool == EditTool.Crop) return;

        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;

        IsBusy = true;
        // Do NOT disable CanApply here — button must stay visible and enabled throughout

        var source        = _workingBitmap;
        var tool          = SelectedTool;
        var modeIndex     = SelectedModeIndex;
        var bgMode        = modeIndex switch
        {
            0 => BackgroundRemovalMode.Circle,
            1 => BackgroundRemovalMode.CircleEdge,
            3 => BackgroundRemovalMode.ConvexHull,
            _ => BackgroundRemovalMode.Freeform
        };
        var tolerance     = (int)Tolerance;
        var edgeThreshold = (float)EdgeThreshold;
        var removeEnclosed= RemoveEnclosed;
        var invert        = InvertColors;
        var resizeW       = ResizeWidth;
        var resizeH       = ResizeHeight;

        if (tool == EditTool.RemoveBackground && modeIndex < 0)
        {
            StatusText = "Select a shape to preview";
            IsBusy = false;
            return;
        }

        if (tool == EditTool.Resize && (resizeW < 100 || resizeH < 100))
        {
            IsBusy = false;
            return;
        }

        SKBitmap? result = null;
        string strategy  = string.Empty;

        try
        {
            (result, strategy) = await Task.Run(() =>
            {
                switch (tool)
                {
                    case EditTool.RemoveBackground:
                    {
                        var bgColor = ImageHelper.DetectBackgroundColor(source);
                        var (r, s)  = ImageHelper.RemoveBackground(source, bgColor, tolerance, bgMode, removeEnclosed, edgeThreshold);
                        if (invert) { var inv = ImageHelper.InvertColors(r); r.Dispose(); r = inv; }
                        return (r, s);
                    }
                    case EditTool.Resize:
                    {
                        var resized = ImageHelper.ResizeBitmap(source, resizeW, resizeH);
                        return (resized, $"Resize: {resizeW} × {resizeH}");
                    }
                    default:
                        return ((SKBitmap?)null, string.Empty);
                }
            }, cts.Token);
        }
        catch (OperationCanceledException) { result?.Dispose(); return; }

        if (cts.IsCancellationRequested) { result?.Dispose(); return; }

        var old = _previewBitmap;
        _previewBitmap = result;

        if (result != null)
        {
            var avBitmap   = ImageHelper.ToAvaloniaBitmap(result);
            var oldDisplay = DisplayImage;
            DisplayImage   = avBitmap;
            DisposeDeferred(oldDisplay);
            UpdateSizeText(result);
        }

        DisposeDeferred(old);

        StatusText = strategy;
        IsBusy     = false;
        UpdateButtonStates();
    }

    #endregion

    #region Property Change Callbacks

    partial void OnSelectedToolChanged(EditTool value)
    {
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateUndoRedoState();

        DisposeDeferred(_previewBitmap);
        _previewBitmap = null;

        switch (value)
        {
            case EditTool.Crop:
                InitializeCropRect();
                ShowWorkingBitmap();
                UpdateSizeText(_workingBitmap);
                UpdateButtonStates();
                break;

            case EditTool.Resize:
                InitializeResizeDimensions();
                ShowWorkingBitmap();
                UpdateSizeText(_workingBitmap);
                UpdateButtonStates();
                _ = UpdatePreviewAsync();
                break;

            case EditTool.RemoveBackground:
                ShowWorkingBitmap();
                UpdateButtonStates();
                break;

            case EditTool.None:
            default:
                ShowWorkingBitmap();
                UpdateSizeText(_workingBitmap);
                UpdateButtonStates();
                break;
        }
    }

    partial void OnToleranceChanged(double value)
    {
        if (IsRemoveBackgroundTool) _ = UpdatePreviewAsync();
    }

    partial void OnEdgeThresholdChanged(double value)
    {
        if (IsRemoveBackgroundTool) _ = UpdatePreviewAsync();
    }

    partial void OnSelectedModeIndexChanged(int value)
    {
        IsCircleMode   = value == 1;
        IsFreeformMode = value == 2;
        if (IsRemoveBackgroundTool) _ = UpdatePreviewAsync();
    }

    partial void OnRemoveEnclosedChanged(bool value)
    {
        if (IsRemoveBackgroundTool) _ = UpdatePreviewAsync();
    }

    partial void OnInvertColorsChanged(bool value)
    {
        if (IsRemoveBackgroundTool) _ = UpdatePreviewAsync();
    }

    partial void OnResizeWidthChanged(int value)
    {
        if (_updatingDimensions || !IsResizeTool) return;
        if (MaintainAspectRatio && SourceWidth > 0 && SourceHeight > 0)
        {
            _updatingDimensions = true;
            ResizeHeight = Math.Max(1, Math.Min(2000, (int)Math.Round(value * (double)SourceHeight / SourceWidth)));
            _updatingDimensions = false;
        }
        _ = UpdatePreviewAsync();
    }

    partial void OnResizeHeightChanged(int value)
    {
        if (_updatingDimensions || !IsResizeTool) return;
        if (MaintainAspectRatio && SourceWidth > 0 && SourceHeight > 0)
        {
            _updatingDimensions = true;
            ResizeWidth = Math.Max(1, Math.Min(2000, (int)Math.Round(value * (double)SourceWidth / SourceHeight)));
            _updatingDimensions = false;
        }
        _ = UpdatePreviewAsync();
    }

    partial void OnMaintainAspectRatioChanged(bool value)
    {
        if (value && IsResizeTool && SourceWidth > 0 && SourceHeight > 0)
        {
            _updatingDimensions = true;
            ResizeHeight = Math.Max(1, Math.Min(2000, (int)Math.Round(ResizeWidth * (double)SourceHeight / SourceWidth)));
            _updatingDimensions = false;
            _ = UpdatePreviewAsync();
        }
    }

    #endregion

    #region Private Helpers

    private void ShowWorkingBitmap()
    {
        if (_workingBitmap == null) return;
        var avBitmap   = ImageHelper.ToAvaloniaBitmap(_workingBitmap);
        var oldDisplay = DisplayImage;
        DisplayImage   = avBitmap;
        DisposeDeferred(oldDisplay);
    }

    private void UpdateSizeText(SKBitmap? bmp)
        => ImageSizeText = bmp != null ? $"{bmp.Width} × {bmp.Height}" : string.Empty;

    private void UpdateButtonStates()
    {
        // Apply: always enabled when any tool is active (crop applies immediately; others apply preview)
        CanApply  = SelectedTool != EditTool.None;
        // Cancel: always enabled when any tool is active
        CanCancel = SelectedTool != EditTool.None;
        CanSave   = _hasUnsavedKeep || _previewBitmap != null;
        CanReload = _hasUnsavedKeep || _previewBitmap != null;
    }

    private void InitializeCropRect()
    {
        if (_workingBitmap == null) return;
        CropX = 0;
        CropY = 0;
        CropW = _workingBitmap.Width;
        CropH = _workingBitmap.Height;
    }

    private void InitializeResizeDimensions()
    {
        if (_workingBitmap == null) return;
        const int max = 2000;
        int w = _workingBitmap.Width;
        int h = _workingBitmap.Height;
        if (w > max || h > max)
        {
            double scale = Math.Min((double)max / w, (double)max / h);
            w = Math.Max(100, (int)Math.Round(w * scale));
            h = Math.Max(100, (int)Math.Round(h * scale));
        }
        _updatingDimensions = true;
        ResizeWidth  = w;
        ResizeHeight = h;
        _updatingDimensions = false;
    }

    private void PushCropSnapshot()
    {
        _undoStack.Push(new CropSnapshot(CropX, CropY, CropW, CropH));
        _redoStack.Clear();
        UpdateUndoRedoState();
    }

    private void SetCropSnapshot(CropSnapshot snap)
    {
        CropX = snap.X;
        CropY = snap.Y;
        CropW = snap.W;
        CropH = snap.H;
        StatusText = $"Crop: {snap.W} × {snap.H} at ({snap.X}, {snap.Y})";
    }

    private void UpdateUndoRedoState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }

    private static void DisposeDeferred(IDisposable? disposable)
    {
        if (disposable == null) return;
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => disposable.Dispose(),
            Avalonia.Threading.DispatcherPriority.Background);
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();

        var display  = DisplayImage;
        DisplayImage = null;

        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => display?.Dispose(),
            Avalonia.Threading.DispatcherPriority.Background);

        _previewBitmap?.Dispose();
        _workingBitmap?.Dispose();
        _originalBitmap?.Dispose();
        _previewBitmap  = null;
        _workingBitmap  = null;
        _originalBitmap = null;

        GC.SuppressFinalize(this);
    }

    #endregion
}
