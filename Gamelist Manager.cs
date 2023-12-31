﻿using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using File = System.IO.File;


namespace GamelistManager
{
    public partial class GamelistManager : Form
    {
        private DataSet DataSet;
        private TableLayoutPanel TableLayoutPanel1;
        private string XMLFilename;
        private VideoView videoView1;
        private LibVLC libVLC;
        private MediaPlayer mediaPlayer;


        public GamelistManager()
        {
            InitializeComponent();
            DataSet = new DataSet();
        }

        private void LoadGamelistXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set the title of the dialog
            openFileDialog.Title = "Select a Gamelist";

            // Set the initial directory (optional)
            //openFileDialog.InitialDirectory = "C:\\";

            // Set the filter for the type of files to be displayed
            openFileDialog.Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*";

            // Set the default file extension (optional)
            openFileDialog.DefaultExt = "xml";

            // Display the dialog and check if the user clicked OK
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            XMLFilename = openFileDialog.FileName;
            bool successfulLoad = LoadXML(XMLFilename);

            if (successfulLoad == true)
            {
                LoadLastFilenamesToMenu();
            }
        }

        private void ApplyFilters(string visibilityFilter, string genreFilter)
        {
            DataTable dataTable = (DataTable)dataGridView1.DataSource;
            // Merge the genre filter with the visibility filter using "AND" if both are not empty
            string mergedFilter;
            if ((!string.IsNullOrEmpty(genreFilter) && !string.IsNullOrEmpty(visibilityFilter)))
            {
                mergedFilter = $"{genreFilter} AND {visibilityFilter}";
            }
            else
            {
                mergedFilter = (!string.IsNullOrEmpty(genreFilter) ? genreFilter : visibilityFilter);
            }

            // Set the modified RowFilter

            dataTable.DefaultView.RowFilter = mergedFilter;

        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count != 1) { return; }
            var rowIndex = dataGridView1.SelectedRows[0].Index;
            var columnIndex = dataGridView1.Columns["desc"].Index;
            object cellValue = dataGridView1.Rows[rowIndex].Cells[columnIndex].Value;
            string itemDescription = (cellValue != DBNull.Value) ? Convert.ToString(cellValue) : string.Empty;
            richTextBox1.Text = itemDescription;

