using System;
using System.Windows.Forms;

namespace GamelistManager
{
    public partial class BatoceraHostSetup : UserControl
    {
        public BatoceraHostSetup()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string hostName = BatoceraHostName.Text;
            string userID = BatoceraUserID.Text;
            string userPassword = BatoceraUserPassword.Text;

            RegistryManager.SaveScraperSettings(null,"HostName", hostName);
            bool result = CredentialManager.SaveCredentials(hostName, userID, userPassword);

            // false means they did not save.  what are the chances?
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void BatoceraHostSetup_Load(object sender, EventArgs e)
        {
            string hostName = RegistryManager.ReadRegistryValue(null,"HostName");

            if (string.IsNullOrEmpty(hostName))
            {
                return;
            }

            (string userName, string userPassword) = CredentialManager.GetCredentials(hostName);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                return;
            }
            BatoceraHostName.Text = hostName;
            BatoceraUserID.Text = userName;
            BatoceraUserPassword.Text = userPassword;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                BatoceraUserPassword.UseSystemPasswordChar = true;
            }
            else
            {
                BatoceraUserPassword.UseSystemPasswordChar = false;
            }
        }
    }
}

