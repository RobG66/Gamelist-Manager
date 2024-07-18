using System;
using System.Collections.Generic;
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

            var allMediaTypes = ScraperMediaTypes.GetMediaTypesOnly();
            var allMediapaths = MediaPathHelper.GetMediaPaths();

            foreach (var mediaPath in allMediapaths.Values) { 
                Reassociate(mediaPath, allMediaTypes);
            }
         
            MessageBox.Show("Media reassociation is completed.\n\nPlease remember to save!", "Completed!", MessageBoxButtons.OK, MessageBoxIcon.Information);
         
        }

        private void Reassociate(string folder, IEnumerable<string> types)
        {
            string dir = Path.Combine(Path.GetDirectoryName(SharedData.XMLFilename), folder);
            if (!Directory.Exists(dir))
            {
                return;
            }
            
            string[] files = Directory.GetFiles(dir);
            files = files
                .Select(file => $"{folder}/{Path.GetFileName(file)}")
                .ToArray();

            var dataTable = SharedData.DataSet.Tables["game"];
            var lockObject = new object();
            var rowsToAdd = new List<DataGridViewRow>();

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

                        // Create a new DataGridViewRow with the updated data
                        var newRow = new DataGridViewRow();
                        newRow.CreateCells(dataGridView1);
                        newRow.Cells[0].Value = row["path"];
                        //newRow.Cells[0].Value = romNameWithoutExtension;
                        newRow.Cells[1].Value = firstMatch; // Adjust the index based on your DataGridView columns
                        lock (rowsToAdd)
                        {
                            rowsToAdd.Add(newRow);
                        }
                    }
                }
            });

            // Add the rows to the DataGridView on the UI thread
            this.Invoke((MethodInvoker)delegate
            {
                dataGridView1.Rows.AddRange(rowsToAdd.ToArray());
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
