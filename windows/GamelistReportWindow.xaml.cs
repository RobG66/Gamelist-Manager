using System;
using System.Collections.Generic;
using System.Windows;

namespace GamelistManager.classes.gamelist
{
    public partial class GamelistReportWindow : Window
    {
        private List<string> _gamelistPaths;
        private string? _currentSystemPath;

        public GamelistReportWindow(List<string> gamelistPaths, string? currentSystemPath = null)
        {
            InitializeComponent();

            _gamelistPaths = gamelistPaths;
            _currentSystemPath = currentSystemPath;

            // Disable current system checkbox if no current system is loaded
            if (string.IsNullOrEmpty(_currentSystemPath))
            {
                CurrentSystemOnlyCheckbox.IsEnabled = false;
                CurrentSystemOnlyCheckbox.ToolTip = "No system currently loaded";
            }

            UpdateInfoText();
        }

        private void CurrentSystemOnlyCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateInfoText();
        }

        private void UpdateInfoText()
        {
            if (CurrentSystemOnlyCheckbox?.IsChecked == true && !string.IsNullOrEmpty(_currentSystemPath))
            {
                string systemName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(_currentSystemPath) ?? "Unknown");
                InfoText.Text = $"Ready to analyze: {systemName}";
            }
            else
            {
                InfoText.Text = $"Ready to analyze {_gamelistPaths.Count} system(s)";
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            bool includeStorage = StorageCheckbox.IsChecked ?? true;

            List<string> pathsToAnalyze;

            if (CurrentSystemOnlyCheckbox.IsChecked == true && !string.IsNullOrEmpty(_currentSystemPath))
            {
                pathsToAnalyze = new List<string> { _currentSystemPath };
            }
            else
            {
                pathsToAnalyze = _gamelistPaths;
            }

            // Close this window and start the report generation
            Close();

            await GamelistReportGenerator.GenerateReportAsync(pathsToAnalyze, includeStorage);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}