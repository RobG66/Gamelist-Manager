using System;
using System.IO;
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
            LoadTextBoxes();
            
        }

        private void LoadTextBoxes()
        {
            var mediaTypes = global::GamelistManager.SharedData.MediaTypes;

            foreach (var mediaType in mediaTypes)
            {
                // Construct the control name based on the media type
                string controlName = "textbox" + mediaType.ToLower();

                // Find the control by name
                var controls = Controls.Find(controlName, true);
                Console.WriteLine(controlName);
                if (controls.Length > 0 && controls[0] is TextBox textBox)
                {
                    // Set the text of the TextBox to the corresponding path
                    string currentValue = global::GamelistManager.SharedData.GetMediaTypePath(mediaType);
                    if (!string.IsNullOrEmpty(currentValue))
                    {
                        textBox.Text = global::GamelistManager.SharedData.GetMediaTypePath(mediaType);
                    }
                    else
                    {
                        textBox.Text = "./images";
                    }
                }

            }
        }


        private void buttondefault_Click(object sender, EventArgs e)
        {
            RegistryManager.WriteRegistryValue(null,"MediaPaths", string.Empty);
            SharedData.ConfigureMediaPaths();
            LoadTextBoxes();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var mediaType in global::GamelistManager.SharedData.MediaTypes)
            {
                string textBoxName = $"textbox{mediaType}".ToLower();
                Control[] controls = this.Controls.Find(textBoxName, true);
                if (controls.Length > 0 && controls[0] is TextBox textBox)
                {
                    string textboxValue = textBox.Text.Trim();
                    global::GamelistManager.SharedData.SetMediaTypePath(mediaType, textboxValue);
                    if (sb.Length > 0)
                    {
                        sb.Append(",");
                    }
                    sb.Append($"{mediaType}={textboxValue}");
                }
            }

            RegistryManager.WriteRegistryValue(null, "MediaPaths", sb.ToString());
            
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
    }
}
