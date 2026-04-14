using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Gamelist_Manager.Models;
using Gamelist_Manager.Services;
using Gamelist_Manager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Gamelist_Manager.Views;

public partial class MenuView : UserControl
{
    private readonly Dictionary<string, CheckBox> _columnCheckBoxes = new();
    private MenuFlyout? _recentFilesFlyout;
    private Bitmap? _loadIconBitmap;

    public MenuView()
    {
        InitializeComponent();
        BuildColumnMenuItems();
        _recentFilesFlyout = RecentButton.Flyout as MenuFlyout;
        SharedDataService.Instance.RecentFiles.CollectionChanged += OnRecentFilesChanged;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ColumnVisibilityChanged -= RefreshColumnCheckBoxes;
            vm.ColumnVisibilityChanged += RefreshColumnCheckBoxes;
            RefreshColumnCheckBoxes();
            BuildRecentMenuItems();
            BuildSystemMenuItems();
        }
    }

    private void OnRecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        BuildRecentMenuItems();
    }

    private void BuildColumnMenuItems()
    {
        if (ColumnsButton.Flyout is not MenuFlyout flyout) return;

        var insertPoint = flyout.Items.OfType<Separator>().Skip(1).FirstOrDefault();
        var insertIndex = insertPoint != null ? flyout.Items.IndexOf(insertPoint) : flyout.Items.Count;

        foreach (var decl in GamelistMetaData.GetToggleableColumns())
        {
            var checkBox = new CheckBox
            {
                Padding = new Avalonia.Thickness(0),
                BorderThickness = new Avalonia.Thickness(0),
                IsHitTestVisible = false,
            };
            _columnCheckBoxes[decl.Type] = checkBox;

            var menuItem = new MenuItem
            {
                Header = decl.Name,
                StaysOpenOnClick = true,
                Icon = checkBox,
                Tag = decl.Type,
            };
            menuItem.Click += ColumnMenuItem_Click;

            flyout.Items.Insert(insertIndex, menuItem);
            insertIndex++;
        }
    }

    private void ColumnMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string type } && DataContext is MainWindowViewModel vm)
            vm.ToggleColumnCommand.Execute(type);
    }

    private void RefreshColumnCheckBoxes()
    {
        if (DataContext is not MainWindowViewModel vm) return;

        foreach (var (type, checkBox) in _columnCheckBoxes)
            checkBox.IsChecked = vm.GetColumnVisible(type);
    }

    private void BuildRecentMenuItems()
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (_recentFilesFlyout == null) return;

        _recentFilesFlyout.Items.Clear();

        _loadIconBitmap ??= new Bitmap(AssetLoader.Open(
            new Uri("avares://Gamelist_Manager/Assets/Icons/load.png")));

        var iconSize = this.TryFindResource("GlobalMenuIconSize", out var res) && res is double d ? d : 16.0;

        var recentFiles = SharedDataService.Instance.RecentFiles;

        foreach (var file in recentFiles)
        {
            var item = new MenuItem
            {
                Header = file.DisplayPath,
                Command = vm.OpenRecentGamelistCommand,
                CommandParameter = file.FilePath,
                Icon = new Image { Source = _loadIconBitmap, Width = iconSize, Height = iconSize },
            };
            ToolTip.SetTip(item, file.ToolTip);
            _recentFilesFlyout.Items.Add(item);
        }

        if (recentFiles.Count > 0)
            _recentFilesFlyout.Items.Add(new Separator());

        _recentFilesFlyout.Items.Add(new MenuItem
        {
            Header = "Clear Recent Files",
            Command = vm.ClearRecentFilesCommand,
        });
    }

    private void BuildSystemMenuItems()
    {
        if (DataContext is not MainWindowViewModel vm) return;

        SystemsPanel.Children.Clear();

        foreach (var system in vm.Systems)
        {
            var button = new Button
            {
                Classes = { "NavButton" },
                Tag = system.GamelistPath,
                Content = new Image
                {
                    Source = system.Logo,
                    MaxWidth = 200,
                    MaxHeight = 38,
                    Stretch = Avalonia.Media.Stretch.Uniform,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                },
            };
            ToolTip.SetTip(button, system.GamelistPath);
            button.Click += (_, _) =>
            {
                vm.OpenSystemGamelistCommand.Execute(system.GamelistPath);
                SystemsButton.Flyout?.Hide();
            };
            SystemsPanel.Children.Add(button);
        }
    }

    private void ProfileButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string profileName } && DataContext is MainWindowViewModel vm)
        {
            vm.SwitchProfileCommand.Execute(profileName);
        }
        Dispatcher.UIThread.Post(() => ProfileButton.Flyout?.Hide());
    }
}
