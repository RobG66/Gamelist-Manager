using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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
            checkBoxDoNotOverwrite.Enabled = !checkBoxSame.Checked;
            checkBoxDefaultPath.Enabled = !checkBoxSame.Checked;
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            dataGridViewImages.Rows.Clear();
            if (checkBoxExisting.Checked)
            {
                FindExisting();
            }
            else
            {
                FindNew();
            }
            labelCount.Visible = true;
            if (dataGridViewImages.RowCount > 0)
            {
                labelCount.Text = $"Found {dataGridViewImages.RowCount.ToString()} Images";
                contextMenuStrip1.Enabled = true;
            }
            else
            {
                labelCount.Text = "No Images Found";
            }

        }

        private void FindExisting()
        {
            var allMediaTypes = ScraperMediaTypes.GetMediaTypesOnly();
            var allMediapaths = MediaPathHelper.GetMediaPaths();

            var rows = new ConcurrentBag<Tuple<string, string, string>>();

            buttonSearch.Enabled = false;
            foreach (var mediaPath in allMediapaths.Values)
            {
                rows = FindMedia(mediaPath, allMediaTypes, false);
                foreach (var tuple in rows)
                {
                    var newRow = new DataGridViewRow();
                    newRow.CreateCells(dataGridViewImages);
                    newRow.Cells[0].Value = tuple.Item1; // File path
                    newRow.Cells[1].Value = tuple.Item2; // Media type
                    newRow.Cells[2].Value = tuple.Item3; // First match
                    dataGridViewImages.Rows.Add(newRow);
                }
            }
            buttonSearch.Enabled = true;
         
        }

        private ConcurrentBag<Tuple<string, string, string>> FindMedia(string folder, IEnumerable<string> mediaTypes, bool useFuzzy)
        {
            string dir = Path.Combine(Path.GetDirectoryName(SharedData.XMLFilename), folder);
            if (!Directory.Exists(dir))
            {
                return null;
            }

            List<string> files = Directory.GetFiles(dir).Select(file => Path.GetFileName(file)).ToList();

            var dataTable = SharedData.DataSet.Tables["game"];
            var rowsToAdd = new ConcurrentBag<Tuple<string, string, string>>(); // Changed to Tuple

            Parallel.ForEach(dataTable.AsEnumerable(), row =>
            {
                string romNameWithoutExtension = Path.GetFileNameWithoutExtension(row["path"].ToString());
                string filePath = row["path"].ToString();
                foreach (string fileType in mediaTypes)
                {
                    string firstMatch = null;
                    if (!useFuzzy)
                    {
                        // Abbreviate thumbnail to thumb because that is what Batocera does
                        string abbreviatedType = fileType == "thumbnail" ? "thumb" : fileType;
                        string pattern = $"{romNameWithoutExtension}-{abbreviatedType}";

                        firstMatch = files
                            .FirstOrDefault(f => f.StartsWith(pattern, StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {                        
                        FuzzySearchHelper fuzzySearchHelper = new FuzzySearchHelper();
                        firstMatch = fuzzySearchHelper.FuzzySearch(romNameWithoutExtension, files);
                    }

                    if (!string.IsNullOrEmpty(firstMatch))
                    {
                        // Store information as Tuple
                        var newRow = Tuple.Create(filePath, fileType, firstMatch);
                        rowsToAdd.Add(newRow);
                    }
                }
            });

            return rowsToAdd;
        }


        private void FindNew()
        {

            var rows = new ConcurrentBag<Tuple<string, string, string>>(); // Changed to Tuple

            var mediatype = comboBoxMediaTypes.Text;
            string mediaPath = textboxSource.Text;

            rows = FindMedia(mediaPath, new string[] { mediatype }, true);

            foreach (var tuple in rows)
            {
                var newRow = new DataGridViewRow();
                newRow.CreateCells(dataGridViewImages);
                newRow.Cells[0].Value = tuple.Item1; // File path
                newRow.Cells[1].Value = tuple.Item2; // Media type
                newRow.Cells[2].Value = tuple.Item3; // First match
                dataGridViewImages.Rows.Add(newRow);
            }

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
            panelsmaller.Enabled = !checkBoxExisting.Checked;
        }

        private void MediaSearch_Load(object sender, EventArgs e)
        {
            comboBoxMediaTypes.SelectedIndex = 0;
            checkBoxDefaultPath.Checked = true;
            checkBoxDoNotOverwrite.Checked = true;
            
            // Show system image if exists
            string romPath = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));
            System.Drawing.Image image = (Bitmap)Properties.Resources.ResourceManager.GetObject(romPath);
            if (image is System.Drawing.Image)
            {
                pictureBox1.Image = image;
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            buttonSave.Enabled = false;
            dataGridViewImages.Rows.Clear();
            labelCount.Visible = false;
            contextMenuStrip1.Enabled = false;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (dataGridViewImages.Rows.Count < 1)
            {
                return;
            }
        }

        private void checkBoxDefaultPath_CheckedChanged(object sender, EventArgs e)
        {
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);
            var mediaPaths = MediaPathHelper.GetMediaPaths();
            var key = comboBoxMediaTypes.Text.ToLower();
            var mediaPath = mediaPaths[key];
            string foldername = Path.GetFileName(mediaPath);
            string defaultPath = Path.Combine(parentFolderPath, foldername);

            if (checkBoxDefaultPath.Checked)
            {
                buttonDestination.Enabled = false;
                textboxDestination.Enabled = false;
                textboxDestination.Text = defaultPath;
            }
            else
            {
                buttonDestination.Enabled = true;
                textboxDestination.Enabled = true;
                textboxDestination.Text = "";
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkBoxDefaultPath.Checked)
            {
                string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);
                var mediaPaths = MediaPathHelper.GetMediaPaths();
                var key = comboBoxMediaTypes.Text.ToLower();
                var mediaPath = mediaPaths[key];
                string foldername = Path.GetFileName(mediaPath);
                string defaultPath = Path.Combine(parentFolderPath, foldername);
                textboxDestination.Text = defaultPath;
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (dataGridViewImages.SelectedRows.Count < 1)
            {
                MessageBox.Show("No rows selected!", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            for (int i = dataGridViewImages.SelectedRows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dataGridViewImages.SelectedRows[i];
                if (!row.IsNewRow) // Ensure not to remove the new row placeholder
                {
                    dataGridViewImages.Rows.Remove(row);
                }
            }



        }
    }
}
