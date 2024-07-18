using System;
using System.Text;
using System.Windows.Forms;

namespace GamelistManager.control
{
    public partial class MediaPathSetup : UserControl
    {
        public MediaPathSetup()
        {
            InitializeComponent();
        }

        private void UserControl1_Load(object sender, EventArgs e)
        {
            LoadMediaPaths();
        }

        private void LoadMediaPaths()
        {
            var mediaPaths = MediaPathHelper.GetMediaPaths();

            foreach (Control control in this.Controls)
            {
                if (control is TextBox textBox)
                {
                    string name = textBox.Name.Replace("textbox","").ToLower();
                    string value = mediaPaths[name];
                    textBox.Text = value;
                }
            }
        }

        private void buttondefault_Click(object sender, EventArgs e)
        {
            RegistryManager.WriteRegistryValue(null, "MediaPaths", string.Empty);
            LoadMediaPaths();
            buttonSave.Enabled = true;
            buttonSave.Text = "Save";
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveMediaPaths();
            buttonSave.Enabled = false;
            buttonSave.Text = "Saved";
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void textbox_TextChanged(object sender, EventArgs e)
        {
            buttonSave.Enabled = true;
            buttonSave.Text = "Save";
        }

        public void SaveMediaPaths()
        {
            var mediaTypes = ScraperMediaTypes.GetMediaTypesOnly();
            StringBuilder regValue = new StringBuilder();

            foreach (Control control in this.Controls)
            {
                if (control is TextBox textBox)
                {
                    string mediaType = textBox.Name.Replace("textbox", "").ToLower();
                    string value = textBox.Text;
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (regValue.Length > 0)
                        {
                            regValue.Append(",");
                        }
                        regValue.Append($"{mediaType}={value}");
                    }
                }

                RegistryManager.WriteRegistryValue(null, "MediaPaths", regValue.ToString());
            }

        }

    }
}