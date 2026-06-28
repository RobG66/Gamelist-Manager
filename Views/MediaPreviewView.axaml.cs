using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Gamelist_Manager.ViewModels;
using System.ComponentModel;
using System.Linq;

namespace Gamelist_Manager.Views;

public partial class MediaPreviewView : UserControl
{
    private Grid? _mediaContentGrid;
    private MediaPreviewViewModel? _viewModel;

    public MediaPreviewView()
    {
        InitializeComponent();

        var rescrapeButton = this.FindControl<Button>("RescrapeButton");
        if (rescrapeButton != null)
            rescrapeButton.Click += OnRescrapeButtonClick;

        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // Done in code behind to ensure the flyout is created fresh on each click.
    // A Flyout (not MenuFlyout) is used so the popup stays open when the checkboxes are toggled.
    private void OnRescrapeButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button button || _viewModel == null) return;

        double fontSize = (double)this.FindResource("GlobalFontSize")!;
        Flyout? flyout = null;

        var panel = new StackPanel { MinWidth = 180 };

        foreach (var scraper in _viewModel.Scrapers)
        {
            var scraperName = scraper.Name;
            bool available = _viewModel.IsScraperAvailable(scraper);
            var item = new Button
            {
                Content = scraperName,
                Command = _viewModel.ScrapeGameCommand,
                CommandParameter = scraperName,
                FontSize = fontSize,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 6),
                IsEnabled = available,
                Opacity = available ? 1.0 : 0.4
            };
            item.Click += (_, _) => flyout?.Hide();
            panel.Children.Add(item);
        }

        panel.Children.Add(new Border
        {
            Height = 1,
            Margin = new Thickness(0, 4),
            Background = (IBrush?)this.FindResource("SystemControlForegroundBaseMediumLowBrush")
        });

        var overwriteMediaBox = new CheckBox
        {
            Content = "Overwrite Media",
            IsChecked = _viewModel.OverwriteMedia,
            FontSize = fontSize,
            Margin = new Thickness(4, 2)
        };
        overwriteMediaBox.IsCheckedChanged += (_, _) =>
            _viewModel.OverwriteMedia = overwriteMediaBox.IsChecked == true;

        var overwriteMetadataBox = new CheckBox
        {
            Content = "Overwrite Metadata",
            IsChecked = _viewModel.OverwriteMetadata,
            FontSize = fontSize,
            Margin = new Thickness(4, 2)
        };
        overwriteMetadataBox.IsCheckedChanged += (_, _) =>
            _viewModel.OverwriteMetadata = overwriteMetadataBox.IsChecked == true;

        panel.Children.Add(overwriteMediaBox);
        panel.Children.Add(overwriteMetadataBox);

        flyout = new Flyout { Content = panel };
        flyout.ShowAt(button);
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel?.OnViewReady();
    }

    private void OnUnloaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        CleanupMediaGrid();
        _viewModel = null;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        var newViewModel = DataContext as MediaPreviewViewModel;
        bool isNewInstance = newViewModel != null && _viewModel != newViewModel;

        if (isNewInstance)
        {
            CleanupMediaGrid();
        }

        if (DataContext is MediaPreviewViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            if (_mediaContentGrid == null)
            {
                InitializeMediaGrid();
            }
        }
        else
        {
            CleanupMediaGrid();
            _viewModel = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaPreviewViewModel.ScaledDisplay) && _mediaContentGrid != null)
        {
            MoveGridBetweenContainers();
        }
    }

    private void CleanupMediaGrid()
    {
        if (_mediaContentGrid == null) return;

        try
        {
            if (_viewModel != null)
            {
                foreach (var mediaItem in _viewModel.MediaItems)
                {
                    mediaItem.PropertyChanged -= OnMediaItemPropertyChanged;
                }
            }

            if (_mediaContentGrid.Parent is Panel parent)
            {
                parent.Children.Remove(_mediaContentGrid);
            }

            _mediaContentGrid.Children.Clear();
            _mediaContentGrid.ColumnDefinitions.Clear();
            _mediaContentGrid.RowDefinitions.Clear();

            _mediaContentGrid = null;
        }
        catch
        {
        }
    }

    private void InitializeMediaGrid()
    {
        if (_viewModel == null) return;

        _mediaContentGrid = new Grid
        {
            Background = Brushes.Transparent
        };

        _mediaContentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _mediaContentGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        _mediaContentGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        int columnIndex = 0;
        foreach (var mediaItem in _viewModel.MediaItems)
        {
            var columnDef = new ColumnDefinition
            {
                Width = CalculateColumnWidth(mediaItem.IsVisible, _viewModel.ScaledDisplay)
            };
            _mediaContentGrid.ColumnDefinitions.Add(columnDef);

            mediaItem.PropertyChanged += OnMediaItemPropertyChanged;

            var mediaItemView = new MediaItemView
            {
                DataContext = mediaItem,
                IsScaled = _viewModel.ScaledDisplay
            };

            Grid.SetColumn(mediaItemView, columnIndex);
            Grid.SetRow(mediaItemView, 0);
            Grid.SetRowSpan(mediaItemView, 3);

            _mediaContentGrid.Children.Add(mediaItemView);
            columnIndex++;
        }

        var host = this.FindControl<Grid>("MediaContentHost");
        if (host != null && !host.Children.Contains(_mediaContentGrid))
        {
            host.Children.Clear();
            host.Children.Add(_mediaContentGrid);
        }

        MoveGridBetweenContainers();
    }

    private void OnMediaItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaItemViewModel.IsVisible) && _viewModel != null && _mediaContentGrid != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateColumnWidths());
        }
    }

    private void UpdateColumnWidths()
    {
        if (_viewModel == null || _mediaContentGrid == null) return;

        int colIdx = 0;
        foreach (var mediaItem in _viewModel.MediaItems)
        {
            if (colIdx < _mediaContentGrid.ColumnDefinitions.Count)
            {
                _mediaContentGrid.ColumnDefinitions[colIdx].Width = CalculateColumnWidth(mediaItem.IsVisible, _viewModel.ScaledDisplay);
            }
            colIdx++;
        }
    }

    private static GridLength CalculateColumnWidth(bool isVisible, bool scaledDisplay)
    {
        if (!isVisible)
            return new GridLength(0);
        return scaledDisplay ? new GridLength(1, GridUnitType.Star) : new GridLength(250);
    }

    private void MoveGridBetweenContainers()
    {
        if (_mediaContentGrid == null || _viewModel == null) return;

        foreach (var child in _mediaContentGrid.Children)
        {
            if (child is MediaItemView mediaItemView)
                mediaItemView.IsScaled = _viewModel.ScaledDisplay;
        }

        UpdateColumnWidths();

        var scrollViewer = this.FindControl<ScrollViewer>("MainScrollViewer");
        if (scrollViewer != null)
        {
            scrollViewer.HorizontalScrollBarVisibility = _viewModel.ScaledDisplay 
                ? Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled 
                : Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
        }
    }
}
