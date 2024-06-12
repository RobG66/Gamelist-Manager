using CredentialManagement;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace GamelistManager.control
{
    public partial class ScraperSetup : Form
    {
        private string scraperPlatform;

        public ScraperSetup(string platform)
        {
            InitializeComponent();
            this.Text = $"{scraperPlatform} Setup";
            labelName.Text = $"{scraperPlatform} ID";
            labelPassword.Text = $"{scraperPlatform} Password";
            scraperPlatform = platform;
        }

        private async void SetDefaults()
        {

            comboBoxBoxSource.SelectedIndex = 0;
            comboBoxImageSource.SelectedIndex = 0;
            comboBoxLogoSource.SelectedIndex = 0;

            int maxThreads = await GetMaxThreads();

            if (comboBoxMaxThreads.Items.Count > 0)
            {
                comboBoxMaxThreads.Items.Clear();
            }

            for (int i = 1; i <= maxThreads; i++)
            {
                comboBoxMaxThreads.Items.Add(i.ToString());
            }

            comboBoxMaxThreads.SelectedIndex = comboBoxMaxThreads.Items.Count - 1;
        }


        private async void Button1_Click(object sender, EventArgs e)
        {

            this.Enabled = false;

            bool response = await CheckScraperCredentialsAsync();

            if (response != true)
            {
                this.Enabled = true;
                MessageBox.Show("The credentials do not appear to be valid and were not saved!", "Credential Check", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                SaveCredentials();
                buttonSave.Enabled = false;
            }

            if (panelOptions.Enabled == false)
            {
                panelOptions.Enabled = true;
                this.Enabled = true;
                SetDefaults();
            }

            if (scraperPlatform != "EmuMovies")
            {
                SaveRegion();
                SaveLanguage();
                SaveNonGameOptions();
            }

            SaveImageSource();
            SaveBoxSource();
            SaveLogoSource();
            SaveMaxThreads();
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
                XmlNode xmlResponse = await API_ScreenScraper.AuthenticateAsync(username, password);
                string text = xmlResponse.OuterXml;
                XmlNode userID = xmlResponse.SelectSingleNode("//ssuser/id");
                string remoteID = userID.InnerText;
                if (username.ToLower() == remoteID.ToLower())
                {
                    return true;
                }
            }

            if (scraperPlatform == "EmuMovies")
            {
                string result = await API_EmuMovies.AuthenticateAsync(username, password);
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
            (string userName, string userPassword) = CredentialManager.GetCredentials(scraperPlatform);

            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userPassword))
            {
                textboxScraperName.Text = userName;
                textboxScraperPassword.Text = userPassword;
                panelOptions.Enabled = true;
            }
            else
            {
                return;
            }

            Cursor.Current = Cursors.WaitCursor;
            this.Enabled = false;

            string boxSource = RegistryManager.ReadRegistryValue(scraperPlatform, "BoxSource");
            string imageSource = RegistryManager.ReadRegistryValue(scraperPlatform, "ImageSource");
            string logoSource = RegistryManager.ReadRegistryValue(scraperPlatform, "LogoSource");

            if (scraperPlatform == "ScreenScraper")
            {
                string region = RegistryManager.ReadRegistryValue(scraperPlatform, "Region");
                string language = RegistryManager.ReadRegistryValue(scraperPlatform, "Language");
                int index = comboBoxLanguage.FindString($"{language}:");
                if (index != -1)
                {
                    comboBoxLanguage.SelectedIndex = index;
                }
                else
                {
                    comboBoxLanguage.SelectedIndex = 0;
                }

                index = comboBoxRegion.FindString($"{region}:");
                if (index != -1)
                {
                    comboBoxRegion.SelectedIndex = index;
                }
                else
                {
                    comboBoxRegion.SelectedIndex = 37;
                }

                bool.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "HideNonGame"), out bool hideNonGame);
                bool.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "NoZZZ"), out bool noZZZ);
                bool.TryParse(RegistryManager.ReadRegistryValue(scraperPlatform, "ScrapeByGameID"), out bool scrapeByGameID);
                checkBoxHideNonGame.Checked = hideNonGame;
                checkBoxNoZZZ.Checked = noZZZ;
                checkBoxScrapeByGameID.Checked = scrapeByGameID;
            }
            else
            {
                comboBoxLanguage.Visible = false;
                comboBoxRegion.Visible = false;
                labelLanguage.Visible = false;
                labelRegion.Visible = false;
                checkBoxNoZZZ.Visible = false;
                checkBoxScrapeByGameID.Visible = false;
                checkBoxHideNonGame.Visible = false;

            }


            this.Enabled = true;
            Cursor.Current = Cursors.Default;

        }

        private void SetComboBoxValues()
        {
            string[] items;

            // Setting items for comboBoxImageSource
            items = new string[] {
                "Screenshot", "Title Screenshot", "Mix V1",
                "Mix V2", "Box 2D", "Box 3D", "Fan Art"
            };
            comboBoxImageSource.Items.AddRange(items);

            // Setting items for comboBoxBoxSource
            items = new string[] {
                "Box 2D", "Box 3D"
            };
            comboBoxBoxSource.Items.AddRange(items);

            // Setting items for comboBoxLogoSource
            items = new string[] {
                "Wheel", "Marquee"
            };
            comboBoxLogoSource.Items.AddRange(items);

            // Setting items for comboBoxRegion
            items = new string[] {
                "de: Germany",
                "asi: Asia",
                "au: Australia",
                "br: Brazil",
                "bg: Bulgaria",
                "ca: Canada",
                "cl: Chile",
                "cn: China",
                "kr: Korea",
                "dk: Denmark",
                "sp: Spain",
                "eu: Europe",
                "fi: Finland",
                "fr: France",
                "gr: Greece",
                "hu: Hungary",
                "il: Israel",
                "it: Italy",
                "jp: Japan",
                "kw: Kuwait",
                "wor: World",
                "mor: Middle East",
                "no: Norway",
                "nz: New Zealand",
                "oce: Oceania",
                "nl: Netherlands",
                "pe: Peru",
                "pl: Poland",
                "pt: Portugal",
                "cz: Czech Republic",
                "uk: United Kingdom",
                "ru: Russia",
                "sk: Slovakia",
                "se: Sweden",
                "tw: Taiwan",
                "tr: Turkey",
                "us: USA",
                "ss: ScreenScraper"
            };
            comboBoxRegion.Items.AddRange(items);

            // Setting items for comboBoxLanguage
            items = new string[] {
                "en: English",
                "de: German",
                "zh: Chinese",
                "ko: Korean",
                "da: Danish",
                "es: Spanish",
                "fi: Finnish",
                "fr: French",
                "hu: Hungarian",
                "it: Italian",
                "ja: Japanese",
                "nl: Dutch",
                "no: Norwegian",
                "pl: Polish",
                "pt: Portuguese",
                "ru: Russian",
                "sk: Slovakian",
                "sv: Swedish",
                "cz: Czech",
                "tr: Turkish"
            };
            comboBoxLanguage.Items.AddRange(items);


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
    }
}
