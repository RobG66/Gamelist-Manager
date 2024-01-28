using CredentialManagement;
using System;
using System.Windows.Forms;

namespace GamelistManager.control
{
    public partial class ScreenScraperSetup : UserControl
    {
        public ScreenScraperSetup()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string userID = textbox_ScreenScraperName.Text;
            string userPassword = textbox_ScreenScraperPassword.Text;

            Credential CredentialManager = new Credential()
            {
                Target = "ScreenScraper",
                Username = userID,
                Password = userPassword,
                PersistanceType = PersistanceType.LocalComputer, // Choose appropriate persistence type
            };
            CredentialManager.Save();
            button1.Enabled = false;
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

        private void ScreenScraperSetup_Load(object sender, EventArgs e)
        {
            (string userName, string userPassword) = CredentialManager.GetCredentials("ScreenScraper");

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                return;
            }
            textbox_ScreenScraperName.Text = userName;
            textbox_ScreenScraperPassword.Text = userPassword;
        }
    }
}
