using System.Threading.Tasks;
using Avalonia.Controls;
using Gamelist_Manager.Views;

namespace Gamelist_Manager.Services;

public class WindowService : IWindowService
{
    public static readonly WindowService Instance = new();

    private Window? _owner;

    private WindowService() { }

    public void SetOwner(Window owner) => _owner = owner;

    public async Task ShowSettingsAsync()
    {
        if (_owner is null) return;
        var window = new SettingsView();
        await window.ShowDialog(_owner);
    }

    public async Task ShowAboutAsync()
    {
        if (_owner is null) return;
        var window = new AboutWindow();
        await window.ShowDialog(_owner);
    }
}
