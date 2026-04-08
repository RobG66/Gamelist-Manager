using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using SkiaSharp;

namespace Gamelist_Manager.ViewModels;

public partial class ImageEditViewModel : ViewModelBase, IDisposable
{
    #region Observable Properties
    [ObservableProperty] private Bitmap? _originalImage;
    [ObservableProperty] private Bitmap? _previewImage;
    [ObservableProperty] private bool _isBusy = true;
    [ObservableProperty] private string _statusText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(KeepCommand))]
    private bool _canKeep;

    // Remove Background options
    [ObservableProperty] private bool _removeBackground;
    [ObservableProperty] private double _tolerance = 20;
    [ObservableProperty] private int _selectedModeIndex = -1;
    [ObservableProperty] private bool _removeEnclosed;
    [ObservableProperty] private bool _invertColors;
    [ObservableProperty] private bool _isFreeformMode;

    // Crop options
    [ObservableProperty] private bool _cropImage;
    [ObservableProperty] private double _padding;
    [ObservableProperty] private bool _cropVertical = true;
    [ObservableProperty] private bool _cropHorizontal = true;
    [ObservableProperty] private bool _canCropVertical;
    [ObservableProperty] private bool _canCropHorizontal;

    // Resize options
    [ObservableProperty] private bool _resizeImage;
    [ObservableProperty] private bool _maintainAspectRatio = true;
    [ObservableProperty] private int _resizeWidth;
    [ObservableProperty] private int _resizeHeight;

    // Size labels
    [ObservableProperty] private string _originalSizeText = string.Empty;
    [ObservableProperty] private string _previewSizeText = string.Empty;
    #endregion

    #region Private Fields
    private readonly string _imagePath;
    private SKBitmap? _originalBitmap;
    private SKBitmap? _currentResult;
    private CancellationTokenSource? _cts;
    private bool _isSaving;
    private bool _isLoaded;
    private int _sourceWidth;
    private int _sourceHeight;
    private bool _updatingDimensions;
    #endregion

    public event Action<string?>? CloseRequested;

    public ImageEditViewModel(string imagePath)
    {
        _imagePath = imagePath;
    }

    public string WindowTitle => "Edit Image";

    public async Task LoadAsync()
    {
        _originalBitmap = await Task.Run(() => SKBitmap.Decode(_imagePath));
        if (_originalBitmap == null)
        {
            CloseRequested?.Invoke(null);
            return;
        }

        OriginalImage = ImageHelper.ToAvaloniaBitmap(_originalBitmap);

        _sourceWidth = _originalBitmap.Width;
        _sourceHeight = _originalBitmap.Height;
        OriginalSizeText = $"{_sourceWidth} × {_sourceHeight}";

        _isLoaded = true;
        await UpdatePreviewAsync();
    }

    #region Commands
    [RelayCommand(CanExecute = nameof(CanKeep))]
    private async Task KeepAsync()
    {
        if (_isSaving) return;
        _isSaving = true;
        CanKeep = false;

        // Always save as PNG so transparency is preserved
        var savedPath = Path.ChangeExtension(_imagePath, ".png");
        try
        {
            var result = _currentResult;
            if (result != null && !string.IsNullOrEmpty(savedPath))
                await Task.Run(() => ImageHelper.SaveBitmapAsPng(result, savedPath));
        }
        finally
        {
            CloseRequested?.Invoke(savedPath);
        }
    }

    [RelayCommand]
    private void Discard()
    {
        if (_isSaving) return;
        CloseRequested?.Invoke(null);
    }
    #endregion

    #region Private Methods
    private async Task UpdatePreviewAsync()
    {
        if (!_isLoaded || _originalBitmap == null) return;

        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;

        IsBusy = true;
        CanKeep = false;

        var source = _originalBitmap;
        var doBgRemove = RemoveBackground;
        var modeIndex = SelectedModeIndex;
        var bgMode = modeIndex switch
        {
            0 => BackgroundRemovalMode.Circle,
            2 => BackgroundRemovalMode.ConvexHull,
            _ => BackgroundRemovalMode.Freeform
        };
        var tolerance = (int)Tolerance;
        var removeEnclosed = RemoveEnclosed;
        var invert = InvertColors;
        var doCrop = CropImage;
        var paddingPercent = (int)Padding;
        var cropV = CropVertical;
        var cropH = CropHorizontal;
        var doResize = ResizeImage;
        var resizeW = ResizeWidth;
        var resizeH = ResizeHeight;

        if (doBgRemove && modeIndex < 0)
        {
            StatusText = "Select a shape to preview";
            IsBusy = false;
            return;
        }

        SKBitmap? result = null;
        string strategy = string.Empty;
        try
        {
            (result, strategy) = await Task.Run(() =>
            {
                SKBitmap r = source;
                bool owned = false;
                string s = string.Empty;

                if (doBgRemove)
                {
                    var bgColor = ImageHelper.DetectBackgroundColor(r);
                    var (bgResult, bgStrategy) = ImageHelper.RemoveBackground(r, bgColor, tolerance, bgMode, removeEnclosed);
                    if (owned) r.Dispose();
                    r = bgResult;
                    owned = true;
                    s = bgStrategy;

                    if (invert)
                    {
                        var inv = ImageHelper.InvertColors(r);
                        r.Dispose();
                        r = inv;
                    }
                }

                if (doCrop)
                {
                    var cropped = ImageHelper.AutoCrop(r, paddingPercent, cropV, cropH);
                    if (cropped != null)
                    {
                        if (owned) r.Dispose();
                        r = cropped;
                        owned = true;
                    }
                }

                if (doResize && resizeW >= 100 && resizeH >= 100)
                {
                    var resized = ImageHelper.ResizeBitmap(r, resizeW, resizeH);
                    if (owned) r.Dispose();
                    r = resized;
                    owned = true;
                }

                return (owned ? r : (SKBitmap?)null, s);
            }, cts.Token);
        }
        catch (OperationCanceledException)
        {
            result?.Dispose();
            return;
        }

        if (cts.IsCancellationRequested)
        {
            result?.Dispose();
            return;
        }

        if (doCrop)
        {
            var probeV = ImageHelper.AutoCrop(source, 0, cropVertical: true, cropHorizontal: false);
            var probeH = ImageHelper.AutoCrop(source, 0, cropVertical: false, cropHorizontal: true);
            CanCropVertical = probeV != null && probeV.Height != source.Height;
            CanCropHorizontal = probeH != null && probeH.Width != source.Width;
            probeV?.Dispose();
            probeH?.Dispose();
        }

        var displayBitmap = result ?? source;
        var oldResult = _currentResult;
        var oldPreview = PreviewImage;
        _currentResult = result;
        PreviewImage = ImageHelper.ToAvaloniaBitmap(displayBitmap);
        oldPreview?.Dispose();
        oldResult?.Dispose();

        PreviewSizeText = $"{displayBitmap.Width} × {displayBitmap.Height}";
        StatusText = strategy;
        IsBusy = false;
        CanKeep = result != null && !_isSaving;
    }

    #endregion

    #region Property Change Callbacks
    partial void OnRemoveBackgroundChanged(bool value)
    {
        _ = UpdatePreviewAsync();
    }

    partial void OnToleranceChanged(double value)
    {
        if (RemoveBackground) _ = UpdatePreviewAsync();
    }

    partial void OnSelectedModeIndexChanged(int value)
    {
        IsFreeformMode = value == 1;
        if (RemoveBackground) _ = UpdatePreviewAsync();
    }

    partial void OnRemoveEnclosedChanged(bool value)
    {
        if (RemoveBackground) _ = UpdatePreviewAsync();
    }

    partial void OnInvertColorsChanged(bool value)
    {
        if (RemoveBackground) _ = UpdatePreviewAsync();
    }

    partial void OnCropImageChanged(bool value) => _ = UpdatePreviewAsync();

    partial void OnCropVerticalChanged(bool value)
    {
        if (CropImage) _ = UpdatePreviewAsync();
    }

    partial void OnCropHorizontalChanged(bool value)
    {
        if (CropImage) _ = UpdatePreviewAsync();
    }

    partial void OnPaddingChanged(double value)
    {
        if (CropImage) _ = UpdatePreviewAsync();
    }

    partial void OnResizeImageChanged(bool value)
    {
        if (value)
        {
            const int max = 2000;
            int w = _sourceWidth, h = _sourceHeight;
            if (w > max || h > max)
            {
                double scale = Math.Min((double)max / w, (double)max / h);
                w = Math.Max(100, (int)Math.Round(w * scale));
                h = Math.Max(100, (int)Math.Round(h * scale));
            }
            _updatingDimensions = true;
            ResizeWidth = w;
            ResizeHeight = h;
            _updatingDimensions = false;
        }
        _ = UpdatePreviewAsync();
    }

    partial void OnResizeWidthChanged(int value)
    {
        if (_updatingDimensions || !ResizeImage) return;
        if (MaintainAspectRatio && _sourceWidth > 0 && _sourceHeight > 0)
        {
            _updatingDimensions = true;
            ResizeHeight = Math.Max(100, Math.Min(2000, (int)Math.Round(value * (double)_sourceHeight / _sourceWidth)));
            _updatingDimensions = false;
        }
        _ = UpdatePreviewAsync();
    }

    partial void OnResizeHeightChanged(int value)
    {
        if (_updatingDimensions || !ResizeImage) return;
        if (MaintainAspectRatio && _sourceWidth > 0 && _sourceHeight > 0)
        {
            _updatingDimensions = true;
            ResizeWidth = Math.Max(100, Math.Min(2000, (int)Math.Round(value * (double)_sourceWidth / _sourceHeight)));
            _updatingDimensions = false;
        }
        _ = UpdatePreviewAsync();
    }

    partial void OnMaintainAspectRatioChanged(bool value)
    {
        if (value && ResizeImage && _sourceWidth > 0 && _sourceHeight > 0)
        {
            _updatingDimensions = true;
            ResizeHeight = Math.Max(100, Math.Min(2000, (int)Math.Round(ResizeWidth * (double)_sourceHeight / _sourceWidth)));
            _updatingDimensions = false;
            _ = UpdatePreviewAsync();
        }
    }
    #endregion

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        PreviewImage?.Dispose();
        _currentResult?.Dispose();
        _originalBitmap?.Dispose();
        OriginalImage?.Dispose();
    }
}
