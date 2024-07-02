using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GamelistManager.control
{
    public partial class ScraperSetup : Form
    {
        private string scraperPlatform;
        public bool useDefaults;

        public ScraperSetup(string platform)
        {
            InitializeComponent();
            this.Text = $"{scraperPlatform} Setup";
            labelName.Text = $"{scraperPlatform} ID";
            labelPassword.Text = $"{scraperPlatform} Password";
            scraperPlatform = platform;
            useDefaults = true;
        }


        private async void Button1_Click(object sender, EventArgs e)
        {
            bool response = await CheckScraperCredentialsAsync();

            if (response != true)
            {
                this.Enabled = true;
                MessageBox.Show("The credentials do not appear to be valid and were not saved!", "Credential Check", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SaveGameOptions();
        }

        private void SaveGameOptions() { 
            
            this.Enabled = false;
            SaveCredentials();
            
            if (useDefaults == true)
            {
                panelOptions.Enabled = true;
                this.Enabled = true;
                SetDefaultOrSavedOptions();
            }

            if (scraperPlatform == "ScreenScraper")
            {
                SaveRegion();
                SaveLanguage();
                SaveNonGameOptions();
            }
           
            SaveImageSource();
            SaveBoxSource();
            SaveLogoSource();
            SaveMaxThreads();

            buttonSave.Enabled = false;
            useDefaults = false;
            this.Enabled = true;
        }

        private void SaveNonGameOptions()
        {
            bool scrapeByGameID = checkBoxScrapeByGameID.Checked;
            bool hideNonGame = checkBoxHideNonGame.Checked;
            bool noZZZ = checkBoxNoZZZ.Checked;
            RegistryManager.SaveScraperSettings(scraperPlatform, "HideNonGame", hideNonGame.ToString());
            RegistryManager.SaveScraperSettings(scraperPlatform, "NoZZZ", noZZZ.ToString());
            RegistryManager.SaveScraperSettings(scraperPlatform, "ScrapeByGameID", scrapeByGameID.ToString());
        }

        private void SaveMaxThreads()
        {
            string maxThreadsValue = comboBoxMaxThreads.Text;
            RegistryManager.SaveScraperSettings(scraperPlatform, "MaxThreads", maxThreadsValue);
        }

        private void SaveLogoSource()
        {
            string boxValue = comboBoxLogoSource.Text;
            RegistryManager.SaveScraperSettings(scraperPlatform, "LogoSource", boxValue);
        }

        private void SaveImageSource()
        {
            string boxValue = comboBoxImageSource.Text;
            RegistryManager.SaveScraperSettings(scraperPlatform, "ImageSource", boxValue);
        }

        private void SaveBoxSource()
        {
            string boxValue = comboBoxBoxSource.Text;
            RegistryManager.SaveScraperSettings(scraperPlatform, "BoxSource", boxValue);
        }

        private void SaveLanguage()
        {
            if (scraperPlatform == "EmuMovies")
            {
                return;
            }
            string boxValue = comboBoxLanguage.Text;
            string language = boxValue.Split(':')[0].Trim();
            RegistryManager.SaveScraperSettings(scraperPlatform, "Language", language);
        }

        private void SaveRegion()
        {
            if (scraperPlatform == "EmuMovies")
            {
                return;
            }
            string boxValue = comboBoxRegion.Text;
            string region = boxValue.Split(':')[0].Trim();
            RegistryManager.SaveScraperSettings(scraperPlatform, "Region", region);
        }

        private void SaveCredentials()
        {
            string userID = textboxScraperName.Text;
            string userPassword = textboxScraperPassword.Text;

            Credential CredentialManager = new Credential()
            {
                Target = scraperPlatform,
                Username = userID,
                Password = userPassword,
                PersistanceType = PersistanceType.LocalComputer,
            };
            CredentialManager.Save();
        }


        private void Button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void ScreenScraperID_TextChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
        }

        private void ScraperPassword_TextChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkboxShowPassword.Checked)
            {
                textboxScraperPassword.UseSystemPasswordChar = false;
            }
            else
            {
                textboxScraperPassword.UseSystemPasswordChar = true;
            }
        }

        private async Task<bool> CheckScraperCredentialsAsync()
        {
            string username = textboxScraperName.Text;
            string password = textboxScraperPassword.Text;

            if (scraperPlatform == "ScreenScraper")
            {
                XmlNode xmlResponse = await API_ScreenScraper.AuthenticateScreenScraperAsync(username, password);
                if (xmlResponse == null)
                {
                    return false;
                }
                
                string text = xmlResponse.OuterXml;
                XmlNode userID = xmlResponse.SelectSingleNode("//ssuser/id");
                if (userID == null)
                {
                    return false;
                }
                
                string remoteID = userID.InnerText;
                if (username.ToLower() == remoteID.ToLower())
                {
                    return true;
                }
                return false;
            }

            if (scraperPlatform == "EmuMovies")
            {
                string result = await API_EmuMovies.AuthenticateEmuMoviesAsync(username, password);
                if (!string.IsNullOrEmpty(result))
                {
                    return true;
                }

            }
            return false;
        }

        private void ComboBox_ImageSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
        }

        private void ComboBox_BoxSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
        }

        private void ComboBox_LogoSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
        }

        private async void ScraperSetup_Load(object sender, EventArgs e)
        {

            if (scraperPlatform == "EmuMovies")
            {
                comboBoxLanguage.Visible = false;
                comboBoxRegion.Visible = false;
                labelLanguage.Visible = false;
                labelRegion.Visible = false;
                checkBoxNoZZZ.Visible = false;
                checkBoxScrapeByGameID.Visible = false;
                checkBoxHideNonGame.Visible = false;
            }

            (string userName, string userPassword) = CredentialManager.GetCredentials(scraperPlatform);

            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userPassword))
            {
                textboxScraperName.Text = userName;
                textboxScraperPassword.Text = userPassword;
                useDefaults = false;
                panelOptions.Enabled = true;
            }
            else
            {
                // If the credential is missing, treat it like a new setup
                // Options controls are disabled until credentials are saved
                useDefaults = true;
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            this.Enabled = false;

            bool result = await CheckScraperCredentialsAsync();

            if (result == false)
            {
                MessageBox.Show($"The credentials could not be validated by {scraperPlatform}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Enabled = true;
                return;
            }

            // Get the option values from ini file
            PopulateComboBoxeValues();

            // Either set defaults or load saved settings
            SetDefaultOrSavedOptions();

            this.Enabled = true;
            Cursor.Current = Cursors.Default;

        }

        private void PopulateComboBoxeValues()
        {
            string file = null;
            if (scraperPlatform == "EmuMovies")
            {
                file = "ini\\emumovies_options.ini";
            }
            if (scraperPlatform == "ScreenScraper")
            {
                file = "ini\\screenscraper_options.ini";
            }

            if (file == null) {return; }

            IniFileReader iniReader = new IniFileReader(file);
            Dictionary<string, Dictionary<string, string>> allSections = iniReader.GetAllSections();

            // Populate ComboBoxes based on section names
            foreach (var section in allSections)
            {               
                string sectionName = section.Key;
                Dictionary<string, string> sectionValues = section.Value;
             
                // Populate ComboBoxes based on section name
                switch (sectionName)
                {
                    case "ImageSource":
                        comboBoxImageSource.Items.AddRange(sectionValues.Values.ToArray());
                        break;
                    case "BoxSource":
                        comboBoxBoxSource.Items.AddRange(sectionValues.Values.ToArray());
                        break;
                    case "LogoSource":
                        comboBoxLogoSource.Items.AddRange(sectionValues.Values.ToArray());
                        break;
                    case "Regions":
                        comboBoxRegion.Items.AddRange(sectionValues.Values.ToArray());
                        break;
                    case "Languages":
                        comboBoxLanguage.Items.AddRange(sectionValues.Values.ToArray());
                        break;
                    default:
                        // Handle unexpected section name
                        break;
                }
            }
        }
        private async void SetDefaultOrSavedOptions()
        {           
            bool saveRequired = false;
            string boxSource = RegistryManager.ReadRegistryValue(scraperPlatform, "BoxSource");
            string imageSource = RegistryManager.ReadRegistryValue(scraperPlatform, "ImageSource");
            string logoSource = RegistryManager.ReadRegistryValue(scraperPlatform, "LogoSource");

            if (comboBoxBoxSource.Items.Contains(boxSource) && !string.IsNullOrEmpty(boxSource))
            {
                comboBoxBoxSource.Text = boxSource;
            }
            else
            {
                if (comboBoxBoxSource.Items.Count > 0)
                {
                    comboBoxBoxSource.SelectedIndex = 0;
                }
                saveRequired = true;
            }

            if (comboBoxImageSource.Items.Contains(imageSource) && !string.IsNullOrEmpty(imageSource))
            {
                comboBoxImageSource.Text = imageSource;
            }
            else
            {
                if (comboBoxImageSource.Items.Count > 0)
                {
                    comboBoxImageSource.SelectedIndex = 0;
                }
                saveRequired = true;
            }

            if (comboBoxLogoSource.Items.Contains(logoSource) && !string.IsNullOrEmpty(logoSource))
            {
                comboBoxLogoSource.Text = logoSource;
            }
            else
            {
                if (comboBoxLogoSource.Items.Count > 0)
                {
                    comboBoxLogoSource.SelectedIndex = 0;
                }
                saveRequired = true;
            }


            int maxThreads = 1;

            if (scraperPlatform == "ScreenScraper")
            {
                maxThreads = await GetMaxThreads();

                bool.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "HideNonGame"), out bool hideNonGame);
                bool.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "NoZZZ"), out bool noZZZ);
                bool.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "ScrapeByGameID"), out bool scrapeByGameID);
                checkBoxHideNonGame.Checked = hideNonGame;
                checkBoxNoZZZ.Checked = noZZZ;
                checkBoxScrapeByGameID.Checked = scrapeByGameID;

                string language = RegistryManager.ReadRegistryValue(scraperPlatform, "Language");
                if (comboBoxLanguage.Items.Contains(language) && !string.IsNullOrEmpty(language))
                {
                    comboBoxLanguage.Text = language;
                }
                else
                {
                    comboBoxLanguage.SelectedIndex = 0;
                }

                string region = RegistryManager.ReadRegistryValue(scraperPlatform, "Region");
                if (comboBoxRegion.Items.Contains(region) && !string.IsNullOrEmpty(region))
                {
                    comboBoxRegion.Text = region;
                }
                else
                {
                    comboBoxRegion.SelectedIndex = 0;
                }
            }

            if (scraperPlatform == "EmuMovies")
            {
                maxThreads = 2;
            }

            if (comboBoxMaxThreads.Items.Count > 0)
            {
                comboBoxMaxThreads.Items.Clear();
            }

            for (int i = 1; i <= maxThreads; i++)
            {
                comboBoxMaxThreads.Items.Add(i.ToString());
            }

            int threads;
            int.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "HideNonGame"), out threads);

            if (threads == 0 || threads > maxThreads)
            {
                comboBoxMaxThreads.SelectedIndex = maxThreads - 1;
            }
            else
            {
                comboBoxMaxThreads.SelectedIndex = threads;
            }

            if (saveRequired == true)
            {
                SaveGameOptions();
            }

        }
        private async Task<int> GetMaxThreads()
        {
            int maxThreads = 1;
            string userName = textboxScraperName.Text;
            string userPassword = textboxScraperPassword.Text;
            if (scraperPlatform == "ScreenScraper")
            {
                maxThreads = await API_ScreenScraper.GetMaxScrap(userName, userPassword);
                return maxThreads;
            }

            if (scraperPlatform == "EmuMovies")
            {
                maxThreads = 2;
            }
            return 1;
        }

        private void checkBoxHideNonGame_CheckedChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
        }

        private void checkBoxNoZZZ_CheckedChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
        }

        private void checkBoxScrapeByGameID_CheckedChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
        }
    }
}
