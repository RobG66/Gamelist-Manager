using Avalonia.Controls;
using Avalonia.Input.Platform;
using Gamelist_Manager.ViewModels;
using Gamelist_Manager.Views;
using System.Threading.Tasks;
using Jukebox.Views;
using Jukebox.ViewModels;

namespace Gamelist_Manager.Services;

public class WindowService : IWindowService
{
    #region Fields & Constants

    private static WindowService? _instance;
    private Window? _owner;
    private JukeboxView? _jukeboxView;

    #endregion

    #region Public Properties

    public static WindowService Instance => _instance ??= new WindowService();

    #endregion

    #region Constructor

    private WindowService() { }

    #endregion

    #region Public Methods

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

    public async Task ShowJukeboxAsync(string[] mediaFiles, string systemName)
    {
        if (_owner is null) return;

        // Single instance — if already open, bring to front
        if (_jukeboxView is { } existing && existing.IsVisible)
        {
            existing.Activate();
            return;
        }

        var viewModel = new JukeboxViewModel();
        viewModel.LoadSystemLogo(systemName);
        var window = new JukeboxView { DataContext = viewModel };

        // Clean up reference when the window closes
        window.Closed += (_, _) => 
        {
            _jukeboxView = null;
            Models.SessionState.Instance.IsJukeboxOpen = false;
        };

        _jukeboxView = window;
        Models.SessionState.Instance.IsJukeboxOpen = true;

        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        window.Loaded += (s, e) => tcs.TrySetResult(true);

        window.Show();

        await tcs.Task;
        await viewModel.PlayMediaFilesAsync(mediaFiles, autoPlay: true);
    }

    public void CloseJukebox()
    {
        if (_jukeboxView is { } existing)
        {
            existing.Close();
        }
    }

    #endregion
}
