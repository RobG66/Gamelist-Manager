using CredentialManagement;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;


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

        private void SaveGameOptions()
        {

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

            buttonSave.Enabled = false;
            useDefaults = false;
            this.Enabled = true;
        }

        private void SaveNonGameOptions()
        {
            bool scrapeByGameID = checkBoxScrapeByGameID.Checked;
            bool hideNonGame = checkBoxHideNonGame.Checked;
            bool noZZZ = checkBoxNoZZZ.Checked;
            RegistryManager.WriteRegistryValue(scraperPlatform, "HideNonGame", hideNonGame.ToString());
            RegistryManager.WriteRegistryValue(scraperPlatform, "NoZZZ", noZZZ.ToString());
            RegistryManager.WriteRegistryValue(scraperPlatform, "ScrapeByGameID", scrapeByGameID.ToString());
        }


     
        private void SaveLanguage()
        {
            if (scraperPlatform != "ScreenScraper")
            {
                return;
            }
            string boxValue = comboBoxLanguage.Text;
            string language = boxValue.Split(':')[0].Trim();
            RegistryManager.WriteRegistryValue(scraperPlatform, "Language", language);
        }

        private void SaveRegion()
        {
            if (scraperPlatform != "ScreenScraper")
            {
                return;
            }
            string boxValue = comboBoxRegion.Text;
            string region = boxValue.Split(':')[0].Trim();
            RegistryManager.WriteRegistryValue(scraperPlatform, "Region", region);
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
                API_ScreenScraper aPI_ScreenScraper = new API_ScreenScraper();
                XmlNode xmlResponse = await aPI_ScreenScraper.AuthenticateScreenScraperAsync(username, password);
                                
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
                API_EmuMovies aPI_EmuMovies = new API_EmuMovies();
                string result = await aPI_EmuMovies.AuthenticateEmuMoviesAsync(username, password);
                if (!string.IsNullOrEmpty(result))
                {
                    return true;
                }

            }
            return false;
        }

        private async void ScraperSetup_Load(object sender, EventArgs e)
        {

            if (scraperPlatform != "ScreenScraper")
            {
                panelOptions.Visible = false;    
            }
            else
            {
                panelOptions.Visible = true;
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

            if (file == null) { return; }

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
                    case "Regions":
                        comboBoxRegion.Items.AddRange(sectionValues.Values.ToArray());
                        break;
                    case "Languages":
                        comboBoxLanguage.Items.AddRange(sectionValues.Values.ToArray());
                        break;
              
                }
            }
        }
        private void SetDefaultOrSavedOptions()
        {
            bool saveRequired = false;
          
            if (scraperPlatform == "ScreenScraper")
            {

                bool.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "HideNonGame"), out bool hideNonGame);
                bool.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "NoZZZ"), out bool noZZZ);
                bool.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "ScrapeByGameID"), out bool scrapeByGameID);
                checkBoxHideNonGame.Checked = hideNonGame;
                checkBoxNoZZZ.Checked = noZZZ;
                checkBoxScrapeByGameID.Checked = scrapeByGameID;

                string language = RegistryManager.ReadRegistryValue(scraperPlatform, "Language");
                if (!string.IsNullOrEmpty(language) && comboBoxLanguage.Items.Contains(language))
                {
                    comboBoxLanguage.Text = language;
                }
                else
                {
                    comboBoxLanguage.SelectedIndex = 0;
                }
                
                string region = RegistryManager.ReadRegistryValue(scraperPlatform, "Region");
                if (!string.IsNullOrEmpty(region) && comboBoxRegion.Items.Contains(region))
                {
                    comboBoxRegion.Text = region;
                }
                else
                {
                    comboBoxRegion.SelectedIndex = 0;
                }
                
            }

            if (saveRequired == true)
            {
                SaveGameOptions();
            }

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
