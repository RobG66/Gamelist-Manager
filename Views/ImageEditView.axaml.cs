using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.ViewModels;
using System;
using System.Threading.Tasks;

namespace Gamelist_Manager.Views;

public partial class ImageEditView : Window
{
    // -------------------------------------------------------------------------
    // Crop drag state
    // -------------------------------------------------------------------------

    private enum DragHandle { None, TL, TC, TR, ML, MR, BL, BC, BR, Move }

    private DragHandle _activeDrag = DragHandle.None;
    private Point      _dragStart;
    private int        _dragStartX, _dragStartY, _dragStartW, _dragStartH;

    // Transform: canvas pixels → image pixels
    // Recomputed every time the overlay is redrawn.
    private double _scaleCanvasToImage;
    private double _imageOffsetX, _imageOffsetY;

    private const int    MinCropSize  = 8;
    private const double HandleRadius = 5.0;

    // -------------------------------------------------------------------------
    // Constructors
    // -------------------------------------------------------------------------

    public ImageEditView()
    {
        InitializeComponent();
    }

    public ImageEditView(string imagePath)
    {
        InitializeComponent();

        var viewModel = new ImageEditViewModel(imagePath);
        viewModel.CloseRequested += result => Close(result);
        DataContext = viewModel;

        ImageBorder.Background = ImageHelper.CreateCheckerboardBrush();

        // Crop overlay interactions
        CropOverlayCanvas.PointerPressed  += OnOverlayPointerPressed;
        CropOverlayCanvas.PointerMoved    += OnOverlayPointerMoved;
        CropOverlayCanvas.PointerReleased += OnOverlayPointerReleased;
        CropOverlayCanvas.SizeChanged     += (_, _) => UpdateOverlay();

        // Redraw overlay whenever crop rect, tool, or image changes
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ImageEditViewModel.CropX)
                               or nameof(ImageEditViewModel.CropY)
                               or nameof(ImageEditViewModel.CropW)
                               or nameof(ImageEditViewModel.CropH)
                               or nameof(ImageEditViewModel.IsCropTool)
                               or nameof(ImageEditViewModel.DisplayImage))
            {
                UpdateOverlay();
            }

            // Keep tool buttons in sync with ViewModel.SelectedTool
            if (e.PropertyName == nameof(ImageEditViewModel.SelectedTool))
                SyncToolButtons(viewModel.SelectedTool);
        };

        // Initialise toolbar selection state
        SyncToolButtons(EditTool.None);

        _ = viewModel.LoadAsync();
    }

    public static async Task<string?> ShowAsync(string imagePath, Window? owner = null)
    {
        owner ??= (Avalonia.Application.Current?.ApplicationLifetime
                   as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner == null) return null;
        return await new ImageEditView(imagePath).ShowDialog<string?>(owner);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        (DataContext as IDisposable)?.Dispose();
    }

    // -------------------------------------------------------------------------
    // Tool toolbar — toggle buttons wired in code-behind
    // -------------------------------------------------------------------------

    private void OnToolButtonClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ImageEditViewModel vm) return;
        if (sender is not Button btn) return;

        vm.SelectedTool = btn.Tag?.ToString() switch
        {
            "RemoveBackground" => EditTool.RemoveBackground,
            "Crop"             => EditTool.Crop,
            "Resize"           => EditTool.Resize,
            _                  => EditTool.None
        };
    }

    /// <summary>Swaps each toolbar button between rounded grey (inactive) and rounded blue (active).</summary>
    private void SyncToolButtons(EditTool active)
    {
        SetActive(BtnRemoveBg, active == EditTool.RemoveBackground);
        SetActive(BtnCrop,     active == EditTool.Crop);
        SetActive(BtnResize,   active == EditTool.Resize);

        static void SetActive(Button b, bool on)
        {
            if (on)
            {
                b.Classes.Remove("grey");
                if (!b.Classes.Contains("blue")) b.Classes.Add("blue");
            }
            else
            {
                b.Classes.Remove("blue");
                if (!b.Classes.Contains("grey")) b.Classes.Add("grey");
            }
        }
    }

    // -------------------------------------------------------------------------
    // Reset crop button
    // -------------------------------------------------------------------------

    private void OnResetCropClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ImageEditViewModel vm) return;
        vm.ResetCropToFull();
    }

    // -------------------------------------------------------------------------
    // Overlay drawing
    //
    // The CropOverlayCanvas sits in the same Grid cell as the Viewbox.
    // It fills the whole cell (same size), so we must replicate the Viewbox's
    // Uniform-stretch maths to know exactly where the image pixel (0,0) lands
    // on the canvas and what the pixel-to-canvas scale is.
    // -------------------------------------------------------------------------

    private void UpdateOverlay()
    {
        if (DataContext is not ImageEditViewModel vm) return;
        if (!vm.IsCropTool) return;
        if (vm.DisplayImage == null) return;

        var canvasW = CropOverlayCanvas.Bounds.Width;
        var canvasH = CropOverlayCanvas.Bounds.Height;
        if (canvasW <= 0 || canvasH <= 0) return;

        int srcW = vm.SourceWidth;
        int srcH = vm.SourceHeight;
        if (srcW <= 0 || srcH <= 0) return;

        // Replicate Viewbox Uniform stretch
        double scaleX = canvasW / srcW;
        double scaleY = canvasH / srcH;
        double scale  = Math.Min(scaleX, scaleY);

        double renderedImageW   = srcW * scale;
        double renderedImageH   = srcH * scale;
        _imageOffsetX           = (canvasW - renderedImageW) / 2.0;
        _imageOffsetY           = (canvasH - renderedImageH) / 2.0;
        _scaleCanvasToImage = 1.0 / scale;

        double cx = _imageOffsetX + vm.CropX * scale;
        double cy = _imageOffsetY + vm.CropY * scale;
        double cw = vm.CropW * scale;
        double ch = vm.CropH * scale;

        // Dim rectangles
        SetRect(DimTop,    0,       0,       canvasW, cy);
        SetRect(DimBottom, 0,       cy + ch, canvasW, canvasH - cy - ch);
        SetRect(DimLeft,   0,       cy,      cx,      ch);
        SetRect(DimRight,  cx + cw, cy,      canvasW - cx - cw, ch);

        // Crop border
        Canvas.SetLeft(CropBorderRect, cx);
        Canvas.SetTop(CropBorderRect,  cy);
        CropBorderRect.Width  = Math.Max(0, cw);
        CropBorderRect.Height = Math.Max(0, ch);

        // Rule-of-thirds lines
        SetLine(GridH1, cx,          cy + ch / 3,     cx + cw, cy + ch / 3);
        SetLine(GridH2, cx,          cy + ch * 2 / 3, cx + cw, cy + ch * 2 / 3);
        SetLine(GridV1, cx + cw / 3, cy,              cx + cw / 3, cy + ch);
        SetLine(GridV2, cx + cw * 2 / 3, cy,          cx + cw * 2 / 3, cy + ch);

        // Handles
        PlaceHandle(HandleTL, cx,          cy);
        PlaceHandle(HandleTC, cx + cw / 2, cy);
        PlaceHandle(HandleTR, cx + cw,     cy);
        PlaceHandle(HandleML, cx,          cy + ch / 2);
        PlaceHandle(HandleMR, cx + cw,     cy + ch / 2);
        PlaceHandle(HandleBL, cx,          cy + ch);
        PlaceHandle(HandleBC, cx + cw / 2, cy + ch);
        PlaceHandle(HandleBR, cx + cw,     cy + ch);
    }

    private static void SetRect(Rectangle r, double x, double y, double w, double h)
    {
        Canvas.SetLeft(r, x);
        Canvas.SetTop(r,  y);
        r.Width  = Math.Max(0, w);
        r.Height = Math.Max(0, h);
    }

    private static void SetLine(Line line, double x1, double y1, double x2, double y2)
    {
        line.StartPoint = new Point(x1, y1);
        line.EndPoint   = new Point(x2, y2);
    }

    private static void PlaceHandle(Ellipse e, double cx, double cy)
    {
        Canvas.SetLeft(e, cx - HandleRadius);
        Canvas.SetTop(e,  cy - HandleRadius);
    }

    // -------------------------------------------------------------------------
    // Hit testing
    // -------------------------------------------------------------------------

    private DragHandle HitTest(Point p)
    {
        if (DataContext is not ImageEditViewModel vm) return DragHandle.None;
        if (!vm.IsCropTool) return DragHandle.None;

        double scale = 1.0 / _scaleCanvasToImage;
        double cx = _imageOffsetX + vm.CropX * scale;
        double cy = _imageOffsetY + vm.CropY * scale;
        double cw = vm.CropW * scale;
        double ch = vm.CropH * scale;

        const double hitR = HandleRadius + 5;

        if (HitHandle(p, cx,          cy))          return DragHandle.TL;
        if (HitHandle(p, cx + cw / 2, cy))          return DragHandle.TC;
        if (HitHandle(p, cx + cw,     cy))          return DragHandle.TR;
        if (HitHandle(p, cx,          cy + ch / 2)) return DragHandle.ML;
        if (HitHandle(p, cx + cw,     cy + ch / 2)) return DragHandle.MR;
        if (HitHandle(p, cx,          cy + ch))     return DragHandle.BL;
        if (HitHandle(p, cx + cw / 2, cy + ch))     return DragHandle.BC;
        if (HitHandle(p, cx + cw,     cy + ch))     return DragHandle.BR;

        if (p.X >= cx && p.X <= cx + cw && p.Y >= cy && p.Y <= cy + ch)
            return DragHandle.Move;

        return DragHandle.None;

        bool HitHandle(Point pt, double hx, double hy)
            => (pt.X - hx) * (pt.X - hx) + (pt.Y - hy) * (pt.Y - hy) <= hitR * hitR;
    }

    // -------------------------------------------------------------------------
    // Pointer events
    // -------------------------------------------------------------------------

    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not ImageEditViewModel vm) return;
        if (!e.GetCurrentPoint(CropOverlayCanvas).Properties.IsLeftButtonPressed) return;

        var pos     = e.GetPosition(CropOverlayCanvas);
        _activeDrag = HitTest(pos);
        if (_activeDrag == DragHandle.None) return;

        _dragStart  = pos;
        _dragStartX = vm.CropX;
        _dragStartY = vm.CropY;
        _dragStartW = vm.CropW;
        _dragStartH = vm.CropH;

        e.Pointer.Capture(CropOverlayCanvas);
        e.Handled = true;
    }

    private void OnOverlayPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_activeDrag == DragHandle.None) return;
        if (DataContext is not ImageEditViewModel vm) return;

        var    pos = e.GetPosition(CropOverlayCanvas);
        double dx  = (pos.X - _dragStart.X) * _scaleCanvasToImage;
        double dy  = (pos.Y - _dragStart.Y) * _scaleCanvasToImage;

        int srcW = vm.SourceWidth;
        int srcH = vm.SourceHeight;
        int nx   = _dragStartX;
        int ny   = _dragStartY;
        int nw   = _dragStartW;
        int nh   = _dragStartH;

        switch (_activeDrag)
        {
            case DragHandle.Move:
                nx = Clamp(_dragStartX + (int)dx, 0, srcW - nw);
                ny = Clamp(_dragStartY + (int)dy, 0, srcH - nh);
                break;

            case DragHandle.TL:
                nx = Clamp(_dragStartX + (int)dx, 0, _dragStartX + _dragStartW - MinCropSize);
                ny = Clamp(_dragStartY + (int)dy, 0, _dragStartY + _dragStartH - MinCropSize);
                nw = _dragStartX + _dragStartW - nx;
                nh = _dragStartY + _dragStartH - ny;
                break;

            case DragHandle.TC:
                ny = Clamp(_dragStartY + (int)dy, 0, _dragStartY + _dragStartH - MinCropSize);
                nh = _dragStartY + _dragStartH - ny;
                break;

            case DragHandle.TR:
                ny = Clamp(_dragStartY + (int)dy, 0, _dragStartY + _dragStartH - MinCropSize);
                nh = _dragStartY + _dragStartH - ny;
                nw = Clamp(_dragStartW + (int)dx, MinCropSize, srcW - nx);
                break;

            case DragHandle.ML:
                nx = Clamp(_dragStartX + (int)dx, 0, _dragStartX + _dragStartW - MinCropSize);
                nw = _dragStartX + _dragStartW - nx;
                break;

            case DragHandle.MR:
                nw = Clamp(_dragStartW + (int)dx, MinCropSize, srcW - nx);
                break;

            case DragHandle.BL:
                nx = Clamp(_dragStartX + (int)dx, 0, _dragStartX + _dragStartW - MinCropSize);
                nw = _dragStartX + _dragStartW - nx;
                nh = Clamp(_dragStartH + (int)dy, MinCropSize, srcH - ny);
                break;

            case DragHandle.BC:
                nh = Clamp(_dragStartH + (int)dy, MinCropSize, srcH - ny);
                break;

            case DragHandle.BR:
                nw = Clamp(_dragStartW + (int)dx, MinCropSize, srcW - nx);
                nh = Clamp(_dragStartH + (int)dy, MinCropSize, srcH - ny);
                break;
        }

        vm.SetCropRectLive(nx, ny, nw, nh);
        UpdateOverlay();
        e.Handled = true;
    }

    private void OnOverlayPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_activeDrag == DragHandle.None) return;
        if (DataContext is not ImageEditViewModel vm) return;

        e.Pointer.Capture(null);
        _activeDrag = DragHandle.None;
        vm.CommitCropRect(_dragStartX, _dragStartY, _dragStartW, _dragStartH);
        e.Handled = true;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static int Clamp(int value, int min, int max)
        => value < min ? min : value > max ? max : value;
}
