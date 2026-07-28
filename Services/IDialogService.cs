using Gamelist_Manager.Models;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services;

public interface IDialogService
{
    Task<ThreeButtonResult> ShowAsync(ThreeButtonDialogConfig config, object? owner = null);
    Task ShowInfoAsync(string title, string message, string? detail = null, object? owner = null);
    Task ShowWarningAsync(string title, string message, string? detail = null, object? owner = null);
    Task ShowErrorAsync(string title, string message, string? detail = null, object? owner = null);
    Task<bool> ShowConfirmAsync(
        string title,
        string message,
        string confirmText = "Yes",
        string cancelText = "Cancel",
        DialogIconTheme icon = DialogIconTheme.Question,
        string? detail = null,
        object? owner = null);
}
