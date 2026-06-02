using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public enum EditTool { None, RemoveBackground, Crop, Resize, Refine }

public partial class ImageEditViewModel : ViewModelBase, IDisposable
{
    #region Constants

    // Zoom
    public const double MinZoom     = 0.10;   //  10 %
    public const double MaxZoom     = 8.00;   // 800 %
    public const double ZoomStep    = 1.25;   // per button click
    public const double DefaultZoom = 1.00;   // 100 %

    // Resize
    public const int    MinResizePx  = 1;
    public const int    MaxResizePx  = 2000;
    public const double MinResizePct = 1;
    public const double MaxResizePct = 400;

    // Refine
    public const double MinSharpen    = 0;
    public const double MaxSharpen    = 100;
    public const double MinCleanEdges = 0;
    public const double MaxCleanEdges = 5;

    // Remove Background
    public const double DefaultTolerance     = 20;
    public const double MinTolerance         = 0;
    public const double MaxTolerance         = 50;
    public const double DefaultEdgeThreshold = 0.15;
    public const double MinEdgeThreshold     = 0.05;
    public const double MaxEdgeThreshold     = 5.00;

    // Crop
    public const int DefaultAutoCropPadding = 4;
    public const int MaxAutoCropPadding     = 64;

    #endregion

    #region Private Fields

