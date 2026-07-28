using Gamelist_Manager.Models;
using Gamelist_Manager.Views;
using System.Threading.Tasks;

namespace Gamelist_Manager.Services;

public class DialogService : IDialogService
{
    private static IDialogService? _instance;

    public static IDialogService Instance => _instance ??= new DialogService();

    public static void SetInstance(IDialogService instance) => _instance = instance;

    public Task<ThreeButtonResult> ShowAsync(ThreeButtonDialogConfig config, object? owner = null)
        => ThreeButtonDialogView.ShowAsync(config, owner);

    public Task ShowInfoAsync(string title, string message, string? detail = null, object? owner = null)
        => ThreeButtonDialogView.ShowInfoAsync(title, message, detail, owner);

    public Task ShowWarningAsync(string title, string message, string? detail = null, object? owner = null)
        => ThreeButtonDialogView.ShowWarningAsync(title, message, detail, owner);

    public Task ShowErrorAsync(string title, string message, string? detail = null, object? owner = null)
        => ThreeButtonDialogView.ShowErrorAsync(title, message, detail, owner);

    public Task<bool> ShowConfirmAsync(
        string title,
        string message,
        string confirmText = "Yes",
        string cancelText = "Cancel",
        DialogIconTheme icon = DialogIconTheme.Question,
        string? detail = null,
        object? owner = null)
        => ThreeButtonDialogView.ShowConfirmAsync(title, message, confirmText, cancelText, icon, detail, owner);
}
