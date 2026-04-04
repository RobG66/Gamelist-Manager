using System.Threading.Tasks;

namespace Gamelist_Manager.Services;

public interface IWindowService
{
    Task ShowSettingsAsync();
    Task ShowAboutAsync();
}
