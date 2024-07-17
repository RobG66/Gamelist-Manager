using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamelistManager.form
{
    public partial class MediaSearch : Form
    {
        public MediaSearch()
        {
            InitializeComponent();
        }

        private void checkBoxSame_CheckedChanged(object sender, EventArgs e)
        {
            buttonDestination.Enabled = !checkBoxSame.Checked;
            textboxDestination.Enabled = !checkBoxSame.Checked;

        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            if (checkBoxExisting.Checked)
            {
                FindExisting();
            }
        }

        private void FindExisting()
        {
            DialogResult result = MessageBox.Show("This will find any existing media and reassociate it with the matching game.\n\nDo you want to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            string[] knownImageTypes = {
                "image",
                "thumb",
                "boxback",
                "fanart",
                "marquee",
                "map",
                "cartridge"
                };

            foreach (string imageType in knownImageTypes) { 
                Reassociate(SharedData.GetMediaTypePath(imageType), knownImageTypes);
            }

            string[] knownVideoTypes = { "video" };
            Reassociate(SharedData.GetMediaTypePath("videos"), knownVideoTypes);

            string[] knownManualTypes = { "manual" };
            Reassociate(SharedData.GetMediaTypePath("manuals"), knownManualTypes);

            MessageBox.Show("Media reassociation is completed.\n\nPlease remember to save!", "Completed!", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void Reassociate(string folder, string[] types)
        {
            string dir = Path.Combine(Path.GetDirectoryName(SharedData.XMLFilename), folder);
            string[] files = Directory.GetFiles(dir);
            files = files
               .Select(file => $"./{folder}/{Path.GetFileName(file)}")
               .ToArray();

            var dataTable = SharedData.DataSet.Tables["game"];
            var lockObject = new object();

            Parallel.ForEach(dataTable.AsEnumerable(), row =>
            {
                string romNameWithoutExtension = Path.GetFileNameWithoutExtension(row["path"].ToString());
                foreach (string fileType in types)
                {
                    string pattern = $"./{folder}/{romNameWithoutExtension}-{fileType}";
                    string firstMatch = files
                        .FirstOrDefault(f => f.StartsWith(pattern, StringComparison.OrdinalIgnoreCase));

                    if (firstMatch != null)
                    {
                        string column = fileType == "thumb" ? "thumbnail" : fileType;

                        lock (lockObject)
                        {
                            row[column] = firstMatch;
                        }
                    }
                }
            });

            SharedData.DataSet.AcceptChanges();


        }


        private void buttonChooseSource_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select a folder";
                folderBrowserDialog.ShowNewFolderButton = true;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderBrowserDialog.SelectedPath;
                    textboxSource.Text = selectedPath;
                }
            }
        }

        private void checkBoxExisting_CheckedChanged(object sender, EventArgs e)
        {
            buttonChooseSource.Enabled = !checkBoxExisting.Checked;
            buttonDestination.Enabled = !checkBoxExisting.Checked;
            textboxDestination.Enabled= !checkBoxExisting.Checked;
            textboxSource.Enabled = !checkBoxExisting.Checked;
            checkBoxSame.Enabled = !checkBoxExisting.Checked;
            checkBoxSearchDefault.Enabled = checkBoxExisting.Checked;
        }
    }
}
