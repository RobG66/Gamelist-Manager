using Avalonia.Controls;
using Avalonia.Input.Platform;
using Gamelist_Manager.Views;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services;

public class WindowService : IWindowService
{
    private static WindowService? _instance;
    public static WindowService Instance => _instance ??= new WindowService();

    private Window? _owner;

    private WindowService() { }

    public void SetOwner(Window owner) => _owner = owner;

    public async Task ShowSettingsAsync()
    {
        if (_owner is null) return;
        var window = new SettingsView();
        await window.ShowDialog(_owner);
    }

    public async Task ShowSettingsAsync(int tabIndex, int scraperIndex)
    {
        if (_owner is null) return;
        var window = new SettingsView();
        window.NavigateTo(tabIndex, scraperIndex);
        await window.ShowDialog(_owner);
    }

    public async Task ShowAboutAsync()
    {
        if (_owner is null) return;
        var window = new AboutWindow();
        await window.ShowDialog(_owner);
    }

    public async Task CopyToClipboardAsync(string text)
    {
        if (_owner is null) return;
        var clipboard = TopLevel.GetTopLevel(_owner)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(text);
    }
}
