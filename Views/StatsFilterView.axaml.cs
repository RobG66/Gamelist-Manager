using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Gamelist_Manager.Views;

public partial class StatsFilterView : UserControl
{
    private const double ScrollStep = 160;
    private ScrollViewer? _mediaScroller;

    public StatsFilterView()
    {
        InitializeComponent();
    }

    private ScrollViewer? MediaScroller =>
        _mediaScroller ??= this.FindControl<ScrollViewer>("MediaAuditScroller");

    private void ScrollMediaLeft_Click(object? sender, RoutedEventArgs e)
    {
        if (MediaScroller is not { } scroller) return;
        scroller.Offset = new Vector(Math.Max(0, scroller.Offset.X - ScrollStep), scroller.Offset.Y);
    }

    private void ScrollMediaRight_Click(object? sender, RoutedEventArgs e)
    {
        if (MediaScroller is not { } scroller) return;
        scroller.Offset = new Vector(Math.Min(scroller.ScrollBarMaximum.X, scroller.Offset.X + ScrollStep), scroller.Offset.Y);
    }
}
