using GamelistManager.classes.helpers;
using System.Windows;

namespace GamelistManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Load and apply saved theme
            var savedTheme = ThemeHelper.Instance.LoadThemePreference();
            ThemeHelper.Instance.ApplyTheme(savedTheme);
        }
    }
}

