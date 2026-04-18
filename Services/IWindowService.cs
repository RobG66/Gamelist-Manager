using Avalonia.Controls;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services;

public interface IWindowService
{
    void SetOwner(Window owner);
    Task ShowSettingsAsync();
    Task ShowAboutAsync();
}
