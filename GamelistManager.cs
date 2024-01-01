using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;


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
            splitContainer2.Panel2Collapsed = true;
            foreach (ToolStripMenuItem menuItem in menuStrip1.Items)
            {
                menuItem.Enabled = false;
            }
            fileToolStripMenuItem.Enabled = true;
            reloadGamelistxmlToolStripMenuItem.Enabled = false;
            saveFileToolStripMenuItem.Enabled = false;

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
                if (IsImageColumn(cell))
                {
                    Image image = GetImageFromCell(cell, parentFolderPath);

                    if (image != null)
                    {
                        AddImageToTableLayoutPanel(image, cell.OwningColumn.Name, ref columnCount);
                    }
                }
            }

            string videoPath = selectedRow.Cells["video"].Value.ToString();
            string videoFile = Path.Combine(parentFolderPath, videoPath.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (System.IO.File.Exists(videoFile))
            {
                PlayVideo(videoFile, ref columnCount);
            }
            else if (!string.IsNullOrEmpty(videoPath))
            {
                Image missingVideoImage = Properties.Resources.missing;
                AddImageToTableLayoutPanel(missingVideoImage, "video", ref columnCount);
            }

            if (columnCount == 0)
            {
                Image noMediaImage = Properties.Resources.nomedia;
                AddImageToTableLayoutPanel(noMediaImage, "", ref columnCount);
            }

            // Apply style
            foreach (ColumnStyle columnStyle in TableLayoutPanel1.ColumnStyles)
            {
                columnStyle.SizeType = SizeType.Percent;
                columnStyle.Width = 100f / TableLayoutPanel1.ColumnCount; // Equal distribution for each column
            }

            TableLayoutPanel1.BackColor = Color.DarkGray;


            for (int row = 0; row < TableLayoutPanel1.RowCount; row++)
            {
                for (int col = 0; col < TableLayoutPanel1.ColumnCount; col++)
                {
                    TableLayoutPanel1.GetControlFromPosition(col, row).BackColor = Color.DarkGray;
                }
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

        private bool IsImageColumn(DataGridViewCell cell)
        {
            return cell.OwningColumn.Tag?.ToString() == "image" &&
                   cell.Value != null &&
                   !string.IsNullOrEmpty(cell.Value.ToString()) &&
                   (cell.Value.ToString().EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    cell.Value.ToString().EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        }

        private Image GetImageFromCell(DataGridViewCell cell, string parentFolderPath)
        {
            string imagePath = cell.Value.ToString();
            string fullPath = Path.Combine(parentFolderPath, imagePath.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));

            try
            {
                object image = System.IO.File.Exists(fullPath) ? Image.FromFile(fullPath) : Properties.Resources.missing;
                return (Image)image;
            }
            catch
            {
                return (Image)Properties.Resources.loaderror;
            }
        }

        private void AddImageToTableLayoutPanel(Image image, string columnName, ref int columnCount)
        {
            PictureBox pictureBox = new PictureBox
            {
                Image = image,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            TableLayoutPanel1.Controls.Add(pictureBox, columnCount, 1);

            string name = string.Empty;

            if (columnName != string.Empty)
            {
                name = char.ToUpper(columnName[0]) + columnName.Substring(1);
            }


            Label label = new Label
            {
                Text = name,
                Font = new Font("Sego UI", 12, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter
            };

            TableLayoutPanel1.Controls.Add(label, columnCount, 0);

            TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent));
            columnCount++;
            TableLayoutPanel1.ColumnCount = columnCount;
        }

        private void PlayVideo(string videoFilePath, ref int columnCount)
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

            Label label = new Label
            {
                Text = "Video",
                Font = new Font("Sego UI", 12, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter
            };
            TableLayoutPanel1.Controls.Add(label, columnCount, 0);
            mediaPlayer.EndReached += MediaPlayer_EndReached;

            try
            {
                mediaPlayer.Play(new Media(libVLC, new Uri("file:///" + videoFilePath)));
                videoView1.MediaPlayer = mediaPlayer;
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately (log, show a message, etc.)
                MessageBox.Show($"An error occurred loading the video: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                ("uplayable", 1),
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
            // Set image column tags
            string columnTag = "image";
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
                            column.Tag = columnTag;
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

                // Change the cell color for the entire row if unplayable
                if (isUnplayable)
                {
                    row.ClearErrors();  // Clear any previous errors
                    row.RowError = "Unplayable game"; // Store the error message
                }
                else
                {
                    // Reset the cell color if not unplayable
                    row.ClearErrors();  // Clear any previous errors
                }
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

        public static class MameGameLoader
        {
            public static async Task<HashSet<string>> GetFilteredGameNames(string mameExePath)
            {
                string command = "-listxml";
                string output = await ExecuteCommandAsync(mameExePath, command);

                var xml = XDocument.Parse(output);
                var machines = xml.Descendants("machine").ToArray();

                var gameNames = new HashSet<string>();

                foreach (var machine in machines)
                {
                    if (ShouldIncludeMachine(machine))
                    {
                        gameNames.Add(machine.Attribute("name")?.Value);
                    }
                }

                return gameNames;

            }

            private static async Task<string> ExecuteCommandAsync(string mameExePath, string command)
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = mameExePath;
                    process.StartInfo.Arguments = command;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();

                    process.WaitForExit();

                    return output;
                }
            }

            private static bool ShouldIncludeMachine(XElement machine)
            {
                return machine.Attribute("isbios")?.Value == "yes" ||
                       machine.Attribute("isdevice")?.Value == "yes" ||
                       machine.Attribute("ismechanical")?.Value == "yes" ||
                       machine.Element("driver")?.Attribute("status")?.Value == "preliminary" ||
                       machine.Element("disk")?.Attribute("status")?.Value == "nodump" ||
                       machine.Attribute("runnable")?.Value == "no";
            }
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

            changeBoolValue(columnName,e.RowIndex);
            UpdateCounters();
        }

        private void changeBoolValue(string columnName,int columnIndex)
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

            InitializeProgressBar(0, totalItemCount);

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

                IncrementProgressBar();
            }

            Cursor.Current = Cursors.Default;
            ResetProgressBar();
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
                string mameExePath = "d:\\launchbox\\emulators\\mame\\mame.exe";
                var gameNames = await MameGameLoader.GetFilteredGameNames(mameExePath);
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

        private List<Tuple<string, int, int>> GetImagesList()
        {
            string parentFolderPath = Path.GetDirectoryName(XMLFilename);

            List<Tuple<string, int, int>> filesInfoList = new List<Tuple<string, int, int>>();

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                // Check if the column tag is set to "image"
                if (column.Tag != null && column.Tag.ToString() == "image")
                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        // Access the cell in the specified column
                        DataGridViewCell cell = row.Cells[column.Index];

                        string imagePath = cell.Value?.ToString();
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            // Build file list
                            string fullpath = Path.Combine(parentFolderPath, imagePath.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));
                            filesInfoList.Add(Tuple.Create(fullpath, row.Index, column.Index));
                        }
                    }
                }
            }

            return filesInfoList;

        }

        private void ClearMissingImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {

            statusBar1.Text = "Checking files...";

            List<Tuple<string, int, int>> filesInfoList = GetImagesList();

            InitializeProgressBar(0, filesInfoList.Count);

            for (int i = filesInfoList.Count - 1; i >= 0; i--)
            {
                var fileInfo = filesInfoList[i];

                bool exists = File.Exists(fileInfo.Item1);

                // Remove from the list if the file exists
                if (exists)
                {
                    filesInfoList.RemoveAt(i);
                }

                // Update progress bar
                IncrementProgressBar();
            }

            Cursor.Current = Cursors.Default;


            statusBar1.Text = XMLFilename;

            int missingCount = filesInfoList.Count;

            if (missingCount == 0)
            {
                MessageBox.Show("There were no missing images in this gamelist", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetProgressBar();
                return;
            }

            MessageBox.Show($"There were {missingCount} missing images in this gamelist.\nWould you like to clear these paths?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            ResetProgressBar();

        }

        private void InitializeProgressBar(int minimum, int maximum)
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

        private void ResetProgressBar()
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

        private void IncrementProgressBar()
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

        private void CheckForSingleColorImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {


            List<Tuple<string, int, int>> filesInfoList = GetImagesList();

            int totalFiles = filesInfoList.Count;

            DialogResult result = MessageBox.Show($"This will check {totalFiles} source images for being single color or corrupt.  This may take a few minutes depending on how many images need to be checked.\n\nDo you want to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }


            statusBar1.Text = "Checking images...";


            InitializeProgressBar(0, filesInfoList.Count);

            int scc = 0;
            string parentFolderPath = Path.GetDirectoryName(XMLFilename);
            int cf = 0;

            foreach (var fileInfo in filesInfoList)
            {
                // Assuming you have a method to check for a single color (replace with your actual color-checking logic)
                string fullPath = Path.Combine(parentFolderPath, fileInfo.Item1.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));

                bool isSingleColor = false;
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        isSingleColor = ImageProcessor.IsSingleColorImage(fullPath);
                    }
                    catch
                    {
                        cf++;
                    }
                    finally
                    {
                        if (isSingleColor)
                        {
                            scc++;
                        }
                    }


                }
                IncrementProgressBar();
            }

            statusBar1.Text = XMLFilename;

            MessageBox.Show($"There were {scc} single color and {cf} corrupt images.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

      
    }

}

