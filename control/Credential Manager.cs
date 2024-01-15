using CredentialManagement;
using System;
using System.Net;
using System.Windows.Forms;

namespace GamelistManager
{
    public partial class CredentialManager : UserControl
    {
        public CredentialManager()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string hostName = BatoceraHostName.Text;
            string userID = BatoceraUserID.Text;
            string userPassword = BatoceraUserPassword.Text;

            RegistryManager.SaveRegistryValue("HostName", hostName);

            Credential CredentialManager = new Credential()
            {
                Target = hostName,
                Username = userID,
                Password = userPassword,
                PersistanceType = PersistanceType.LocalComputer, // Choose appropriate persistence type
            };
            CredentialManager.Save();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MenuStrip menuStrip = (MenuStrip)ParentForm.Controls["menuStrip1"];
            menuStrip.Enabled = true;
            this.Dispose();
        }

        private void UserControl1_Load(object sender, EventArgs e)
        {
            string hostName = RegistryManager.ReadRegistryValue("HostName");

            if (hostName != null && hostName != string.Empty)
            {
                if (TryReadCredentials(hostName, out NetworkCredential retrievedCredentials))
                {
                    BatoceraHostName.Text = hostName;
                    BatoceraUserID.Text = retrievedCredentials.UserName;
                    BatoceraUserPassword.Text = retrievedCredentials.Password;
                }
            }
        }

        public bool TryReadCredentials(string target, out NetworkCredential credential)
        {
            Credential cred = new Credential { Target = target };
            if (cred.Load())
            {
                credential = new NetworkCredential(cred.Username, cred.Password);
                return true;
            }

            credential = null;
            return false;
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
