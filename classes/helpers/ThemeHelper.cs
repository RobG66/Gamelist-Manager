using System.Windows;

namespace GamelistManager.classes.helpers
{
    public class ThemeHelper
    {
        private static ThemeHelper _instance;
        public static ThemeHelper Instance => _instance ??= new ThemeHelper();
        public enum Theme
        {
            Default,
            Blue,
            Cool,
            Dark,
            Gray,
            Sandy,
            Sunset,
            Mint,
            Warm
        }
        public void ApplyTheme(Theme theme)
        {
            var app = Application.Current;
            // Clear existing theme dictionaries
            var existingTheme = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null &&
                               (d.Source.OriginalString.Contains("DefaultTheme") ||
                                d.Source.OriginalString.Contains("BlueTheme") ||
                                d.Source.OriginalString.Contains("WarmTheme") ||
                                d.Source.OriginalString.Contains("CoolTheme") ||
                                d.Source.OriginalString.Contains("GrayTheme") ||
                                d.Source.OriginalString.Contains("MintTheme") ||
                                d.Source.OriginalString.Contains("SandyTheme") ||
                                d.Source.OriginalString.Contains("DarkTheme") ||
                                d.Source.OriginalString.Contains("SunsetTheme")));

            if (existingTheme != null)
                app.Resources.MergedDictionaries.Remove(existingTheme);
            // Add new theme dictionary
            string themeFile = theme switch
            {
                Theme.Default => "DefaultTheme.xaml",
                Theme.Warm => "WarmTheme.xaml",
                Theme.Cool => "CoolTheme.xaml",
                Theme.Gray => "GrayTheme.xaml",
                Theme.Mint => "MintTheme.xaml",
                Theme.Sandy => "SandyTheme.xaml",
                Theme.Sunset => "SunsetTheme.xaml",
                Theme.Blue => "BlueTheme.xaml",
                _ => "DefaultTheme.xaml"
            };

            var newTheme = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/resources/resource dictionaries/themes/{themeFile}", UriKind.Absolute)
            };
            app.Resources.MergedDictionaries.Add(newTheme);
        }
        public void SaveThemePreference(Theme theme)
        {
            Properties.Settings.Default.Theme = theme.ToString();
            Properties.Settings.Default.Save();
        }
        public Theme LoadThemePreference()
        {
            if (Enum.TryParse<Theme>(Properties.Settings.Default.Theme, out var theme))
                return theme;
            return Theme.Default;
        }
    }
}