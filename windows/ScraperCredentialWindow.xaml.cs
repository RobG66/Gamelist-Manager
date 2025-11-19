using GamelistManager.classes.core;
using GamelistManager.classes.helpers;
using GamelistManager.classes.io;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GamelistManager
{
    public partial class ScraperCredentialWindow : Window
    {
        private string currentScraper;

        private ObservableCollection<string> AvailableRegions;
        private ObservableCollection<string> SelectedRegions;

        private string iniPath = $"{SharedData.ProgramDirectory}\\ini\\screenscraper_options.ini";

        private Dictionary<string, string> regions = [];
        private Dictionary<string, string> languages = [];

        // Default JSON fallback string
        private static readonly string DefaultJsonFallback =
            "[\"USA (us)\",\"ScreenScraper (ss)\",\"Europe (eu)\",\"United Kingdom (uk)\",\"World (wor)\"]";


        public ScraperCredentialWindow(string scraper)
        {
            InitializeComponent();
            currentScraper = scraper;

            if (currentScraper == "ScreenScraper")
            {
                // Only load regions/languages and initialize collections for ScreenScraper
                regions = IniFileReader.GetSection(iniPath, "Regions");
                languages = IniFileReader.GetSection(iniPath, "Languages");

                AvailableRegions = [];
                SelectedRegions = [];

                listBoxAvailableRegions.ItemsSource = AvailableRegions;
                listBoxSelectedRegions.ItemsSource = SelectedRegions;

                var view = CollectionViewSource.GetDefaultView(AvailableRegions);
                view.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));

                SetupForScreenScraper();
                stackPanel_ScreenScraperOptions.Visibility = Visibility.Visible;

            }
            else
            {
                stackPanel_ScreenScraperOptions.Visibility = Visibility.Collapsed;
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string userName, userPassword;
            (userName, userPassword) = CredentialHelper.GetCredentials(currentScraper);

            textBox_UserID.Text = userName;
            textBox_Password.Text = userPassword;
            button_Save.IsEnabled = false;
            this.Title = $"{currentScraper} Credential Setup";

        }

        private void SetupForScreenScraper()
        {
            comboBox_Region.ItemsSource = regions.Keys;
            comboBox_Language.ItemsSource = languages.Keys;

            string language = Properties.Settings.Default.Language;
            string region = Properties.Settings.Default.Region;

            comboBox_Language.Text = (!string.IsNullOrEmpty(language) && languages.ContainsKey(language))
                ? language
                : (languages.Count > 0 ? languages.Keys.First() : "");

            comboBox_Region.Text = (!string.IsNullOrEmpty(region) && regions.ContainsKey(region))
                ? region
                : (regions.Count > 0 ? regions.Keys.First() : "");

            checkBox_GenreAlwaysEnglish.IsChecked = Properties.Settings.Default.ScrapeEnglishGenreOnly;

            checkBox_ScrapeAnyMedia.IsChecked = Properties.Settings.Default.ScrapeAnyMedia;


            LoadListboxes();

            // Remove primary Region from fallback list if present
            var fallbackList = SelectedRegions.ToList();
            if (fallbackList.Contains(region) && !string.IsNullOrEmpty(region))
            {
                fallbackList.Remove(region);
            }
        }

        private void LoadListboxes()
        {
            AvailableRegions.Clear();
            SelectedRegions.Clear();

            foreach (var regionCode in regions.Keys)
                AvailableRegions.Add(regionCode);

            List<string> fallbackList;

            string jsonString = Properties.Settings.Default.Region_Fallback;

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                // Use default JSON string if nothing in settings
                jsonString = DefaultJsonFallback;
            }

            try
            {
                fallbackList = JsonSerializer.Deserialize<List<string>>(jsonString) ?? [];
            }
            catch
            {
                // Fallback in case JSON is invalid
                fallbackList = JsonSerializer.Deserialize<List<string>>(DefaultJsonFallback) ?? [];
            }

            // Move fallback regions to SelectedRegions in order
            foreach (var regionCode in fallbackList)
            {
                if (AvailableRegions.Contains(regionCode))
                {
                    AvailableRegions.Remove(regionCode);
                    SelectedRegions.Add(regionCode);
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            var selected = listBoxAvailableRegions.SelectedItems.Cast<string>().ToList();
            foreach (var region in selected)
            {
                AvailableRegions.Remove(region);
                SelectedRegions.Add(region);
            }

            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            var selected = listBoxSelectedRegions.SelectedItems.Cast<string>().ToList();
            foreach (var region in selected)
            {
                SelectedRegions.Remove(region);
                AvailableRegions.Add(region);
            }

            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void Button_Up_Click(object sender, RoutedEventArgs e)
        {
            var selected = listBoxSelectedRegions.SelectedItems.Cast<string>().ToList();
            foreach (var region in selected)
            {
                int index = SelectedRegions.IndexOf(region);
                if (index > 0)
                    SelectedRegions.Move(index, index - 1);
            }

            button_Save.Content = "Save";
            button_Save.IsEnabled = true;

        }

        private void Button_Down_Click(object sender, RoutedEventArgs e)
        {
            var selected = listBoxSelectedRegions.SelectedItems.Cast<string>().Reverse().ToList();
            foreach (var region in selected)
            {
                int index = SelectedRegions.IndexOf(region);
                if (index < SelectedRegions.Count - 1)
                    SelectedRegions.Move(index, index + 1);
            }

            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            // Reset to default fallback
            Properties.Settings.Default.Region_Fallback = DefaultJsonFallback;
            LoadListboxes();
            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            string userID = textBox_UserID.Text;
            string password = textBox_Password.Text;

            if (string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("One or more fields are empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (currentScraper == "ScreenScraper")
            {
                string primaryRegion = comboBox_Region.Text;
                string language = comboBox_Language.Text;
                bool scrapeEnglishGenreOnly = checkBox_GenreAlwaysEnglish.IsChecked == true;
                bool scrapeAnyMedia = checkBox_ScrapeAnyMedia.IsChecked == true;

                Properties.Settings.Default.Region = primaryRegion;
                Properties.Settings.Default.Language = language;
                Properties.Settings.Default.ScrapeEnglishGenreOnly = scrapeEnglishGenreOnly;
                Properties.Settings.Default.ScrapeAnyMedia = scrapeAnyMedia;

                // Build JSON fallback: primary first
                var fallbackList = SelectedRegions.ToList();
                if (fallbackList.Contains(primaryRegion) && !string.IsNullOrEmpty(primaryRegion))
                {
                    fallbackList.Remove(primaryRegion);
                }

                string jsonFallback = JsonSerializer.Serialize(fallbackList);
                Properties.Settings.Default.Region_Fallback = jsonFallback;
            }

            CredentialHelper.SaveCredentials(currentScraper, userID, password);
            Properties.Settings.Default.Save();

            button_Save.Content = "Saved";
            button_Save.IsEnabled = false;
        }
    }
}
