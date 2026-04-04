using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Gamelist_Manager.ViewModels;
using Gamelist_Manager.Views;

namespace Gamelist_Manager;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        // Explicit mapping of ViewModels to Views
        return param switch
        {
            MediaPreviewViewModel => new MediaPreviewView(),
            ScraperViewModel => new ScraperView(),
            DatToolViewModel => new DatToolView(),
            _ => new TextBlock { Text = "View not found for: " + param.GetType().Name }
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}