using GamelistManager.classes;
using System.Windows;
using System.Windows.Controls;

namespace GamelistManager
{
    /// <summary>
    /// Interaction logic for ScraperCredentialDialog.xaml
    /// </summary>
    public partial class ScraperCredentialDialog : Window
    {
        string currentScraper;
        public ScraperCredentialDialog(string scraper)
        {
            InitializeComponent();
            currentScraper = scraper;
        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string userName = string.Empty;
            string userPassword = string.Empty;
            (userName, userPassword) = CredentialManager.GetCredentials(currentScraper);


            textBox_UserID.Text = userName;
            textBox_Password.Text = userPassword;
            button_Save.IsEnabled = false;
            this.Title = $"{currentScraper} Credential Setup";

            if (currentScraper == "ScreenScraper")
            {
                SetupForScreenScraper();
                stackPanel_ScreenScraperOptions.Visibility = Visibility.Visible;
            }
            else
            {
                stackPanel_ScreenScraperOptions.Visibility = Visibility.Collapsed; ;
            }
        }

        private void SetupForScreenScraper()
        {
            IniFileReader iniReader = new IniFileReader($"{SharedData.ProgramDirectory}\\ini\\screenscraper_options.ini");
            var regions = iniReader.GetSection("Regions");
            var languages = iniReader.GetSection("Languages");

            comboBox_Region.ItemsSource = regions.Keys;
            comboBox_Language.ItemsSource = languages.Keys;

            string language = Properties.Settings.Default.Language;
            string region = Properties.Settings.Default.Region;

            if (!string.IsNullOrEmpty(language) && languages.ContainsKey(language))
            {
                comboBox_Language.Text = language;
            }
            else
            {
                comboBox_Language.SelectedIndex = 0;
            }

            if (!string.IsNullOrEmpty(region) && regions.ContainsKey(region))
            {
                comboBox_Region.Text = region;
            }
            else
            {
                comboBox_Region.SelectedIndex = 0;
            }
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

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
                string region = comboBox_Region.Text;
                string language = comboBox_Language.Text;

                Properties.Settings.Default.Region = region;
                Properties.Settings.Default.Language = language;
                Properties.Settings.Default.Save();
            }

            bool result = CredentialManager.SaveCredentials(currentScraper, userID, password);
            button_Save.Content = "Saved";
            button_Save.IsEnabled = false;
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            button_Save.Content = "Save";
            button_Save.IsEnabled = true;
        }
    }
}