    private readonly string _imagePath;
    private readonly IBrush _checkerboardBrush = ImageProcessing.CreateCheckerboardBrush();
    private SKBitmap? _originalBitmap;
    private SKBitmap? _workingBitmap;
    private SKBitmap? _previewBitmap;
    private CancellationTokenSource? _cts;
    private bool _isSaving;
    private bool _isLoaded;
    private bool _updatingDimensions;
    private bool _updatingResizePercent;
    private bool _hasUnsavedKeep;
    private bool _hasPendingCrop;
    private bool _disposed;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomedImageWidth))]
    [NotifyPropertyChangedFor(nameof(ZoomedImageHeight))]
    private Bitmap? _displayImage;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private string _imageSizeText = string.Empty;
    [ObservableProperty] private string _imageTypeText = string.Empty;
    [ObservableProperty] private string _imageFileSizeText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageBackground))]
    private bool _showCheckerboard = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomedImageWidth))]
    [NotifyPropertyChangedFor(nameof(ZoomedImageHeight))]
    [NotifyPropertyChangedFor(nameof(ZoomReadoutText))]
    private double _zoomLevel = DefaultZoom;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemoveBackgroundTool))]
    [NotifyPropertyChangedFor(nameof(IsCropTool))]
    [NotifyPropertyChangedFor(nameof(IsResizeTool))]
    [NotifyPropertyChangedFor(nameof(IsRefineTool))]
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

    // Remove Background
    [ObservableProperty] private double _tolerance = DefaultTolerance;
    [ObservableProperty] private double _edgeThreshold = DefaultEdgeThreshold;
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
    [ObservableProperty] private int _autoCropPadding = DefaultAutoCropPadding;

    // Resize
    [ObservableProperty] private bool   _maintainAspectRatio = true;
    [ObservableProperty] private double _resizePercent = 100;
    [ObservableProperty] private int    _resizeWidth;
    [ObservableProperty] private int    _resizeHeight;

    // Refine
    [ObservableProperty] private double _sharpenAmount    = MinSharpen;
    [ObservableProperty] private double _cleanEdgesAmount = MinCleanEdges;
    [ObservableProperty] private bool   _smoothEdges;
    [ObservableProperty] private bool   _hasAlphaChannel;

    #endregion

    #region Computed Properties

    public bool IsRemoveBackgroundTool => SelectedTool == EditTool.RemoveBackground;
    public bool IsCropTool             => SelectedTool == EditTool.Crop;
    public bool IsResizeTool           => SelectedTool == EditTool.Resize;
    public bool IsRefineTool           => SelectedTool == EditTool.Refine;

    public int     SourceWidth      => _workingBitmap?.Width  ?? 0;
    public int     SourceHeight     => _workingBitmap?.Height ?? 0;
    public IBrush? ImageBackground  => ShowCheckerboard ? _checkerboardBrush : null;

    public double ZoomedImageWidth  => (DisplayImage?.PixelSize.Width  ?? 0) * ZoomLevel;
    public double ZoomedImageHeight => (DisplayImage?.PixelSize.Height ?? 0) * ZoomLevel;
    public string ZoomReadoutText   => $"{(int)Math.Round(ZoomLevel * 100)}%";

    #endregion

    #region Events

    public event Action<string?>? CloseRequested;

    #endregion

    // Design-time constructor
    public ImageEditViewModel() : this(string.Empty)
    {
        SelectedTool = EditTool.Crop;
    }

    public ImageEditViewModel(string imagePath)
    {
        _imagePath = imagePath;
    }

    #region Public Methods

    public async Task LoadAsync()
    {
        if (_disposed) return;

        IsBusy = true;
        var original = await Task.Run(() => SKBitmap.Decode(_imagePath));
        if (_disposed) { original?.Dispose(); return; }
        if (original == null) { CloseRequested?.Invoke(null); return; }

        _originalBitmap = original;
        _workingBitmap = _originalBitmap.Copy();
        _isLoaded = true;
        UpdateSizeText(_workingBitmap);
        UpdateHasAlphaChannel();
        UpdateImageInfo();
        SelectedTool = EditTool.Crop;
        IsBusy = false;
    }

    public void SetCropRectLive(int x, int y, int w, int h)
    {
        CropX = x; CropY = y; CropW = w; CropH = h;
        MarkCropPending();
        StatusText = $"Crop: {w} × {h}  at ({x}, {y})";
    }

    public void ZoomIn()
        => ZoomLevel = Math.Min(MaxZoom, Math.Round(ZoomLevel * ZoomStep, 2));

    public void ZoomOut()
        => ZoomLevel = Math.Max(MinZoom, Math.Round(ZoomLevel / ZoomStep, 2));

    public void ZoomFit(double viewportWidth, double viewportHeight)
    {
        if (DisplayImage == null || viewportWidth <= 0 || viewportHeight <= 0) return;
        int imgW = DisplayImage.PixelSize.Width;
        int imgH = DisplayImage.PixelSize.Height;
        if (imgW <= 0 || imgH <= 0) return;
        ZoomLevel = Math.Clamp(Math.Min(viewportWidth / imgW, viewportHeight / imgH),
                               MinZoom, MaxZoom);
    }

    /// <summary>Called by the view when either refine slider is released.</summary>
    public void CommitRefinePreview()
    {
        if (IsRefineTool) _ = UpdatePreviewAsync();
    }

    /// <summary>Called by the view when the resize slider is released.</summary>
    public void CommitResizePreview()
    {
        if (IsResizeTool) _ = UpdatePreviewAsync();
    }

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        if (SelectedTool == EditTool.Crop)
        {
            ApplyCropNow();
            return;
        }

        if (_previewBitmap == null)
        {
            StatusText = "Preview not ready — please wait.";
            return;
        }

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
        CancelPreviewWork();
        _hasUnsavedKeep = true;
        _hasPendingCrop = false;

        var tool = SelectedTool;
        ShowWorkingBitmap();
        UpdateSizeText(_workingBitmap);
        UpdateHasAlphaChannel();
        UpdateImageInfo();
        UpdateButtonStates();

        StatusText = tool switch
        {
            EditTool.Crop             => $"Crop applied — {_workingBitmap?.Width} × {_workingBitmap?.Height}",
            EditTool.Resize           => $"Resize applied — {_workingBitmap?.Width} × {_workingBitmap?.Height}",
            EditTool.Refine           => "Refine applied.",
            EditTool.RemoveBackground => "Background removed — pick another tool or save.",
            _                         => "Changes applied."
        };

        switch (tool)
        {
            case EditTool.Crop:
                InitializeCropRect();
                break;
            case EditTool.Resize:
                InitializeResizeDimensions();
                break;
        }

        CanCancel = false;
        CanApply  = false;
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        var tool = SelectedTool;

        CancelPreviewWork();
        DisposeDeferred(_previewBitmap);
        _previewBitmap  = null;
        _hasPendingCrop = false;

        ShowWorkingBitmap();
        UpdateSizeText(_workingBitmap);

        switch (tool)
        {
            case EditTool.Crop:
                InitializeCropRect();
                break;
            case EditTool.Resize:
                InitializeResizeDimensions();
                break;
            case EditTool.Refine:
                SharpenAmount    = MinSharpen;
                CleanEdgesAmount = MinCleanEdges;
                SmoothEdges      = false;
                break;
            case EditTool.RemoveBackground:
                SelectedModeIndex = -1;
                Tolerance         = DefaultTolerance;
                EdgeThreshold     = DefaultEdgeThreshold;
                RemoveEnclosed    = false;
                InvertColors      = false;
                break;
        }

        UpdateButtonStates();
        CanApply   = false;
        CanCancel  = false;
        StatusText = "Changes cancelled.";
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (_isSaving) return;
        _isSaving = true;
        CanSave = false;

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
            await Task.Run(() => ImageProcessing.SaveBitmapAsPng(capture, savedPath));
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

        CancelPreviewWork();
        DisposeDeferred(_previewBitmap);
        _previewBitmap = null;

        var old = _workingBitmap;
        _workingBitmap = _originalBitmap.Copy();
        DisposeDeferred(old);

        _hasUnsavedKeep = false;
        _hasPendingCrop = false;

        ShowWorkingBitmap();
        UpdateSizeText(_workingBitmap);
        UpdateHasAlphaChannel();
        UpdateImageInfo();
        switch (SelectedTool)
        {
            case EditTool.Crop:
                InitializeCropRect();
                break;
            case EditTool.Resize:
                InitializeResizeDimensions();
                break;
        }
        UpdateButtonStates();
        CanApply  = false;
        CanCancel = false;
        StatusText = "Reverted to original.";
    }

    [RelayCommand]
    private unsafe void AutoCrop()
    {
        if (_workingBitmap == null) return;

        int w = _workingBitmap.Width;
        int h = _workingBitmap.Height;
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

        CropX = nx; CropY = ny; CropW = nw; CropH = nh;
        MarkCropPending();
        StatusText = $"Auto-crop: {nw} × {nh} — click Apply to confirm";
    }

    #endregion

    #region Preview

    private async Task UpdatePreviewAsync()
    {
        if (_disposed) return;
        if (!_isLoaded || _workingBitmap == null) return;
        if (SelectedTool == EditTool.None || SelectedTool == EditTool.Crop) return;

        CancelPreviewWork();
        var cts = new CancellationTokenSource();
        _cts = cts;

        IsBusy   = true;
        CanApply = false;

        var source        = _workingBitmap.Copy();
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
        var removeEnclosed = RemoveEnclosed;
        var invert        = InvertColors;
        var resizeW          = ResizeWidth;
        var resizeH          = ResizeHeight;
        var sharpenAmount    = (float)SharpenAmount;
        var cleanEdgesAmount = (float)CleanEdgesAmount;
        var smoothEdges      = SmoothEdges;

        if (tool == EditTool.Refine && sharpenAmount <= 0.001f && cleanEdgesAmount <= 0.001f && !smoothEdges)
        {
            DisposeDeferred(_previewBitmap);
            _previewBitmap = null;
            ShowWorkingBitmap();
            UpdateSizeText(_workingBitmap);
            StatusText = "Refine: move a slider or check Smooth Edges to preview";
            IsBusy = false;
            UpdateButtonStates();
            CanApply  = false;
            CanCancel = false;
            return;
        }

        if (tool == EditTool.RemoveBackground && modeIndex < 0)
        {
            StatusText = "Select a shape to preview";
            IsBusy     = false;
            CanApply   = false;
            return;
        }

        if (tool == EditTool.Resize && (resizeW < 1 || resizeH < 1))
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
                return tool switch
                {
                    EditTool.RemoveBackground => ProcessRemoveBackground(
                        source, tolerance, bgMode, removeEnclosed, edgeThreshold, invert),
                    EditTool.Resize => (
                        ImageProcessing.ResizeBitmap(source, resizeW, resizeH),
                        $"Resize: {resizeW} × {resizeH}"),
                    EditTool.Refine =>
                        ImageProcessing.ApplyRefine(source, sharpenAmount, cleanEdgesAmount, smoothEdges),
                    _ => ((SKBitmap?)null, string.Empty)
                };
            }, cts.Token);
        }
        catch (OperationCanceledException) { result?.Dispose(); return; }
        finally
        {
            source.Dispose();
            if (ReferenceEquals(_cts, cts))
                _cts = null;
            cts.Dispose();
        }

        if (_disposed || cts.IsCancellationRequested) { result?.Dispose(); return; }

        var old = _previewBitmap;
        _previewBitmap = result;

        if (result != null)
        {
            var avBitmap   = ImageProcessing.ToAvaloniaBitmap(result);
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

    private static (SKBitmap? result, string strategy) ProcessRemoveBackground(
        SKBitmap source, int tolerance, BackgroundRemovalMode bgMode,
        bool removeEnclosed, float edgeThreshold, bool invert)
    {
        var bgColor = ImageProcessing.DetectBackgroundColor(source);
        var (r, s)  = ImageProcessing.RemoveBackground(
            source, bgColor, tolerance, bgMode, removeEnclosed, edgeThreshold);
        if (invert) { var inv = ImageProcessing.InvertColors(r); r.Dispose(); r = inv; }
        return (r, s);
    }

    #endregion

    #region Property Change Callbacks

    partial void OnSelectedToolChanged(EditTool value)
    {
        CancelPreviewWork();
        DisposeDeferred(_previewBitmap);
        _previewBitmap  = null;
        _hasPendingCrop = false;

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
                break;

            case EditTool.Refine:
                SharpenAmount    = 0;
                CleanEdgesAmount = 0;
                SmoothEdges      = false;
                ShowWorkingBitmap();
                UpdateSizeText(_workingBitmap);
                UpdateButtonStates();
                break;

            case EditTool.RemoveBackground:
                SelectedModeIndex = -1;
                Tolerance         = 20;
                EdgeThreshold     = 0.15;
                RemoveEnclosed    = false;
                InvertColors      = false;
                ShowWorkingBitmap();
                UpdateButtonStates();
                break;

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
            double ratio = (double)SourceHeight / SourceWidth;
            int newH = (int)Math.Round(value * ratio);
            int newW = value;
            if (newH > MaxResizePx)
            {
                newH = MaxResizePx;
                newW = Math.Max(MinResizePx, (int)Math.Round(newH / ratio));
                ResizeWidth = newW;
            }
            ResizeHeight = Math.Max(MinResizePx, newH);
            UpdateResizePercentFromWidth(newW);
            _updatingDimensions = false;
        }
        else
        {
            UpdateResizePercentFromWidth(value);
        }
    }

    partial void OnResizeHeightChanged(int value)
    {
        if (_updatingDimensions || !IsResizeTool) return;
        if (MaintainAspectRatio && SourceWidth > 0 && SourceHeight > 0)
        {
            _updatingDimensions = true;
            double ratio = (double)SourceWidth / SourceHeight;
            int newW = (int)Math.Round(value * ratio);
            int newH = value;
            if (newW > MaxResizePx)
            {
                newW = MaxResizePx;
                newH = Math.Max(MinResizePx, (int)Math.Round(newW / ratio));
                ResizeHeight = newH;
            }
            ResizeWidth = Math.Max(MinResizePx, newW);
            UpdateResizePercentFromWidth(newW);
            _updatingDimensions = false;
        }
    }

    partial void OnResizePercentChanged(double value)
    {
        if (_updatingDimensions || _updatingResizePercent || !IsResizeTool || SourceWidth <= 0 || SourceHeight <= 0) return;

        double scale = Math.Clamp(value, MinResizePct, MaxResizePct) / 100.0;
        int newW = (int)Math.Round(SourceWidth  * scale);
        int newH = (int)Math.Round(SourceHeight * scale);

        if (newW > MaxResizePx || newH > MaxResizePx)
        {
            double clampScale = Math.Min((double)MaxResizePx / SourceWidth,
                                         (double)MaxResizePx / SourceHeight);
            newW = Math.Max(MinResizePx, (int)Math.Round(SourceWidth  * clampScale));
            newH = Math.Max(MinResizePx, (int)Math.Round(SourceHeight * clampScale));
        }

        _updatingDimensions = true;
        ResizeWidth  = newW;
        ResizeHeight = newH;
        _updatingDimensions = false;
    }

    partial void OnMaintainAspectRatioChanged(bool value)
    {
        if (!value || !IsResizeTool || SourceWidth <= 0 || SourceHeight <= 0) return;
        _updatingDimensions = true;
        ResizeHeight = Math.Max(MinResizePx, Math.Min(MaxResizePx,
            (int)Math.Round(ResizeWidth * (double)SourceHeight / SourceWidth)));
        _updatingDimensions = false;
        _ = UpdatePreviewAsync();
    }

    partial void OnSharpenAmountChanged(double value)
    {
        if (!IsRefineTool) return;

        CancelPreviewWork();
        DisposeDeferred(_previewBitmap);
        _previewBitmap = null;
        ShowWorkingBitmap();
        UpdateSizeText(_workingBitmap);
        StatusText = $"Sharpness: {value:0}%";
        UpdateButtonStates();
        CanApply  = false;
        CanCancel = false;
    }

    partial void OnCleanEdgesAmountChanged(double value)
    {
        if (!IsRefineTool) return;

        CancelPreviewWork();
        DisposeDeferred(_previewBitmap);
        _previewBitmap = null;
        ShowWorkingBitmap();
        UpdateSizeText(_workingBitmap);
        StatusText = $"Clean edges: {value:0.#}";
        UpdateButtonStates();
        CanApply  = false;
        CanCancel = false;
    }

    partial void OnSmoothEdgesChanged(bool value)
    {
        if (IsRefineTool) _ = UpdatePreviewAsync();
    }

    #endregion

    #region Private Helpers

    private void ShowWorkingBitmap()
    {
        if (_workingBitmap == null) return;
        var avBitmap   = ImageProcessing.ToAvaloniaBitmap(_workingBitmap);
        var oldDisplay = DisplayImage;
        DisplayImage   = avBitmap;
        DisposeDeferred(oldDisplay);
    }

    private void UpdateSizeText(SKBitmap? bmp)
        => ImageSizeText = bmp != null ? $"{bmp.Width} × {bmp.Height}" : string.Empty;

    private void UpdateButtonStates()
    {
        CanApply  = SelectedTool == EditTool.Crop
            ? _hasPendingCrop
            : SelectedTool != EditTool.None && _previewBitmap != null;
        CanCancel = _hasPendingCrop || _previewBitmap != null;
        CanSave   = _hasUnsavedKeep || _previewBitmap != null;
        CanReload = _hasUnsavedKeep || _previewBitmap != null;
    }

    private void MarkCropPending()
    {
        _hasPendingCrop = true;
        UpdateButtonStates();
    }

    private void InitializeCropRect()
    {
        if (_workingBitmap == null) return;
        CropX = 0; CropY = 0;
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
            w = Math.Max(1, (int)Math.Round(w * scale));
            h = Math.Max(1, (int)Math.Round(h * scale));
        }
        _updatingDimensions = true;
        ResizeWidth  = w;
        ResizeHeight = h;
        UpdateResizePercentFromWidth(w);
        _updatingDimensions = false;
    }

    private void UpdateResizePercentFromWidth(int width)
    {
        if (SourceWidth <= 0) return;
        _updatingResizePercent = true;
        ResizePercent = Math.Max(1, Math.Min(400, Math.Round(width * 100.0 / SourceWidth)));
        _updatingResizePercent = false;
    }

    private static void DisposeDeferred(IDisposable? disposable)
    {
        if (disposable == null) return;
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () => disposable.Dispose(),
            Avalonia.Threading.DispatcherPriority.Background);
    }

    private void UpdateImageInfo()
    {
        ImageTypeText = string.IsNullOrEmpty(_imagePath)
            ? string.Empty
            : Path.GetExtension(_imagePath).TrimStart('.').ToUpperInvariant();

        if (_workingBitmap == null)
        {
            try
            {
                var info = new System.IO.FileInfo(_imagePath);
                if (info.Exists) ImageFileSizeText = FormatFileSize(info.Length);
            }
            catch { ImageFileSizeText = string.Empty; }
            return;
        }

        // Encode to PNG in the background — accurate size without blocking the UI
        var snapshot = _workingBitmap.Copy();
        _ = Task.Run(() =>
        {
            try
            {
                long size;
                using (snapshot)
                using (var img  = SKImage.FromBitmap(snapshot))
                using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
                    size = data.Size;

                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () => ImageFileSizeText = FormatFileSize(size),
                    Avalonia.Threading.DispatcherPriority.Background);
            }
            catch { }
        });
    }

    private static string FormatFileSize(long bytes) =>
        bytes < 1024           ? $"{bytes} B"
        : bytes < 1024 * 1024  ? $"{bytes / 1024.0:0.#} KB"
                               : $"{bytes / (1024.0 * 1024.0):0.##} MB";

    private unsafe void UpdateHasAlphaChannel()
    {
        if (_workingBitmap == null || _workingBitmap.AlphaType == SKAlphaType.Opaque)
        {
            HasAlphaChannel = false;
            return;
        }
        byte* p  = (byte*)_workingBitmap.GetPixels();
        int   rb = _workingBitmap.RowBytes;
        int   w  = _workingBitmap.Width;
        int   h  = _workingBitmap.Height;
        for (int y = 0; y < h; y++)
        {
            byte* row = p + y * rb;
            for (int x = 0; x < w; x++)
                if (row[x * 4 + 3] < 255) { HasAlphaChannel = true; return; }
        }
        HasAlphaChannel = false;
    }

    private void CancelPreviewWork()
    {
        var cts = _cts;
        _cts = null;
        if (cts == null) return;
        cts.Cancel();
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CancelPreviewWork();

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
