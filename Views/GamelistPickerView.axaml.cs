using Avalonia.Controls;
using Avalonia.Input;
using Gamelist_Manager.ViewModels;
using System;

namespace Gamelist_Manager.Views;

public partial class GamelistPickerView : Window
{
    private GamelistPickerViewModel? _vm;

    public GamelistPickerView()
    {
        InitializeComponent();
        this.Loaded += (_, _) => SearchBox.Focus();
    }

    public GamelistPickerView(GamelistPickerViewModel vm) : this()
    {
        _vm = vm;
        DataContext = vm;

        vm.ConfirmRequested += OnConfirm;
        vm.CancelRequested += OnCancel;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _vm?.StartCountingGames();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _vm?.CancelCounting();
        base.OnClosing(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(null);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    internal void OnTileDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control { DataContext: SystemPickerItem item })
            _vm?.ConfirmSystemCommand.Execute(item);
    }

    private void OnConfirm(object? sender, EventArgs e) => Close(_vm?.SelectedSystem);
    private void OnCancel(object? sender, EventArgs e) => Close(null);
}