            if (splitContainer2.Panel2Collapsed != true)
            {
                ShowMedia();
            }

        }


        private string GetGenreFilter()
        {

            // Get the DataTable bound to the DataGridView
            DataTable dataTable = (DataTable)dataGridView1.DataSource;

            // Get the current row filter
            string currentFilter = dataTable.DefaultView.RowFilter;

            // Check if there's an existing genre filter
            bool hasGenreFilter = currentFilter.Contains("genre");

            string genreFilter = string.Empty;

            if (hasGenreFilter)
            {
                // Extract the genre filter from the current filter
                genreFilter = currentFilter
                .Split(new[] { "AND", "OR" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(filter => filter.Contains("genre"));
            }

            return genreFilter;
        }

        private string Getvisibilityfilter()
        {
            // Get the DataTable bound to the DataGridView
            DataTable dataTable = (DataTable)dataGridView1.DataSource;

            // Get the current row filter
            string currentFilter = dataTable.DefaultView.RowFilter;

            // Check if there's an existing visibility filter
            bool hasVisibilityFilter = currentFilter.Contains("hidden");

            string visibilityFilter = string.Empty;
            if (hasVisibilityFilter)
            {
                // Extract the visibility filter from the current filter
                visibilityFilter = currentFilter
                .Split(new[] { "AND", "OR" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(filter => filter.Contains("hidden"));
            }

            return visibilityFilter;
        }

        private void ShowVisibleItemsOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showAllItemsToolStripMenuItem.Checked = false;
            ShowHiddenItemsOnlyToolStripMenuItem.Checked = false;
            ShowVisibleItemsOnlyToolStripMenuItem.Checked = true;
            string visibilityFilter = "hidden = false OR hidden IS NULL";
            string genreFilter = GetGenreFilter();
            ApplyFilters(visibilityFilter, genreFilter);
            UpdateCounters();
        }

        private void ShowHiddenItemsOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowVisibleItemsOnlyToolStripMenuItem.Checked = false;
            showAllItemsToolStripMenuItem.Checked = false;
            ShowHiddenItemsOnlyToolStripMenuItem.Checked = true;
            string visibilityFilter = "hidden = true";
            string genreFilter = GetGenreFilter();
            ApplyFilters(visibilityFilter, genreFilter);
            UpdateCounters();
        }

        private void ShowAllItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowVisibleItemsOnlyToolStripMenuItem.Checked = false;
            ShowHiddenItemsOnlyToolStripMenuItem.Checked = false;
            showAllItemsToolStripMenuItem.Checked = true;
            string visibilityFilter = string.Empty;
            string genreFilter = GetGenreFilter();
            ApplyFilters(visibilityFilter, genreFilter);

            UpdateCounters();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.gamelistmanager;
            splitContainer2.Panel2Collapsed = true;
            foreach (ToolStripMenuItem menuItem in menuStrip1.Items)
            {
                menuItem.Enabled = false;
            }
            fileToolStripMenuItem.Enabled = true;
            reloadGamelistxmlToolStripMenuItem.Enabled = false;
            saveFileToolStripMenuItem.Enabled = false;
            RemoteToolStripMenuItem.Enabled = true;
            LoadLastFilenamesToMenu();

        }

        private void ShowMediaToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.Checked == false)
            {
                splitContainer2.Panel2Collapsed = true;

                ClearTableLayoutPanel();
                return;
            }

            if (dataGridView1.SelectedRows.Count != 1) { return; }

            splitContainer2.Panel2Collapsed = false;

            ShowMedia();
        }

        private PictureBox MakePictureBox(string imagePath, string name)
        {
            System.Drawing.Image image;

            if (!File.Exists(imagePath))
            {
                image = Properties.Resources.missing;
            }
            else
            {
                try
                {
                    image = System.Drawing.Image.FromFile(imagePath);
                }
                catch
                {
                    image = Properties.Resources.loaderror;
                }
            }

            PictureBox pictureBox = new PictureBox
            {
                Image = image,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = name,
                Tag = imagePath,
                ContextMenuStrip = contextMenuStrip1
            };

            return pictureBox;
        }

        private Label MakeLabel(string text)
        {
            string labelName = text;
            Label label = new Label
            {
                Text = labelName,
                Font = new Font("Sego UI", 12, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter
            };
            return label;
        }


        private void ShowMedia()
        {
            if (TableLayoutPanel1 != null)
            {
                ClearTableLayoutPanel();
            }

            TableLayoutPanel1 = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                RowCount = 2,
                ColumnCount = 0
            };

            //splitContainer2.Panel2.Controls.Add(TableLayoutPanel1);
            panel2.Controls.Add(TableLayoutPanel1);

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

            int columnCount = 0;
            string parentFolderPath = Path.GetDirectoryName(XMLFilename);

            foreach (DataGridViewCell cell in selectedRow.Cells)
            {
                if (cell.OwningColumn.Tag == null || cell.OwningColumn.Tag.ToString().ToLower() != "image")
                {
                    continue;
                }

                if (cell.Value == null || string.IsNullOrEmpty(cell.Value.ToString()))
                {
                    continue;
                }

                string imageCellValue = cell.Value.ToString();
                string imagePath = Path.Combine(parentFolderPath, imageCellValue.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));
                string columnName = cell.OwningColumn.Name;

                PictureBox pictureBox = MakePictureBox(imagePath, columnName);
                TableLayoutPanel1.Controls.Add(pictureBox, columnCount, 1);

                string labelName = char.ToUpper(columnName[0]) + columnName.Substring(1);
                Label label = MakeLabel(labelName);
                TableLayoutPanel1.Controls.Add(label, columnCount, 0);

                TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent));
                columnCount++;
                TableLayoutPanel1.ColumnCount = columnCount;
            }

            string videoCellValue = selectedRow.Cells["video"].Value.ToString();
            string videoPath = Path.Combine(parentFolderPath, videoCellValue.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (System.IO.File.Exists(videoPath))
            {
                TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent));
                columnCount++;
                TableLayoutPanel1.ColumnCount = columnCount;

                Core.Initialize();

                libVLC = new LibVLC();
                mediaPlayer = new MediaPlayer(libVLC);

                videoView1 = new VideoView
                {
                    Dock = DockStyle.Fill
                };

                TableLayoutPanel1.Controls.Add(videoView1, columnCount, 1);

                Label videoLabel = MakeLabel("Video");
                TableLayoutPanel1.Controls.Add(videoLabel, columnCount, 0);
                // Add event show the video loops!
                mediaPlayer.EndReached += MediaPlayer_EndReached;

                try
                {
                    mediaPlayer.Play(new Media(libVLC, new Uri("file:///" + videoPath)));
                    videoView1.MediaPlayer = mediaPlayer;
                }
                catch (Exception ex)
                {
                    // Handle exceptions appropriately (log, show a message, etc.)
                    MessageBox.Show($"An error occurred loading the video: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Just in case there were no images or videos, show a 'no media' image 
            if (columnCount == 0)
            {
                PictureBox picturebox = MakePictureBox(string.Empty, "video");
                picturebox.Image = Properties.Resources.nomedia;
                TableLayoutPanel1.Controls.Add(picturebox, 0, 1);
                TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent));
                columnCount++;
            }

            // Apply style so everythhing is sized and spaced evently
            foreach (ColumnStyle columnStyle in TableLayoutPanel1.ColumnStyles)
            {
                columnStyle.SizeType = SizeType.Percent;
                columnStyle.Width = 100f / TableLayoutPanel1.ColumnCount; // Equal distribution for each column
            }


        }

        private void ClearTableLayoutPanel()
        {
            //The TableLayoutPanel is cleared each time a new item is selected
            //This is to ensure the proper display of new media elements
            //Which may be a different amount
            foreach (Control control in TableLayoutPanel1.Controls)

            {
                if (control is VideoView)
                {
                    //Properly dispose VLC
                    //Stop playback
                    mediaPlayer.Stop();

                    //Get rid of event for video looping
                    if (mediaPlayer != null)
                    {
                        mediaPlayer.EndReached -= MediaPlayer_EndReached;
                    }

                    // Dispose of resources
                    mediaPlayer.Dispose();
                    videoView1.Dispose();  // Dispose of the VideoView control
                    libVLC.Dispose();
                }
                else
                {
                    control.Dispose();
                }

            }

            TableLayoutPanel1.Dispose();
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            // Check if mediaPlayer is not null before using it
            if (mediaPlayer != null)
            {
                if (TableLayoutPanel1.InvokeRequired)
                {
                    TableLayoutPanel1.BeginInvoke(new Action(() =>
                    {
                        mediaPlayer.Stop();
                        mediaPlayer.Time = 0; // Seek to the beginning
                        mediaPlayer.Play();
                    }));
                }
                else
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Time = 0; // Seek to the beginning
                    mediaPlayer.Play();
                }
            }
        }


        private void UpdateCounters()
        {
            int hiddenItems = DataSet.Tables[0].AsEnumerable()
            .Count(row => row.Field<bool?>("hidden") == true);

            // Count rows where "hidden" is false
            int visibleItems = DataSet.Tables[0].AsEnumerable()
            .Count(row => row.Field<bool?>("hidden") != true);

            // Count rows where "hidden" is false
            int favoriteItems = DataSet.Tables[0].AsEnumerable()
            .Count(row => row.Field<bool?>("favorite") == true);

            int visibleRowCount = dataGridView1.Rows.Cast<DataGridViewRow>().Count(row => row.Visible);

            labelVisibleCount.Text = (visibleItems).ToString();
            labelHiddenCount.Text = (hiddenItems).ToString();
            labelShowingCount.Text = (visibleRowCount).ToString();
            labelFavoriteCount.Text = (favoriteItems).ToString();
        }

        private void ConvertColumnToBoolean(DataTable dataTable, string columnName)
        {
            // Check if the column exists in the DataTable
            if (!dataTable.Columns.Contains(columnName))
            {
                //If it does not exist, make it and then just return
                //Nothing to convert.........
                DataColumn HiddenColumn = new DataColumn(columnName, typeof(bool));
                dataTable.Columns.Add(HiddenColumn);
                return;
            }


            // Create a new boolean column
            DataColumn newHiddenColumn = new DataColumn("new_bool", typeof(bool));
            dataTable.Columns.Add(newHiddenColumn);

            // Iterate through the rows and update the new boolean column
            foreach (DataRow row in dataTable.Rows)
            {
                object columnValue = row[columnName];
                if (columnValue != DBNull.Value && columnValue is string)
                {
                    // Convert the string to boolean
                    if (bool.TryParse((string)columnValue, out bool convertedColumnValue))
                    {
                        // Update the new boolean column with the boolean value
                        row["new_bool"] = convertedColumnValue;
                    }
                    else
                    {
                        // Handle the case where the conversion fails
                    }
                }
            }

            // Remove the old "hidden" column
            dataTable.Columns.Remove(columnName);

            // Rename the new column to "hidden"
            newHiddenColumn.ColumnName = columnName;

        }

        static void AddScrapColumns(DataTable mainTable, DataTable scrapTable, string columnPrefix)
        {
            // Check if the tables and common key column exist
            if (mainTable == null || scrapTable == null || !mainTable.Columns.Contains("game_Id") || !scrapTable.Columns.Contains("game_Id"))
            {
                return;
            }

            // Add scrap columns to the main table using the game_id key
            foreach (DataRow mainRow in mainTable.Rows)
            {
                // Extract the game_id key value
                object gameId = mainRow["game_Id"];

                // Find the corresponding scrap rows in the second table
                DataRow[] matchingScrapRows = scrapTable.Select($"game_Id = {gameId}");

                // Add scrap columns to the main row
                foreach (DataRow matchingScrapRow in matchingScrapRows)
                {
                    // Generate the column name with the prefix
                    string columnName = $"{columnPrefix}{matchingScrapRow["name"]}";

                    // Check if the column already exists in the main table, if not, add it
                    if (!mainTable.Columns.Contains(columnName))
                    {
                        mainTable.Columns.Add(columnName.ToLower(), typeof(string));
                    }

                    // Set the value in the main row
                    mainRow[columnName] = matchingScrapRow["date"];
                }
            }
        }

        public bool LoadXML(string fileName)
        {

            Cursor.Current = Cursors.WaitCursor;

            dataGridView1.DataSource = null;
            DataSet.Reset();

            XMLFilename = fileName;

            try
            {
                DataSet.ReadXml(XMLFilename);
            }

            catch (Exception ex)
            {
                // Handle exceptions appropriately (log, show a message, etc.)
                MessageBox.Show($"An error occurred loading the file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            //Add scraper columns
            AddScrapColumns(
            DataSet.Tables[0], DataSet.Tables[1], "scrap_");

            //Convert true/false columns to boolean
            ConvertColumnToBoolean(DataSet.Tables[0], "hidden");
            ConvertColumnToBoolean(DataSet.Tables[0], "favorite");

            DataSet.Tables[0].Columns.Add("unplayable", typeof(bool));
            DataSet.Tables[0].Columns.Add("missing", typeof(bool));
            SetColumnOrdinals(DataSet.Tables[0],
                ("missing", 0),
                ("unplayable", 1),
                ("hidden", 2),
                ("favorite", 3),
                ("path", 4),
                ("name", 5),
                ("genre", 6),
                ("releasedate", 7),
                ("players", 8),
                ("rating", 9),
                ("lang", 10),
                ("region", 11),
                ("publisher", 12),
                ("developer", 13),
                ("playcount", 14),
                ("gametime", 15),
                ("lastplayed", 16)
            );

            dataGridView1.DataSource = DataSet.Tables[0];

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.Visible = false;
                col.ReadOnly = true;
            }

            //Checking of checkboxes is handled by an on_click event
            //This allows the form to update right away
            //dataGridView1.Columns["hidden"].ReadOnly = false;
            //dataGridView1.Columns["favorite"].ReadOnly = false;
            dataGridView1.Columns["hidden"].Visible = true;
            dataGridView1.Columns["path"].Visible = true;
            dataGridView1.Columns["name"].Visible = true;
            dataGridView1.Columns["genre"].Visible = true;
            dataGridView1.Columns["players"].Visible = true;
            dataGridView1.Columns["rating"].Visible = true;

            dataGridView1.Columns["favorite"].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns["hidden"].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns["missing"].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns["unplayable"].SortMode = DataGridViewColumnSortMode.Automatic;

            dataGridView1.Columns["hidden"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridView1.Columns["favorite"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridView1.Columns["players"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridView1.Columns["rating"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridView1.Columns["path"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns["name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns["genre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            BuildCombobox();
            SetColumnTags();
            ResetForm();
            UpdateCounters();

            Cursor.Current = Cursors.Default;

            RegistryManager.SaveLastFilename(XMLFilename);

            return true;
        }

        private void SetColumnTags()
        {
            DataGridViewTextBoxColumn imageColumn = (DataGridViewTextBoxColumn)dataGridView1.Columns["video"];

            // Setting the Tag property for the "image" column
            imageColumn.Tag = "video";


            // Set image column tags
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                // Check each cell in the column for a picture filename with ".jpg" or ".png" extension
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    object cellValue = row.Cells[column.Index].Value;

                    // Check if the cell value is a filename with ".jpg" or ".png" extension
                    if (cellValue != null)
                    {
                        string fileName = cellValue.ToString().ToLower();

                        if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        {
                            column.Tag = "image";
                            break; // If at least one cell has a matching filename, set the tag and break the loop
                        }
                    }
                }
            }
        }

        private void BuildCombobox()
        {
            // setup combobox values
            var uniqueValues = dataGridView1.Rows.Cast<DataGridViewRow>()
                  .Select(row => row.Cells["genre"].Value)
                  .Where(value => value != null && !string.IsNullOrEmpty(value.ToString()))
                  .Distinct()
                  .ToArray();


            // Sort Items
            Array.Sort(uniqueValues);

            comboBox1.Items.Clear();
            comboBox1.Items.Add("<All Genres>");
            comboBox1.Items.Add("<Empty Genres>");
            comboBox1.Items.AddRange(uniqueValues);
            comboBox1.SelectedIndex = 0;

        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            string selectedItem = comboBox1.SelectedItem as string;

            string genreFilter = string.Empty;

            if (index == 1)
            {
                genreFilter = "genre IS NULL";

            }

            if (index > 1)
            {
                selectedItem = selectedItem.Replace("'", "''");
                genreFilter = $"genre = '{selectedItem}'";
            }

            string visibilityFilter = Getvisibilityfilter();
            ApplyFilters(visibilityFilter, genreFilter);

            showAllGenreToolStripMenuItem.Checked = false;
            showGenreOnlyToolStripMenuItem.Checked = true;

            UpdateCounters();

        }

        private void ResetForm()
        {
            statusBar1.Text = XMLFilename;
            showAllItemsToolStripMenuItem.Checked = true;
            ShowVisibleItemsOnlyToolStripMenuItem.Checked = false;
            ShowHiddenItemsOnlyToolStripMenuItem.Checked = false;
            showAllGenreToolStripMenuItem.Checked = true;
            showGenreOnlyToolStripMenuItem.Checked = false;
            verifyRomPathsToolStripMenuItem.Checked = false;
            ShowMediaToolStripMenuItem.Checked = false;

            foreach (ToolStripMenuItem item in menuStrip1.Items)
            {
                item.Enabled = true;
            }

            foreach (var item in columnsToolStripMenuItem.DropDownItems)
            {
                if (item is ToolStripMenuItem toolStripItem)
                {
                    toolStripItem.Checked = false;
                }
            }
            descriptionToolStripMenuItem.Checked = true;

            saveFileToolStripMenuItem.Enabled = true;
            reloadGamelistxmlToolStripMenuItem.Enabled = true;


            string romPath = Path.GetFileName(Path.GetDirectoryName(XMLFilename));
            Image image = (Bitmap)Properties.Resources.ResourceManager.GetObject(romPath);
            //Image image = LoadImageFromResource(romPath);
         
            if (image is Image)
            {
                pictureBox1.Image = image;
            }
            else
            {
                pictureBox1.Image = Properties.Resources.gamelistmanager;
            }

            }
         private void LoadLastFilenamesToMenu()
        {
            try
            {
                // Retrieve the last filenames from the registry
                List<string> lastFilenames = RegistryManager.LoadLastFilenames();

                // Get the "Load Gamelist XML" menu item
                ToolStripMenuItem loadGamelistMenuItem = loadGamelistxmlToolStripMenuItem;

                // Create a list to store items to remove
                var itemsToRemove = new List<ToolStripMenuItem>();

                // Identify existing filename sub-menu items to remove
                foreach (ToolStripMenuItem item in fileToolStripMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    if (item.Name != null && item.Name.StartsWith("lastfile_"))
                    {
                        itemsToRemove.Add(item);
                    }
                }

                // Remove existing filename sub-menu items
                foreach (var item in itemsToRemove)
                {
                    fileToolStripMenuItem.DropDownItems.Remove(item);
                }

                // Add the last filenames as sub-menu items
                foreach (string filename in lastFilenames)
                {
                    ToolStripMenuItem filenameMenuItem = new ToolStripMenuItem(filename);
                    filenameMenuItem.Name = "lastfile_" + filename.Replace(" ", "_"); // Use a unique name for each item
                    filenameMenuItem.Click += FilenameMenuItem_Click;
                    fileToolStripMenuItem.DropDownItems.Add(filenameMenuItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading last filenames to menu: {ex.Message}");
            }
        }


        private void FilenameMenuItem_Click(object sender, EventArgs e)
        {
            // Handle the click event for the filename menu item
            ToolStripMenuItem filenameMenuItem = (ToolStripMenuItem)sender;
            string selectedFilename = filenameMenuItem.Text;
            string selectedItem = filenameMenuItem.Name;
            bool success = LoadXML(selectedFilename);
            if (success == true && !selectedItem.StartsWith("lastfile_"))

            {
                LoadLastFilenamesToMenu();
            }
        }


        private void SetColumnOrdinals(DataTable dataTable, params (string columnName, int ordinal)[] columns)
        {
            foreach (var (columnName, ordinal) in columns)
            {
                if (dataTable.Columns.Contains(columnName))
                {
                    dataTable.Columns[columnName].SetOrdinal(ordinal);
                }
            }
        }

        private void MediaPathsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            bool visible;
            if (menuItem.Checked)
            {
                visible = true;
            }
            else
            {
                visible = false;
            }

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                // Check if the column has the tag 'image'
                if (column.Tag != null && column.Tag.ToString() == "image")
                {
                    // Set the column's visibility to true
                    column.Visible = visible;
                }
                if (column.Name != null && column.Name.ToString() == "video")
                {
                    // Set the column's visibility to true
                    column.Visible = visible;
                }
            }

        }

        private void Updatecolumnview(object sender)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            bool visible;
            if (menuItem.Checked)
            {
                visible = true;
            }
            else
            {
                visible = false;
            }
            string columnName = menuItem.Text.Replace(" ", "").ToLower();

            if (dataGridView1.Columns.Contains(columnName))
            {
                dataGridView1.Columns[columnName].Visible = visible;
            }
        }

        private void favoriteToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void LanguageToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void DeveloperToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void PublisherToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void RegionToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void ReleaseDateToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void GametimeToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void LastplayedToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void PlaycountToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void ViewToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {

            if (dataGridView1.SelectedRows.Count == 0)
            {
                return;
            }

            var rowIndex = dataGridView1.SelectedRows[0].Index;
            var columnIndex = dataGridView1.Columns["genre"].Index;
            object cellValue = dataGridView1.Rows[rowIndex].Cells[columnIndex].Value;
            string genre = (cellValue != DBNull.Value) ? Convert.ToString(cellValue) : string.Empty;
            showGenreOnlyToolStripMenuItem.Text = string.IsNullOrEmpty(genre) ? "Show Empty Genre" : "Show Only '" + genre + "' Items";
        }

        private void HighlightUnplayableGames(HashSet<string> gameNames)
        {

            Cursor.Current = Cursors.WaitCursor;
            var dataTable = DataSet.Tables[0];
            var columnIndex = dataTable.Columns["path"].Ordinal;

            // Suspend the DataGridView layout to improve performance
            dataGridView1.SuspendLayout();

            foreach (DataRow row in dataTable.Rows)
            {
                string originalPath = row[columnIndex].ToString();
                string path = ExtractPath(originalPath);

                // Check if the path is in the HashSet
                bool isUnplayable = gameNames.Contains(path);

                // Set the value of the "IsUnplayable" column to true or false
                row["unplayable"] = isUnplayable;
            }

            // Resume the DataGridView layout
            dataGridView1.ResumeLayout();

            // Refresh the DataGridView to update the UI once all changes are made
            dataGridView1.Refresh();

            Cursor.Current = Cursors.Default;
        }

        private string ExtractPath(string originalPath)
        {
            // Use regex to match the pattern and extract the desired value
            Match match = Regex.Match(originalPath, "/([^/]+)\\.[^/.]+$");
            return match.Success ? match.Groups[1].Value : originalPath;
        }



        private void ShowAllGenreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            showAllGenreToolStripMenuItem.Checked = true;
            showGenreOnlyToolStripMenuItem.Checked = false;
        }

        private void ShowGenreOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                return;
            }

            var rowIndex = dataGridView1.SelectedRows[0].Index;
            var columnIndex = dataGridView1.Columns["genre"].Index;
            object cellValue = dataGridView1.Rows[rowIndex].Cells[columnIndex].Value;
            string genre = (cellValue != DBNull.Value) ? Convert.ToString(cellValue) : string.Empty;

            if (string.IsNullOrEmpty(genre))
            {
                comboBox1.SelectedIndex = 1;
            }
            else
            {
                comboBox1.Text = genre;
            }

            showAllGenreToolStripMenuItem.Checked = false;
            showGenreOnlyToolStripMenuItem.Checked = true;
            UpdateCounters();
        }

        private string GetGenreFromSelectedRow()
        {
            setAllGenreVisibleToolStripMenuItem.Enabled = true;
            setAllGenreHiddenToolStripMenuItem.Enabled = true;

            if (dataGridView1.SelectedRows.Count > 0)
            {
                var rowIndex = dataGridView1.SelectedRows[0].Index;
                var columnIndex = dataGridView1.Columns["genre"].Index;
                object cellValue = dataGridView1.Rows[rowIndex].Cells[columnIndex].Value;
                return (cellValue != DBNull.Value) ? Convert.ToString(cellValue) : string.Empty;
            }

            return string.Empty;
        }

        private void EditToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {

            int selectedRowCount = dataGridView1.SelectedRows.Count;

            if (selectedRowCount < 2)
            {
                setAllItemsVisibleToolStripMenuItem.Text = "Set Item Visible";
                setAllItemsHiddenToolStripMenuItem.Text = "Set Item Hidden";
                deleteRowToolStripMenuItem.Text = "Delete Row";
            }
            else
            {
                setAllItemsVisibleToolStripMenuItem.Text = "Set Selected Items Visible";
                setAllItemsHiddenToolStripMenuItem.Text = "Set Selected Items Hidden";
                deleteRowToolStripMenuItem.Text = "Delete Selected Rows";
            }

            if (selectedRowCount == 1)
            {
                setAllGenreVisibleToolStripMenuItem.Enabled = true;
                setAllGenreHiddenToolStripMenuItem.Enabled = true;

                string genre = GetGenreFromSelectedRow();

                if (genre == string.Empty)
                {
                    genre = "Empty Genre";
                }

                setAllGenreHiddenToolStripMenuItem.Text = "Set All \"" + genre + "\" Hidden";
                setAllGenreVisibleToolStripMenuItem.Text = "Set All \"" + genre + "\" Visible";

            }
            else
            {
                setAllGenreVisibleToolStripMenuItem.Enabled = false;
                setAllGenreHiddenToolStripMenuItem.Enabled = false;
            }
        }

        private void DeleteRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count < 1) { return; }

            int count = dataGridView1.SelectedRows.Count;

            string item = "item";
            string has = "has";

            if (count > 1)
            {
                item = "items";
                has = "have";
            }

            DialogResult result = MessageBox.Show($"Do you want delete the selected {item}?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                dataGridView1.Rows.RemoveAt(row.Index);
            }

            UpdateCounters();

            MessageBox.Show($"{count} {item} {has} been deleted!", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ReloadGamelistxmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show($"Do you want to reload the file '{XMLFilename}'?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            LoadXML(XMLFilename);

        }

        private void SetAllGenreVisibleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string genre = GetGenreFromSelectedRow();
            SetVisibilityByItemValue("genre", genre, false);
        }

        private void SetAllGenreHiddenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string genre = GetGenreFromSelectedRow();
            SetVisibilityByItemValue("genre", genre, true);
        }

        private void SetVisibilityByItemValue(string colname, string colvalue, bool hiddenValue)
        {

            DataTable dataTable = DataSet.Tables[0];

            var rowsToUpdate = dataTable.AsEnumerable()
                .Where(row =>
            (string.Equals(row.Field<string>(colname), colvalue, StringComparison.OrdinalIgnoreCase) ||
            (string.IsNullOrEmpty(colvalue) && string.IsNullOrEmpty(row.Field<string>(colname))))
        );
            foreach (var row in rowsToUpdate)
            {
                row["hidden"] = hiddenValue;
            }
        }

        private void SetColumnsReadOnly(DataGridView dataGridView, bool readonlyboolean, params string[] columnNames)
        {
            foreach (string name in columnNames)
            {
                if (dataGridView.Columns.Contains(name))
                {
                    dataGridView.Columns[name].ReadOnly = readonlyboolean;
                    if (!readonlyboolean)
                    {
                        dataGridView.Columns[name].DefaultCellStyle.ForeColor = Color.Blue;
                    }
                    else
                    {
                        dataGridView.Columns[name].DefaultCellStyle.ForeColor = Color.Black;
                    }
                }
            }
        }

        private void EditRowDataToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            bool readonlyBoolean = true;

            if (editRowDataToolStripMenuItem.Checked)
            {
                readonlyBoolean = false;
            }

            SetColumnsReadOnly(dataGridView1, readonlyBoolean, "name", "genre", "players", "rating", "lang", "region", "publisher");

        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            //This event was a HUGE PITA to do and is required
            //for proper view updating when a datagridview cell checkbox is 
            //changed and we need to update the form right away

            // Check if the clicked column is the hidden column
            string columnName = dataGridView1.Columns[e.ColumnIndex].Name;
            if (columnName != "hidden" && columnName != "favorite")
            {
                // Exit the method if the clicked column is not the hidden column
                return;
            }

            if (dataGridView1.SelectedRows.Count != 1 || e.RowIndex < 0)
            {
                return;
            }

            changeBoolValue(columnName, e.RowIndex);
            UpdateCounters();
        }

        private void changeBoolValue(string columnName, int columnIndex)
        {
            var hiddenValue = dataGridView1.Rows[columnIndex].Cells[columnName].Value;
            //Get the path value so we can lookup the row in the table and change it there
            var pathValue = dataGridView1.Rows[columnIndex].Cells["path"].Value;

            bool currentValue = false;

            if (hiddenValue is bool)
            {
                currentValue = (bool)hiddenValue;
            }
            else if (hiddenValue == DBNull.Value)
            {
                currentValue = false;
            }

            string path = (string)pathValue;

            // Find the corresponding row in the DataSet
            DataRow[] rows = DataSet.Tables[0].Select($"path = '{path}'");

            if (rows.Length > 0)
            {
                DataRow tabledata = rows[0];

                if (currentValue == true)
                {
                    //We will just remove the value since that is the same as false
                    tabledata[columnName] = DBNull.Value;
                }
                else
                {
                    tabledata[columnName] = true;
                }
            }

        }


        private void SaveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {

            string destinationFileName = Path.ChangeExtension(XMLFilename, "old");

            DialogResult result = MessageBox.Show($"Do you save the file '{XMLFilename}'?\nA backup will be saved as {destinationFileName}", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }


            Cursor.Current = Cursors.WaitCursor;

            //Remove all temporary columns
            //Identify extra columns created earlier
            List<DataColumn> columnsToDelete = new List<DataColumn>();
            foreach (DataColumn column in DataSet.Tables[0].Columns)
            {
                if (column.ColumnName.StartsWith("scrap_") || column.ColumnName.Contains("missing"))
                {
                    columnsToDelete.Add(column);
                }
            }

            // Delete identified columns
            foreach (DataColumn columnToDelete in columnsToDelete)
            {
                DataSet.Tables[0].Columns.Remove(columnToDelete.ColumnName);
            }

            // Set a few ordinals
            SetColumnOrdinals(DataSet.Tables[0],
                ("name", 0),
                ("path", 1),
                ("genre", 2),
                ("hidden", 3)
            );



            // Since the deleted columns were removed, make sure these are not checked
            verifyRomPathsToolStripMenuItem.Checked = false;
            scraperDatesToolStripMenuItem.Checked = false;

            DataSet.Tables[0].AcceptChanges();

            File.Copy(XMLFilename, destinationFileName, true);

            DataSet.WriteXml(XMLFilename);

            Cursor.Current = Cursors.Default;

            MessageBox.Show("File save completed!", "Notification", MessageBoxButtons.OK);
        }

        private void ScraperDatesToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {

            bool isVisible = false;

            if (scraperDatesToolStripMenuItem.Checked)
            {
                isVisible = true;
            }

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                // Check if the column name starts with "scrap_"
                if (column.Name.StartsWith("scrap_"))
                {
                    // Set the visibility for scrap columns
                    column.Visible = isVisible;
                }

            }
        }

        private void ClearScraperDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string items = "items?";
            if (dataGridView1.SelectedRows.Count == 1)
            {
                items = "item?";
            }

            DialogResult result = MessageBox.Show($"Do you want to clear the scraper dates for the selected {items}", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            SetScraperDate(string.Empty);

        }

        private void SetScraperDate(string date)
        {
            foreach (DataGridViewRow selectedRow in dataGridView1.SelectedRows)
            {
                // Loop through columns in the selected row
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    // Check if the column name starts with "scrap_"
                    if (column.Name.StartsWith("scrap_"))
                    {
                        // Clear or set the value in the current scrap column
                        if (date == string.Empty)
                        {
                            selectedRow.Cells[column.Name].Value = DBNull.Value;
                        }
                        else
                        {
                            selectedRow.Cells[column.Name].Value = date;
                        }
                    }
                }

                string pathValue = selectedRow.Cells["path"].Value.ToString();

                // Find the corresponding row in table0
                DataRow[] rowsInTable0 = DataSet.Tables[0].Select($"path = '{pathValue}'");

                // Check if a matching row is found
                if (rowsInTable0.Length > 0)
                {
                    // Get the game_id from the matched row in table0
                    int gameId = Convert.ToInt32(rowsInTable0[0]["game_id"]);

                    // Find and update or add rows in table1 with matching game_id
                    DataRow[] rowsToUpdate = DataSet.Tables[1].Select($"game_id = {gameId}");
                    if (rowsToUpdate.Length > 0)
                    {
                        // Update the date field or remove the row
                        foreach (DataRow rowToUpdate in rowsToUpdate)
                        {
                            if (date == string.Empty)
                            {
                                rowToUpdate.Delete();
                            }
                            else
                            {
                                rowToUpdate["date"] = date;
                            }
                        }
                    }
                    else
                    {
                        // Add a new row to table1
                        DataRow newRow = DataSet.Tables[1].NewRow();
                        newRow["game_id"] = gameId;
                        newRow["date"] = (date == string.Empty) ? DBNull.Value : (object)date;
                        DataSet.Tables[1].Rows.Add(newRow);
                    }
                }
            }

            DataSet.Tables[1].AcceptChanges();

            MessageBox.Show("Scraper dates have been updated!", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateScraperDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string items = "items?";
            if (dataGridView1.SelectedRows.Count == 1)
            {
                items = "item?";
            }

            DialogResult result = MessageBox.Show("Do you want to update the scraper dates for the selected " + items, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            DateTime currentDateTime = DateTime.Now;
            string iso8601Format = currentDateTime.ToString("yyyyMMddTHHmmss");

            SetScraperDate(iso8601Format);
        }

        private void VerifyRomPathsToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (verifyRomPathsToolStripMenuItem.Checked == true)
            {
                dataGridView1.Columns["missing"].Visible = false;
                verifyRomPathsToolStripMenuItem.Checked = false;
                return;
            }

            int missingCount = 0;
            int totalItemCount = DataSet.Tables[0].Rows.Count;

            ProgressBarInitialize(0, totalItemCount);

            Cursor.Current = Cursors.WaitCursor;

            verifyRomPathsToolStripMenuItem.Checked = true;

            // Get the index of the "path" column

            string parentFolderPath = Path.GetDirectoryName(XMLFilename);
            // Loop through each row in the DataTable
            foreach (DataRow row in DataSet.Tables[0].Rows)
            {
                // Access the "path" column value for each row
                string itemPath = row["path"].ToString();
                string fullPath = Path.Combine(parentFolderPath, itemPath.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));

                bool missing = File.Exists(fullPath);

                //Some paths are actually folders, not files, so check for a folder name
                //Consider it not missing if the folder exists
                //Too many variations of how games are setup
                if (!missing)
                {
                    missing = Directory.Exists(fullPath);
                }
                // Set the "FileExists" column value based on file existence

                if (missing == false)
                {
                    missingCount++;
                    row["missing"] = true;
                }

                ProgressBarIncrement();
            }

            Cursor.Current = Cursors.Default;
            ProgressBarReset();
            if (missingCount > 0)
            {
                MessageBox.Show("There are " + missingCount.ToString() + " missing items in this gamelist", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information); ;
                dataGridView1.Columns["missing"].Visible = true;
                dataGridView1.Columns["missing"].SortMode = DataGridViewColumnSortMode.Automatic;
                dataGridView1.Columns["missing"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            }
            else
            {
                MessageBox.Show("There are no missing items in this gamelist", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dataGridView1.Columns["missing"].Visible = false;
                verifyRomPathsToolStripMenuItem.Checked = false;
            }
        }

        private void SetAllItemsVisibleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count < 1)
            {
                return;
            }

            string item = "item";

            if (dataGridView1.SelectedRows.Count > 1)
            {
                item = "items";
            }

            DialogResult result = MessageBox.Show("Do you want to set the selected " + item + " visible?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                DataGridViewCell cell = row.Cells["hidden"];
                cell.Value = false;
            }
        }


        private void SetAllItemsHiddenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count < 1)
            {
                return;
            }

            string item = "item";

            if (dataGridView1.SelectedRows.Count > 1)
            {
                item = "items";
            }

            DialogResult result = MessageBox.Show("Do you want to set the selected " + item + " hidden?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                DataGridViewCell cell = row.Cells["hidden"];
                cell.Value = true;
            }

        }

        private void DescriptionToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            if (descriptionToolStripMenuItem.Checked)
            {
                splitContainer1.Panel2Collapsed = false;
            }
            else
            {
                splitContainer1.Panel2Collapsed = true;
            }

        }

        private async void MAMEHighlightUnplayableToolStripMenuItem_Click(object sender, EventArgs e)
        {

            string parentFolderName = Path.GetFileName(Path.GetDirectoryName(XMLFilename));

            if (parentFolderName != "mame")
            {
                MessageBox.Show("This doesn't appear to be a gamelist for mame!", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            string message = "This will identify games that are not playable according to the following rules:\n\n" +
            "isbios = yes\n" +
            "isdevice = yes\n" +
            "ismechanical = yes\n" +
            "driver status = preliminary\n" +
            "disk status = nodump\n" +
            "runnable = no\n\n" +
            "You will be prompted for the location of a current mame.exe file.  Select cancel on the file requester to abort.";

            MessageBox.Show(message, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);



            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set the title of the dialog
            openFileDialog.Title = "Select a mame.exe program";

            // Set the initial directory (optional)
            //openFileDialog.InitialDirectory = "C:\\";

            // Set the filter for the type of files to be displayed
            openFileDialog.Filter = "EXE Files (*.exe)|*.exe|All Files (*.*)|*.*";

            // Set the default file extension (optional)
            openFileDialog.DefaultExt = "exe";

            // Display the dialog and check if the user clicked OK
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            try
            {
                statusBar1.Text = "Started XML Import.....";
                string mameExePath = openFileDialog.FileName;
                var gameNames = await MameUnplayable.GetFilteredGameNames(mameExePath);
                statusBar1.Text = "Identifying unplayable games....";
                HighlightUnplayableGames(gameNames);
                statusBar1.Text = XMLFilename;
                dataGridView1.Columns["unplayable"].Visible = true;
                dataGridView1.Columns["unplayable"].SortMode = DataGridViewColumnSortMode.Automatic;
                dataGridView1.Columns["unplayable"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProgressBarInitialize(int minimum, int maximum)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Minimum = minimum;
                    progressBar1.Maximum = maximum;
                    progressBar1.Value = minimum;
                });
            }
            else
            {
                progressBar1.Minimum = minimum;
                progressBar1.Maximum = maximum;
                progressBar1.Value = minimum;
            }
        }

        private void ProgressBarReset()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Value = progressBar1.Minimum;
                });
            }
            else
            {
                progressBar1.Value = progressBar1.Minimum;
            }
        }

        private void ProgressBarIncrement()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Value++;
                });
            }
            else
            {
                progressBar1.Value++;
            }
        }

        private void FileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            if (saveFileToolStripMenuItem.Enabled == true)
            {
                saveFileToolStripMenuItem.Text = $"Save '{XMLFilename}'";
            }
        }

        private void ClearRecentFilesToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show($"Do you want clear the recent file history?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                RegistryManager.ClearRecentFiles();
                LoadLastFilenamesToMenu();
            }
        }

        public class MediaObject
        {
            public string FullPath { get; set; }
            public int RowIndex { get; set; }
            public int ColumnIndex { get; set; }
            public string Status { get; set; }
        }

        private List<MediaObject> GetMediaList(string mediaType)
        {
            List<MediaObject> mediaList = new List<MediaObject>();

            string parentFolderPath = Path.GetDirectoryName(XMLFilename);

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                // Check if the column tag is set to "image"
                if (column.Tag != null && column.Tag.ToString() == mediaType)
                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        // Access the cell in the specified column
                        DataGridViewCell cell = row.Cells[column.Index];

                        string cellPathValue = cell.Value?.ToString();
                        if (!string.IsNullOrEmpty(cellPathValue))
                        {
                            // Build file list
                            string fullPath = Path.Combine(parentFolderPath, cellPathValue.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));
                            mediaList.Add(new MediaObject
                            {
                                FullPath = fullPath,
                                RowIndex = row.Index,
                                ColumnIndex = column.Index,
                                Status = string.Empty
                            });
                        }
                    }
                }
            }

            return mediaList;
        }


        private void CheckForSingleColorImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {

            List<MediaObject> mediaList = GetMediaList("image");
            int totalFiles = mediaList.Count;

            DialogResult result = MessageBox.Show($"This will check {totalFiles} item images for being single color, missing or corrupt.  This may take a few minutes depending on how many images need to be checked.\n\nDo you want to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            statusBar1.Text = "Checking images...";

            ProgressBarInitialize(0, totalFiles);

            int missingImages = 0;
            int corruptImages = 0;
            int singleColorImages = 0;


            foreach (var mediaObject in mediaList)
            {
                string fileName = mediaObject.FullPath;
                int rowIndex = mediaObject.RowIndex;
                int columnIndex = mediaObject.ColumnIndex;
                bool isSingleColor = false;

                bool fileExists = File.Exists(fileName);

                if (!fileExists)
                {
                    missingImages++;
                    mediaObject.Status = "missing";
                    continue;
                }

                try
                {
                    isSingleColor = ImageProcessor.IsSingleColorImage(fileName);
                }
                catch
                {
                    corruptImages++;
                    mediaObject.Status = "corrupt";

                }
                finally
                {
                    if (isSingleColor)
                    {
                        singleColorImages++;
                        mediaObject.Status = "singlecolor";
                    }
                }


                // Update progress bar
                ProgressBarIncrement();
            }
            statusBar1.Text = XMLFilename;

            if (singleColorImages == 0 && corruptImages == 0 && missingImages == 0)
            {
                MessageBox.Show("No bad or missing images were found", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool boolResult = false;
            int intResult = 0;

            ProgressBarReset();

            Popup1 popupForm = new Popup1(corruptImages, singleColorImages, missingImages);
            // Set the start position and location for the instance of Popup1
            popupForm.StartPosition = FormStartPosition.Manual;
            popupForm.Location = new Point(this.Location.X + 50, this.Location.Y + 50);
            popupForm.ShowDialog();

            // Handle the result and other logic as needed
            boolResult = popupForm.BoolResult;
            intResult = popupForm.intResult;

            if (boolResult == false)
            {
                return;
            }

            // Make a filtered list where Status isn't empty
            List<MediaObject> filteredMediaObjects = mediaList.Where(obj => !string.IsNullOrEmpty(obj.Status)).ToList();

            if (intResult == 1)
            {
                string csvFileName = Directory.GetCurrentDirectory() + "\\" + "bad_images.csv";
                if (ExportToCSV(filteredMediaObjects, csvFileName))
                {
                    MessageBox.Show($"The file '{csvFileName}' was successfully saved", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"There was an error saving file '{csvFileName}'", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            foreach (var mediaObject in filteredMediaObjects)
            {
                string fileName = mediaObject.FullPath;
                int rowIndex = mediaObject.RowIndex;
                int columnIndex = mediaObject.ColumnIndex;
                string status = mediaObject.Status;

                switch (intResult)
                {
                    case 2:
                        try
                        {
                            File.Delete(fileName);
                        }
                        catch
                        {
                            //catch exception - if we care?
                        }
                        break;
                    case 3:
                        string oldFilePath = fileName;
                        string newFileNamePrefix = "bad-";
                        string directory = Path.GetDirectoryName(oldFilePath);
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(oldFilePath);
                        string newFilePath = Path.Combine(directory, $"{newFileNamePrefix}{fileNameWithoutExtension}");
                        try
                        {
                            File.Move(oldFilePath, newFilePath);
                        }
                        catch
                        {
                            // Catch exception - if we care?
                        }
                        break;
                }
                dataGridView1.Rows[rowIndex].Cells[columnIndex].Value = DBNull.Value;
            }
        }





        static bool ExportToCSV(List<MediaObject> mediaObjects, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Write header
                    writer.WriteLine("FullPath,Status");

                    // Write records
                    foreach (var mediaObject in mediaObjects)
                    {
                        writer.WriteLine($"{mediaObject.FullPath},{mediaObject.Status}");
                    }
                }
                // Return success!
                return true;
            }

            catch
            {
                // Return failure
                return false;
            }

        }

        private void editToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string pictureBoxName = contextMenuStrip1.SourceControl.Name;
            PictureBox pictureBox = this.Controls.Find(pictureBoxName, true).OfType<PictureBox>().FirstOrDefault();

            if (pictureBox == null)
            {
                return;
            }

            string imagePath = pictureBox.Tag.ToString();


            try
            {
                Process.Start(imagePath);
            }
            catch
            {
                // Handle the exception if the process can't be started 
                MessageBox.Show("Error loading image!");
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            string pictureBoxName = contextMenuStrip1.SourceControl.Name;
            PictureBox pictureBox = this.Controls.Find(pictureBoxName, true).OfType<PictureBox>().FirstOrDefault();

            if (pictureBox == null)
            {
                return;
            }

            string imagePath = pictureBox.Tag.ToString();
            Clipboard.SetText(imagePath);
        }

        static (string, string, string) GetCredentials()
        {
            string hostName = RegistryManager.ReadRegistryValue("HostName");

            if (hostName == null || hostName == string.Empty)
            {
                MessageBox.Show("The batocera hostname is not set.\nPlease use the remote setup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return ("connectfailed", null, null);
            }

            CredentialManager credentialManager = new CredentialManager();

            string userName = string.Empty;
            string userPassword = string.Empty;

            if (credentialManager.TryReadCredentials(hostName, out NetworkCredential retrievedCredentials))
            {
                userName = retrievedCredentials.UserName;
                userPassword = retrievedCredentials.Password;
            }

            if (userName == null || userName == string.Empty)
            {
                MessageBox.Show("The batocera username is not set.\nPlease run SSH setup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (null, null, null);
            }
            return (hostName, userName, userPassword);
        }


        static string ExecuteSshCommand(string command)
        {
            var result = GetCredentials();
            string hostName = result.Item1;
            string userName = result.Item2;
            string userPassword = result.Item3;

            if (hostName == null || hostName == string.Empty)
            {
                return "connectfailed";
            }

            string output = string.Empty;
            using (var client = new SshClient(hostName, userName, userPassword))
            {
                try
                {
                    client.Connect();
                }
                catch
                {
                    MessageBox.Show($"Could not connect to host {hostName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return "connectfailed";
                }

                using (var commandRunner = client.RunCommand(command))
                {
                    // Show the server output in a message box
                    output = commandRunner.Result;
                }

                client.Disconnect();
                userPassword = null;
                return output;
            }
        }

        private void stopEmulationstationToolStripMenuItem_Click(object sender, EventArgs e)
        {

            DialogResult result = MessageBox.Show("Are you sure you want to stop EmulationStation?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop ; batocera-es-swissknife --espid"; // Replace with your desired command
            string output = ExecuteSshCommand(command) as string;

            if (output == "connectfailed")
            {
                return;
            }

            if (!string.IsNullOrEmpty(output) && output.Length > 0 && output[0] == '0')
            {
                MessageBox.Show("Emulationstation is stopped", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("An error has occured!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void rebootBatoceraHostToolStripMenuItem_Click(object sender, EventArgs e)
        {

            DialogResult result = MessageBox.Show("Are you sure you want to reboot your Batocera host?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;reboot"; // Replace with your desired command
            string output = ExecuteSshCommand(command) as string;

            if (output == "connectfailed")
            {
                return;
            }

            MessageBox.Show("Reboot has been sent to host!", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void setupSSHToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Hide();
            CredentialManager userControl = new CredentialManager();
            splitContainer1.Panel2.Controls.Add(userControl);
            menuStrip1.Enabled = false;
            // I could not figure out how to make the richtextbox visible
            // from within the Credential Manager control because it's nested
            // So as a workaround I make an event to handle this.
            menuStrip1.EnabledChanged += MenuStrip1_EnabledChanged;
        }

        private void MenuStrip1_EnabledChanged(object sender, EventArgs e)
        {
            richTextBox1.Visible = true;
            menuStrip1.EnabledChanged -= MenuStrip1_EnabledChanged;
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string hostName = RegistryManager.ReadRegistryValue("HostName");

            if (hostName == null || hostName == string.Empty)
            {
                MessageBox.Show("The batocera hostname is not set.\nPlease run SSH setup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            CredentialManager credentialManager = new CredentialManager();

            string userName = string.Empty;
            string userPassword = string.Empty;

            if (credentialManager.TryReadCredentials(hostName, out NetworkCredential retrievedCredentials))
            {
                userName = retrievedCredentials.UserName;
                userPassword = retrievedCredentials.Password;
            }


            string sshPath = "C:\\Windows\\System32\\OpenSSH\\ssh.exe"; // Path to ssh.exe on Windows

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(sshPath)
                {
                    Arguments = $"-t {userName}@{hostName}",
                    UseShellExecute = true,
                    //RedirectStandardOutput = true,
                    //RedirectStandardError = true,
                    CreateNoWindow = false // Set this to false to see the terminal window
                };

                Process process = new Process { StartInfo = psi };
                process.Start();

            }
            catch (Exception)
            {
                MessageBox.Show("Could not start OpenSSH", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void getVersionInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string command = "batocera-es-swissknife --version"; // Replace with your desired command
            string output = ExecuteSshCommand(command) as string;

            if (output == "connectfailed")
            {
                return;
            }

            MessageBox.Show($"Your Batocera is version {output}", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void stopRunningEmulatorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to stop any running emulators?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;reboot"; // Replace with your desired command
            string output = ExecuteSshCommand(command) as string;

            if (output == "connectfailed")
            {
                return;
            }

            MessageBox.Show("Running emulators should be stopped now", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void showAvailableUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string command = "batocera-es-swissknife --update"; // Replace with your desired command
            string output = ExecuteSshCommand(command) as string;

            if (output == "connectfailed")
            {
                return;
            }

            MessageBox.Show($"{output}", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void shutdownBatoceraHostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to shutdown your Batocera host?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;sleep 3;shutdown -h now"; // Replace with your desired command
            string output = ExecuteSshCommand(command) as string;

            if (output == "connectfailed")
            {
                return;
            }

            MessageBox.Show("Shutdown has been sent to host!", "Confirmation", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void mapANetworkDriveToolStripMenuItem_ClickAsync(object sender, EventArgs e)
        {
            MapNetworkDrive();
        }

        private void MapNetworkDrive()
        {
            var result = GetCredentials();
            string hostName = result.Item1;
            string userName = result.Item2;
            string userPassword = result.Item3;

            if (hostName == null || hostName == string.Empty)
            {
                return;
            }

            string networkShareToCheck = $"\\\\{hostName}\\share";
            
            bool isMapped = DriveMappingChecker.IsShareMapped(networkShareToCheck);

            
            if (isMapped == true)
            {
                MessageBox.Show($"There already is a drive mapping for {networkShareToCheck}", "Map Network Drive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            char driveLetter = '\0';

            // Get first letter starting at z: working backward
            for (char drive = 'Z'; drive >= 'D'; drive--)
            {
                if (!DriveInfo.GetDrives().Any(d => d.Name[0] == drive))
                {
                    driveLetter = drive;
                    break;
                }
            }

            string networkSharePath = $"\\\\{hostName}\\share";
            string exePath = "net";
            string command = $" use {driveLetter}: {networkSharePath} /user:{userName} {userPassword}";

            // Execute the net use command
            string output = CommandExecutor.ExecuteCommand(exePath, command);

            if (output != null && output != string.Empty)
            {
                MessageBox.Show(output, "Map Network Drive", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }
    public class DriveMappingChecker
    {
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        public static extern int WNetGetConnection(
            [MarshalAs(UnmanagedType.LPTStr)] string localName,
            [MarshalAs(UnmanagedType.LPTStr)] System.Text.StringBuilder remoteName,
            ref int length);

        public static bool IsShareMapped(string networkPath)
        {
            try
            {
                // Iterate through drive letters (A-Z) and check if any is mapped to the specified network path
                for (char driveLetter = 'A'; driveLetter <= 'Z'; driveLetter++)
                {
                    string drive = driveLetter + ":";

                    System.Text.StringBuilder remoteName = new System.Text.StringBuilder(256);
                    int length = remoteName.Capacity;

                    // Call WNetGetConnection to get the remote name for the specified drive letter
                    int result = WNetGetConnection(drive, remoteName, ref length);

                    if (result == 0)
                    {
                        // Check if the mapped path matches the desired network path
                        if (string.Equals(remoteName.ToString(), networkPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking drive mapping: {ex.Message}");
                return false;
            }
        }
    }



    private void checkForMissingVideosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<MediaObject> mediaList = GetMediaList("video");
            int totalFiles = mediaList.Count;

            statusBar1.Text = "Checking images...";

            ProgressBarInitialize(0, totalFiles);

            int missingVideos = 0;
            
            foreach (var mediaObject in mediaList)
            {
                string fileName = mediaObject.FullPath;
                int rowIndex = mediaObject.RowIndex;
                int columnIndex = mediaObject.ColumnIndex;
            
                bool fileExists = File.Exists(fileName);

                if (!fileExists)
                {
                    missingVideos++;
                    mediaObject.Status = "missing";
                }

                // Update progress bar
                ProgressBarIncrement();
            }

            statusBar1.Text = XMLFilename;

            ProgressBarReset();

            if (missingVideos == 0)
            {
                MessageBox.Show("There are no missing videos", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string are = "are";
            string videos = "videos";
            if (missingVideos == 1)
            {
                are = "is";
                videos = "video";
            }

            DialogResult result = MessageBox.Show($"There {are} {missingVideos} missing {videos}.  Do you want to clear these paths?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            // Make a filtered list where Status isn't empty
            List<MediaObject> filteredMediaObjects = mediaList.Where(obj => !string.IsNullOrEmpty(obj.Status)).ToList();

            DialogResult result2 = MessageBox.Show($"Would like a list created first?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result2 == DialogResult.Yes)
            {
                string csvFileName = Directory.GetCurrentDirectory() + "\\" + "missing_videos.csv";
                if (ExportToCSV(filteredMediaObjects, csvFileName))
                {
                    MessageBox.Show($"The file '{csvFileName}' was successfully saved", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"There was an error saving file '{csvFileName}'", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            foreach (var mediaObject in filteredMediaObjects)
            {
                int rowIndex = mediaObject.RowIndex;
                int columnIndex = mediaObject.ColumnIndex;
                dataGridView1.Rows[rowIndex].Cells[columnIndex].Value = DBNull.Value;
            }
        }
    }
}

