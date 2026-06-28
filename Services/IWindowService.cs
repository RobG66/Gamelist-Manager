using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services;

public interface IWindowService
{
    void SetOwner(Window owner);
    Task ShowSettingsAsync();
    Task ShowSettingsAsync(int tabIndex, int scraperIndex);
    Task ShowAboutAsync();
    Task CopyToClipboardAsync(string text);

    // TODO: Jukebox disabled — pending Jukebox project restoration and LibVLC→mpv migration.
    // Task ShowJukeboxAsync(string[] mediaFiles, string systemName, Action<Jukebox.ViewModels.JukeboxViewModel, Window>? configure = null);
    // void CloseJukebox();
}
