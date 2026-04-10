using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Gamelist_Manager.Models;
using Gamelist_Manager.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace Gamelist_Manager.Views;

public partial class MenuView : UserControl
{
    private readonly Dictionary<string, CheckBox> _columnCheckBoxes = new();

    public MenuView()
    {
        InitializeComponent();
        BuildColumnMenuItems();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ColumnVisibilityChanged -= RefreshColumnCheckBoxes;
            vm.ColumnVisibilityChanged += RefreshColumnCheckBoxes;
            RefreshColumnCheckBoxes();
        }
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

    private void RecentFileButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string filePath } && DataContext is MainWindowViewModel vm)
        {
            vm.OpenRecentGamelistCommand.Execute(filePath);
        }
        Dispatcher.UIThread.Post(() => RecentButton.Flyout?.Hide());
    }

    private void SystemItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string gamelistPath } && DataContext is MainWindowViewModel vm)
        {
            vm.OpenSystemGamelistCommand.Execute(gamelistPath);
        }
        Dispatcher.UIThread.Post(() => SystemsButton.Flyout?.Hide());
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
