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
    // Task ShowJukeboxAsync(string[] mediaFiles, string systemName, Action<Jukebox.ViewModels.JukeboxViewModel, Window>? configure = null);
    // void CloseJukebox();
}
