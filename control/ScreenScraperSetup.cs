using CredentialManagement;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace GamelistManager.control
{
    public partial class ScreenScraperSetup : UserControl
    {

        public ScreenScraperSetup()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
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
                button1.Enabled = false;
            }

            SaveRegion();
            SaveLanguage();
            SaveImageSource();
            SaveBoxSource();
            SaveLogoSource();
            SaveMaxThreads();

            this.Enabled = true;
        }

        private void SaveMaxThreads()
        {
            string maxThreadsValue = comboBox_MaxThreads.Text;
            RegistryManager.SaveRegistryValue("MaxThreads", maxThreadsValue);
        }

        private void SaveLogoSource()
        {
            string boxValue = comboBox_LogoSource.Text;
            RegistryManager.SaveRegistryValue("LogoSource", boxValue);
        }

        private void SaveImageSource()
        {
            string boxValue = comboBox_ImageSource.Text;
            RegistryManager.SaveRegistryValue("ImageSource", boxValue);
        }

        private void SaveBoxSource()
        {
            string boxValue = comboBox_BoxSource.Text;
            RegistryManager.SaveRegistryValue("BoxSource", boxValue);
        }

        private void SaveLanguage()
        {
            string boxValue = comboBox_Language.Text;
            string language = boxValue.Split(':')[0].Trim();
            RegistryManager.SaveRegistryValue("Language", language);
        }

        private void SaveRegion()
        {
            string boxValue = comboBox_Region.Text;
            string region = boxValue.Split(':')[0].Trim();
            RegistryManager.SaveRegistryValue("Region", region);
        }

        private void SaveCredentials()
        {
            string userID = textbox_ScreenScraperName.Text;
            string userPassword = textbox_ScreenScraperPassword.Text;

            Credential CredentialManager = new Credential()
            {
                Target = "ScreenScraper",
                Username = userID,
                Password = userPassword,
                PersistanceType = PersistanceType.LocalComputer,
            };
            CredentialManager.Save();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void ScreenScraperID_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = true;
        }

        private void ScreenScraperPassword_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textbox_ScreenScraperPassword.UseSystemPasswordChar = false;
            }
            else
            {
                textbox_ScreenScraperPassword.UseSystemPasswordChar = true;
            }
        }

        private async void ScreenScraperSetup_Load(object sender, EventArgs e)
        {
            (string userName, string userPassword) = CredentialManager.GetCredentials("ScreenScraper");

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                return;
            }
            textbox_ScreenScraperName.Text = userName;
            textbox_ScreenScraperPassword.Text = userPassword;

            string boxSource = RegistryManager.ReadRegistryValue("BoxSource");
            string imageSource = RegistryManager.ReadRegistryValue("ImageSource");
            string logoSource = RegistryManager.ReadRegistryValue("LogoSource");
            string region = RegistryManager.ReadRegistryValue("Region");
            string language = RegistryManager.ReadRegistryValue("Language");

            comboBox_BoxSource.Text = boxSource;
            comboBox_ImageSource.Text = imageSource;
            comboBox_LogoSource.Text = logoSource;

            int index = comboBox_Language.FindString($"{language}:");
            if (index != -1)
            {
                comboBox_Language.SelectedIndex = index;
            }
            else
            {
                comboBox_Language.SelectedIndex = 0;
            }

            index = comboBox_Region.FindString($"{region}:");
            if (index != -1)
            {
                comboBox_Region.SelectedIndex = index;
            }
            else
            {
                comboBox_Region.SelectedIndex = 37;
            }

            // Setup max threads, show the biggest value by default
            int maxThreads = 1;
            XmlNode xmlResponse = await CheckUser();
            if (xmlResponse != null)
            {
                XmlNode maxThreadsNode = xmlResponse.SelectSingleNode("//ssuser/maxthreads");
                maxThreads = int.Parse(maxThreadsNode.InnerText);
            }

            if (comboBox_MaxThreads.Items.Count > 0)
            {
                comboBox_MaxThreads.Items.Clear();
            }
            for (int i = 1; i <= maxThreads; i++)
            {
                comboBox_MaxThreads.Items.Add(i.ToString());
            }
            comboBox_MaxThreads.SelectedIndex = comboBox_MaxThreads.Items.Count - 1;
        }

        private async Task<XmlNode> CheckUser()
        {
            string userId = textbox_ScreenScraperName.Text;
            string userPassword = textbox_ScreenScraperPassword.Text;
            string url = $"https://api.screenscraper.fr/api2/ssuserInfos.php?devid=xxx&devpassword=yyy&softname=zzz&output=xml&ssid={userId}&sspassword={userPassword}";
            XMLResponder responder = new XMLResponder();
            XmlNode xmlResponse = await responder.GetXMLResponseAsync(url);
            return xmlResponse;
        }


        private void comboBox_ImageSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = true;
        }

        private void comboBox_BoxSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = true;
        }

        private void comboBox_LogoSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = true;
        }
    }
}
