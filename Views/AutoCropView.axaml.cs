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

public partial class AutoCropView : Window
{
    #region Private Fields
    private string _imagePath = string.Empty;
    private SKBitmap? _originalBitmap;
    private SKBitmap? _croppedBitmap;
    private Bitmap? _previewAvaBitmap;
    private CancellationTokenSource? _cts;
    private bool _isSaving;
    private bool _hasCroppableContent;
    #endregion

    #region Constructor
    public AutoCropView()
    {
        InitializeComponent();
    }

    public AutoCropView(string imagePath)
    {
        InitializeComponent();
        _imagePath = imagePath;
        var checkerboard = CreateCheckerboardBrush();
        OriginalImageBorder.Background = checkerboard;
        PreviewImageBorder.Background = checkerboard;
        PaddingSlider.ValueChanged += PaddingSlider_ValueChanged;
        MaintainAspectRatioCheckBox.IsCheckedChanged += MaintainAspectRatioCheckBox_CheckedChanged;
        _ = LoadAsync();
    }
    #endregion

    #region Public Methods
    public static async Task<string?> ShowAsync(string imagePath, Window? owner = null)
    {
        owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return null;
        return await new AutoCropView(imagePath).ShowDialog<string?>(owner);
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
        await UpdatePreviewAsync(0);
    }

    private async Task UpdatePreviewAsync(int paddingPercent)
    {
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;

        if (_originalBitmap == null) return;

        var source = _originalBitmap;
        bool cropAllSides = MaintainAspectRatioCheckBox.IsChecked == true;
        SKBitmap? cropped;
        try
        {
            cropped = await Task.Run(() => ImageHelper.AutoCrop(source, paddingPercent, cropAllSides), cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested)
        {
            cropped?.Dispose();
            return;
        }

        var oldCropped = _croppedBitmap;
        var oldPreview = _previewAvaBitmap;

        if (cropped == null)
        {
            _croppedBitmap = null;
            _previewAvaBitmap = ToAvaloniaBitmap(source);
            PreviewImage.Source = _previewAvaBitmap;
            DimensionText.Text = $"{source.Width} × {source.Height} — nothing to crop";
            KeepButton.IsEnabled = false;
            _hasCroppableContent = false;
        }
        else if (cropped.Width == source.Width && cropped.Height == source.Height)
        {
            _croppedBitmap = null;
            cropped.Dispose();
            _previewAvaBitmap = ToAvaloniaBitmap(source);
            PreviewImage.Source = _previewAvaBitmap;
            DimensionText.Text = $"{source.Width} × {source.Height} — cannot be cropped further";
            KeepButton.IsEnabled = false;
            _hasCroppableContent = true;
        }
        else
        {
            _croppedBitmap = cropped;
            _previewAvaBitmap = ToAvaloniaBitmap(cropped);
            PreviewImage.Source = _previewAvaBitmap;
            DimensionText.Text = $"{source.Width} × {source.Height}  →  {cropped.Width} × {cropped.Height}";
            KeepButton.IsEnabled = true;
            _hasCroppableContent = true;
        }

        oldPreview?.Dispose();
        oldCropped?.Dispose();
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
    private async void PaddingSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        var padding = (int)e.NewValue;
        PaddingValueText.Text = $"{padding}%";
        if (_hasCroppableContent || padding == 0)
            await UpdatePreviewAsync(padding);
    }

    private async void MaintainAspectRatioCheckBox_CheckedChanged(object? sender, RoutedEventArgs e)
    {
        await UpdatePreviewAsync((int)PaddingSlider.Value);
    }

    private async void KeepButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isSaving) return;
        _isSaving = true;
        KeepButton.IsEnabled = false;
        DiscardButton.IsEnabled = false;

        var savedPath = Path.ChangeExtension(_imagePath, ".png");
        try
        {
            var result = _croppedBitmap;
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
        _croppedBitmap?.Dispose();
        _originalBitmap?.Dispose();
    }
    #endregion
}
