using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace Gamelist_Manager.Behaviors;

public class AutoScrollToEndBehavior : Behavior<ScrollViewer>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject!.PropertyChanged += OnScrollViewerPropertyChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.PropertyChanged -= OnScrollViewerPropertyChanged;
        base.OnDetaching();
    }

    private void OnScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.ExtentProperty)
        {
            Dispatcher.UIThread.Post(
                () => AssociatedObject!.Offset = new Vector(AssociatedObject.Offset.X, double.PositiveInfinity),
                DispatcherPriority.Background);
        }
    }
}
