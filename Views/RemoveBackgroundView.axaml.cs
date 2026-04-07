using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Gamelist_Manager.Classes.Helpers;
using SkiaSharp;

namespace Gamelist_Manager.Views;

public partial class RemoveBackgroundView : Window
{
    #region Private Fields
    private string _imagePath = string.Empty;
    private SKBitmap? _originalBitmap;
    private SKBitmap? _currentResult;
    private Bitmap? _previewAvaBitmap;
    private SKColor _backgroundColor;
    private CancellationTokenSource? _cts;
    private bool _isSaving;
    #endregion

    #region Constructor
    public RemoveBackgroundView()
    {
        InitializeComponent();
    }

    public RemoveBackgroundView(string imagePath)
    {
        InitializeComponent();
        _imagePath = imagePath;
        var checkerboard = CreateCheckerboardBrush();
        OriginalImageBorder.Background = checkerboard;
        PreviewImageBorder.Background = CreateCheckerboardBrush();
        ToleranceSlider.ValueChanged += ToleranceSlider_ValueChanged;
        ModeComboBox.SelectionChanged += ModeComboBox_SelectionChanged;
        RemoveEnclosedCheckBox.IsCheckedChanged += RemoveEnclosedCheckBox_IsCheckedChanged;
        InvertCheckBox.IsCheckedChanged += InvertCheckBox_IsCheckedChanged;
        UpdateCheckBoxState();
        _ = LoadAsync();
    }
    #endregion

    #region Public Methods
    public static async Task<string?> ShowAsync(string imagePath, Window? owner = null)
    {
        owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return null;
        return await new RemoveBackgroundView(imagePath).ShowDialog<string?>(owner);
    }
    #endregion

    #region Private Methods
    private async Task LoadAsync()
    {
        _originalBitmap = await Task.Run(() => SKBitmap.Decode(_imagePath));
        if (_originalBitmap == null)
        {
            Close(null);
            return;
        }

        OriginalImage.Source = ToAvaloniaBitmap(_originalBitmap);
        _backgroundColor = ImageHelper.DetectBackgroundColor(_originalBitmap);
        await UpdatePreviewAsync((int)ToleranceSlider.Value);
    }

    private async Task UpdatePreviewAsync(int tolerance)
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;

        if (_originalBitmap == null) return;

        SetBusy(true);

        var source = _originalBitmap;
        var bg = _backgroundColor;
        var mode = GetSelectedMode();
        var removeEnclosed = RemoveEnclosedCheckBox.IsChecked == true;
        var invert = InvertCheckBox.IsChecked == true;

        SKBitmap? result = null;
        string strategy = string.Empty;
        try
        {
            (result, strategy) = await Task.Run(() =>
            {
                var (r, s) = ImageHelper.RemoveBackground(source, bg, tolerance, mode, removeEnclosed);
                if (invert && r != null)
                {
                    var inverted = ImageHelper.InvertColors(r);
                    r.Dispose();
                    return (inverted, s);
                }
                return (r, s);
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

        var oldResult = _currentResult;
        _currentResult = result;

        var oldPreview = _previewAvaBitmap;
        _previewAvaBitmap = result != null ? ToAvaloniaBitmap(result) : null;
        PreviewImage.Source = _previewAvaBitmap;
        oldPreview?.Dispose();
        oldResult?.Dispose();

        StrategyText.Text = strategy;
        SetBusy(false);
    }

    private void SetBusy(bool busy)
    {
        BusyOverlay.IsVisible = busy;
        KeepButton.IsEnabled = !busy;
    }

    private static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());
        return new Bitmap(ms);
    }

    private static void SaveResult(SKBitmap bitmap, string path)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var temp = path + ".tmp";
        using (var fs = File.OpenWrite(temp))
            data.SaveTo(fs);
        File.Move(temp, path, overwrite: true);
    }

    private void UpdateCheckBoxState()
    {
        bool isFreeform = ModeComboBox.SelectedIndex == 2;
        RemoveEnclosedCheckBox.IsEnabled = isFreeform;
        InvertCheckBox.IsEnabled = isFreeform;
    }

    private static ImageBrush CreateCheckerboardBrush()
    {
        const int tileSize = 8;
        const int bitmapSize = tileSize * 2;

        var bmp = new WriteableBitmap(
            new PixelSize(bitmapSize, bitmapSize),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using var fb = bmp.Lock();
        for (int y = 0; y < bitmapSize; y++)
            for (int x = 0; x < bitmapSize; x++)
            {
                bool isLight = (x < tileSize) == (y < tileSize);
                int argb = isLight ? unchecked((int)0xFFFFFFFF) : unchecked((int)0xFFCCCCCC);
                Marshal.WriteInt32(fb.Address + y * fb.RowBytes + x * 4, argb);
            }

        return new ImageBrush(bmp)
        {
            TileMode = TileMode.Tile,
            DestinationRect = new RelativeRect(0, 0, bitmapSize, bitmapSize, RelativeUnit.Absolute)
        };
    }
    #endregion

    #region Event Handlers
    private async void ToleranceSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        var tolerance = (int)e.NewValue;
        ToleranceValueText.Text = tolerance.ToString();
        await UpdatePreviewAsync(tolerance);
    }

    private async void ModeComboBox_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        UpdateCheckBoxState();
        await UpdatePreviewAsync((int)ToleranceSlider.Value);
    }

    private async void RemoveEnclosedCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        await UpdatePreviewAsync((int)ToleranceSlider.Value);
    }

    private async void InvertCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        await UpdatePreviewAsync((int)ToleranceSlider.Value);
    }

    private BackgroundRemovalMode GetSelectedMode() => ModeComboBox.SelectedIndex switch
    {
        1 => BackgroundRemovalMode.Circle,
        2 => BackgroundRemovalMode.Freeform,
        3 => BackgroundRemovalMode.ConvexHull,
        _ => BackgroundRemovalMode.Automatic
    };

    private async void KeepButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isSaving) return;
        _isSaving = true;
        KeepButton.IsEnabled = false;
        DiscardButton.IsEnabled = false;

        // Always save as PNG so transparency is preserved, even if the source was JPEG.
        var savedPath = Path.ChangeExtension(_imagePath, ".png");
        try
        {
            var result = _currentResult;
            if (result != null && !string.IsNullOrEmpty(savedPath))
                await Task.Run(() => SaveResult(result, savedPath));
        }
        finally
        {
            Close(savedPath);
        }
    }

    private void DiscardButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
    #endregion

    #region Cleanup
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _cts?.Cancel();
        _cts?.Dispose();
        _previewAvaBitmap?.Dispose();
        _currentResult?.Dispose();
        _originalBitmap?.Dispose();
    }
    #endregion
}
