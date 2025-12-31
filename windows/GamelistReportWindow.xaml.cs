using System;
using System.Collections.Generic;
using System.Windows;

namespace GamelistManager.classes.gamelist
{
    public partial class GamelistReportWindow : Window
    {
        private List<string> _gamelistPaths;

        public GamelistReportWindow(List<string> gamelistPaths)
        {
            InitializeComponent();

            _gamelistPaths = gamelistPaths;

            // Update info text with count
            InfoText.Text = $"Ready to analyze {_gamelistPaths.Count} system(s)";
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            bool includeStorage = StorageCheckbox.IsChecked ?? true;

            // Close this window and start the report generation
            Close();

            await GamelistReportGenerator.GenerateReportAsync(_gamelistPaths, includeStorage);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}