using CredentialManagement;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace GamelistManager.control
{
    public partial class ScreenScraperSetup : Form
    {

        public ScreenScraperSetup()
        {
            InitializeComponent();
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            //ScraperForm scraperForm = new ScraperForm();
            this.Enabled = false;

            XmlNode xmlResonse = await CheckUser();

            if (xmlResonse == null)
            {
                MessageBox.Show("The credentials do not appear to be valid and were not saved!", "Credential Check", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                SaveCredentials();
                buttonSave.Enabled = false;
            }

            SaveRegion();
            SaveLanguage();
            SaveImageSource();
            SaveBoxSource();
            SaveLogoSource();
            SaveMaxThreads();
            SaveNonGameOptions();

            this.Enabled = true;
        }


        private void SaveNonGameOptions()
        {
            bool scrapeByGameID = checkBoxScrapeByGameID.Checked;
            bool hideNonGame = checkBoxHideNonGame.Checked;
            bool noZZZ = checkBoxNoZZZ.Checked;
            RegistryManager.SaveRegistryValue("HideNonGame", hideNonGame.ToString());
            RegistryManager.SaveRegistryValue("NoZZZ", noZZZ.ToString());
            RegistryManager.SaveRegistryValue("ScrapeByGameID", scrapeByGameID.ToString());
        }

        private void SaveMaxThreads()
        {
            string maxThreadsValue = comboBoxMaxThreads.Text;
            RegistryManager.SaveRegistryValue("MaxThreads", maxThreadsValue);
        }

        private void SaveLogoSource()
        {
            string boxValue = comboBoxLogoSource.Text;
            RegistryManager.SaveRegistryValue("LogoSource", boxValue);
        }

        private void SaveImageSource()
        {
            string boxValue = comboBoxImageSource.Text;
            RegistryManager.SaveRegistryValue("ImageSource", boxValue);
        }

        private void SaveBoxSource()
        {
            string boxValue = comboBoxBoxSource.Text;
            RegistryManager.SaveRegistryValue("BoxSource", boxValue);
        }

        private void SaveLanguage()
        {
            string boxValue = comboBoxLanguage.Text;
            string language = boxValue.Split(':')[0].Trim();
            RegistryManager.SaveRegistryValue("Language", language);
        }

        private void SaveRegion()
        {
            string boxValue = comboBoxRegion.Text;
            string region = boxValue.Split(':')[0].Trim();
            RegistryManager.SaveRegistryValue("Region", region);
        }

        private void SaveCredentials()
        {
            string userID = textboxScreenScraperName.Text;
            string userPassword = textboxScreenScraperPassword.Text;

            Credential CredentialManager = new Credential()
            {
                Target = "ScreenScraper",
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

        private void ScreenScraperPassword_TextChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkboxShowPassword.Checked)
            {
                textboxScreenScraperPassword.UseSystemPasswordChar = false;
            }
            else
            {
                textboxScreenScraperPassword.UseSystemPasswordChar = true;
            }
        }

        private async void ScreenScraperSetup_Load(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            this.Enabled = false;
            (string userName, string userPassword) = CredentialManager.GetCredentials("ScreenScraper");

            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userPassword))
            {
                textboxScreenScraperName.Text = userName;
                textboxScreenScraperPassword.Text = userPassword;
            }

            string boxSource = RegistryManager.ReadRegistryValue("BoxSource");
            string imageSource = RegistryManager.ReadRegistryValue("ImageSource");
            string logoSource = RegistryManager.ReadRegistryValue("LogoSource");
            string region = RegistryManager.ReadRegistryValue("Region");
            string language = RegistryManager.ReadRegistryValue("Language");

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

            bool.TryParse(RegistryManager.ReadRegistryValue("HideNonGame"), out bool hideNonGame);
            bool.TryParse(RegistryManager.ReadRegistryValue("NoZZZ"), out bool noZZZ);
            bool.TryParse(RegistryManager.ReadRegistryValue("ScrapeByGameID"), out bool scrapeByGameID);

            checkBoxHideNonGame.Checked = hideNonGame;
            checkBoxNoZZZ.Checked = noZZZ;
            checkBoxScrapeByGameID.Checked = scrapeByGameID;

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

            // Setup max threads, show the biggest value by default
            int maxThreads = 1;
            XmlNode xmlResponse = await CheckUser();
            if (xmlResponse != null)
            {
                XmlNode maxThreadsNode = xmlResponse.SelectSingleNode("//ssuser/maxthreads");
                maxThreads = int.Parse(maxThreadsNode.InnerText);
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

        private async Task<XmlNode> CheckUser()
        {
            string userId = textboxScreenScraperName.Text;
            string userPassword = textboxScreenScraperPassword.Text;
            string url = $"https://api.screenscraper.fr/api2/ssuserInfos.php?devid=xxx&devpassword=yyy&softname=zzz&output=xml&ssid={userId}&sspassword={userPassword}";
            XMLResponder responder = new XMLResponder();
            XmlNode xmlResponse = await responder.GetXMLResponseAsync(url);
            return xmlResponse;
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


    }
}
