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



        private async void Button1_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            XmlNode xmlResonse = await CheckScraperCredentialsAsync();

            if (xmlResonse == null)
            {
                MessageBox.Show("The credentials do not appear to be valid and were not saved!", "Credential Check", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                SaveCredentials();
                buttonSave.Enabled = false;
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
            RegistryManager.SaveScraperSettings(scraperPlatform,"HideNonGame", hideNonGame.ToString());
            RegistryManager.SaveScraperSettings(scraperPlatform,"NoZZZ", noZZZ.ToString());
            RegistryManager.SaveScraperSettings(scraperPlatform,"ScrapeByGameID", scrapeByGameID.ToString());
        }

        private void SaveMaxThreads()
        {
            string maxThreadsValue = comboBoxMaxThreads.Text;
            RegistryManager.SaveScraperSettings(scraperPlatform, "MaxThreads", maxThreadsValue);
        }

        private void SaveLogoSource()
        {
            string boxValue = comboBoxLogoSource.Text;
            RegistryManager.SaveScraperSettings(scraperPlatform,"LogoSource", boxValue);
        }

        private void SaveImageSource()
        {
            string boxValue = comboBoxImageSource.Text;
            RegistryManager.SaveScraperSettings(scraperPlatform,"ImageSource", boxValue);
        }

        private void SaveBoxSource()
        {
            string boxValue = comboBoxBoxSource.Text;
            RegistryManager.SaveScraperSettings(scraperPlatform,"BoxSource", boxValue);
        }

        private void SaveLanguage()
        {
            if (scraperPlatform == "EmuMovies")
            {
                return;
            }
            string boxValue = comboBoxLanguage.Text;
            string language = boxValue.Split(':')[0].Trim();
            RegistryManager.SaveScraperSettings(scraperPlatform,"Language", language);
        }

        private void SaveRegion()
        {
            if (scraperPlatform == "EmuMovies")
            {
                return;
            }
            string boxValue = comboBoxRegion.Text;
            string region = boxValue.Split(':')[0].Trim();
            RegistryManager.SaveScraperSettings(scraperPlatform,"Region", region);
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

        private async Task<XmlNode> CheckScraperCredentialsAsync()
        {
            if (scraperPlatform == "ScreenScraper")
            {
                string userId = textboxScraperName.Text;
                string userPassword = textboxScraperPassword.Text;
                string url = $"https://api.screenscraper.fr/api2/ssuserInfos.php?devid=xxx&devpassword=yyy&softname=zzz&output=xml&ssid={userId}&sspassword={userPassword}";
                XMLResponder responder = new XMLResponder();
                XmlNode xmlResponse = await responder.GetXMLResponseAsync(url);
                return xmlResponse;
            }
            return null;
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
            Cursor.Current = Cursors.WaitCursor;
            this.Enabled = false;

            (string userName, string userPassword) = CredentialManager.GetCredentials(scraperPlatform);

            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userPassword))
            {
                textboxScraperName.Text = userName;
                textboxScraperPassword.Text = userPassword;
            }

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


            if (!string.IsNullOrEmpty(boxSource))
            {
                comboBoxBoxSource.Text = boxSource;
            }
            else
            {
                comboBoxBoxSource.SelectedIndex = 0;
            }

            if (!string.IsNullOrEmpty(imageSource))
            {
                comboBoxImageSource.Text = imageSource;
            }
            else
            {
                comboBoxImageSource.SelectedIndex = 0;
            }

            if (!string.IsNullOrEmpty(logoSource))
            {
                comboBoxLogoSource.Text = logoSource;
            }
            else
            {
                comboBoxLogoSource.SelectedIndex = 0;
            }

            // Setup max threads, show the biggest value by default
            int maxThreads = 1;
            XmlNode xmlResponse = await CheckScraperCredentialsAsync();
            if (xmlResponse != null)
            {
                XmlNode maxThreadsNode = xmlResponse.SelectSingleNode("//ssuser/maxthreads");
                maxThreads = int.Parse(maxThreadsNode.InnerText);
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

            comboBoxMaxThreads.SelectedIndex = comboBoxMaxThreads.Items.Count - 1;

            this.Enabled = true;
            Cursor.Current = Cursors.Default;

        }
    }
}
