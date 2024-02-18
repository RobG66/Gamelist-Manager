using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace GamelistManager
{

    public partial class GamelistManagerForm : Form
    {
        private TableLayoutPanel TableLayoutPanel1;
        private VideoView videoView1;
        private LibVLC libVLC;
        private MediaPlayer mediaPlayer;
        public DataGridView DataGridView
        {
            get { return dataGridView1; }
        }

        public GamelistManagerForm()
        {
            InitializeComponent();
        }

        private void SaveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(SharedData.XMLFilename);
        }

        public void SaveFile(string filename)
        {
            string oldFilename = Path.ChangeExtension(filename, "old");

            DialogResult result = MessageBox.Show($"Do you want to save the file '{filename}'?\nA backup will be saved as {oldFilename}", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            toolStripMenuItemScraperDates.Checked = false;
            Cursor.Current = Cursors.WaitCursor;

            // Temporarily remove this event to prevent triggering during save
            dataGridView1.SelectionChanged -= DataGridView1_SelectionChanged;

            // Set a few ordinals to tidy up
            SetColumnOrdinals(SharedData.DataSet.Tables[0],
               ("name", 0),
                ("path", 1),
                ("genre", 2),
                ("hidden", 3)
            );

            // Remove all temporary and empty columns
            for (int i = dataGridView1.Columns.Count - 1; i >= 0; i--)
            {
                DataGridViewColumn column = dataGridView1.Columns[i];
                string columnName = column.Name;

                // Remove from the DataGridView
                if (column.Tag != null && column.Tag.ToString() == "temp")
                {
                    SharedData.DataSet.Tables[0].Columns.Remove(columnName);
                    continue;
                }

                bool allNull = SharedData.DataSet.Tables[0].AsEnumerable().All(row => row.IsNull(columnName));
                // If all values are null, remove the column
                if (allNull)
                {
                    SharedData.DataSet.Tables[0].Columns.Remove(columnName);
                }
            }

            SharedData.DataSet.AcceptChanges();

            File.Copy(filename, oldFilename, true);
            SharedData.DataSet.WriteXml(filename);

            //Update scrap elements.  Easier to do with XML than DataSet 
            if (SharedData.ScrapedList.Count > 0)
            {
                XDocument xdoc = XDocument.Load(filename);

                foreach (ScraperData scraperData in SharedData.ScrapedList)
                {
                    string romName = scraperData.Item;
                    string source = scraperData.Source;
                    string time = scraperData.ScrapeTime;

                    // Find the game element with the matching path
                    XElement targetGameElement = xdoc.Descendants("gameList").Elements("game")
                    .FirstOrDefault(e => e.Element("path")?.Value == $"./{romName}");

                    if (targetGameElement != null)
                    {
                        // Get or create the specific scrap element based on its name
                        XElement targetScrapElement = targetGameElement.Elements("scrap")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == source);

                        if (targetScrapElement == null)
                        {
                            // If the scrap element does not exist, create a new one
                            targetScrapElement = new XElement("scrap",
                                new XAttribute("name", source),
                                new XAttribute("date", time));

                            // Add the new scrap element to the game element
                            targetGameElement.Add(targetScrapElement);
                        }
                        else
                        {
                            // Update existing properties
                            targetScrapElement.SetAttributeValue("date", time);
                        }
                    }
                }

                // Save updated XML with scraper information
                xdoc.Save(filename);
                xdoc = null;
            }

            Cursor.Current = Cursors.Default;

            SharedData.IsDataChanged = false;

            // Reload after save
            LoadXML(filename);

            // Restore event
            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;

            MessageBox.Show("File save completed!", "Notification", MessageBoxButtons.OK);
        }


        private bool SaveReminder()
        {
            DialogResult result = MessageBox.Show("There are unsaved changes, do you want to save them now?", "Confirmation", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                return false;
            }
            if (result == DialogResult.Cancel)
            {
                return true;
            }

            SaveFile(SharedData.XMLFilename);

            return false;
        }

        private void LoadGamelistXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SharedData.IsDataChanged == true)
            {
                bool result = SaveReminder();
                if (result == true)
                {
                    return;  //cancelled
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a Gamelist",
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                DefaultExt = "xml"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string filename = openFileDialog.FileName;
            bool successfulLoad = LoadXML(filename);

            if (successfulLoad == true)
            {
                RegistryManager.SaveLastOpenedGamelistName(filename);
                List<string> recentFiles = RegistryManager.GetRecentFiles();
                UpdateRecentFilesMenu(recentFiles);
            }
        }

        private void ApplyFilters(string visibilityFilter, string genreFilter)
        {
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
            SharedData.DataSet.Tables[0].DefaultView.RowFilter = mergedFilter;
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {

            if (dataGridView1.SelectedRows.Count != 1 || dataGridView1.DataSource == null)
            {
                return;
            }

            var rowIndex = dataGridView1.SelectedRows[0].Index;
            var columnIndex = dataGridView1.Columns["desc"].Index;
            object cellValue = dataGridView1.Rows[rowIndex].Cells[columnIndex].Value;
            string itemDescription = (cellValue != DBNull.Value) ? Convert.ToString(cellValue) : string.Empty;
            richTextBoxDescription.Text = itemDescription;

            // If media is being shown, update that view
            if (splitContainerBig.Panel2Collapsed != true)
            {
                ShowMedia();
            }
        }

        private string GetGenreFilter()
        {
            // Get the current row filter
            string currentFilter = SharedData.DataSet.Tables[0].DefaultView.RowFilter;

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

        private string GetVisibilityFilter()
        {

            // Get the current row filter
            string currentFilter = SharedData.DataSet.Tables[0].DefaultView.RowFilter;

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
            toolStripMenuItemShowAllHiddenAndVisible.Checked = false;
            toolStripMenuItemShowHiddenOnly.Checked = false;
            toolStripMenuItemShowVisibleOnly.Checked = true;
            string visibilityFilter = "hidden = false OR hidden IS NULL";
            string genreFilter = GetGenreFilter();
            ApplyFilters(visibilityFilter, genreFilter);
            UpdateCounters();
        }

        private void ShowHiddenItemsOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripMenuItemShowVisibleOnly.Checked = false;
            toolStripMenuItemShowAllHiddenAndVisible.Checked = false;
            toolStripMenuItemShowHiddenOnly.Checked = true;
            string visibilityFilter = "hidden = true";
            string genreFilter = GetGenreFilter();
            ApplyFilters(visibilityFilter, genreFilter);
            UpdateCounters();
        }

        private void ShowAllItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripMenuItemShowVisibleOnly.Checked = false;
            toolStripMenuItemShowHiddenOnly.Checked = false;
            toolStripMenuItemShowAllHiddenAndVisible.Checked = true;
            string visibilityFilter = string.Empty;
            string genreFilter = GetGenreFilter();
            ApplyFilters(visibilityFilter, genreFilter);

            UpdateCounters();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Get the assembly of the current executing code
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get the version information
            Version version = assembly.GetName().Version;

            // Show the version number
            this.Text = $"Gamelist Manager {version.Major}.{version.Minor}";

            // Show the logo
            pictureBoxSystemLogo.Image = Properties.Resources.gamelistmanager;
            splitContainerBig.Panel2Collapsed = true;

            foreach (ToolStripMenuItem menuItem in menuStripMainMenu.Items)
            {
                menuItem.Enabled = false;
            }

            toolStripMenuItemFileMenu.Enabled = true;
            toolStripMenuItemReload.Enabled = false;
            toolStripMenuItemSave.Enabled = false;
            toolStripMenuItemRemoteMenu.Enabled = true;

            List<string> recentFiles = RegistryManager.GetRecentFiles();
            UpdateRecentFilesMenu(recentFiles);

        }

        private void ClearMenuRecentFiles()
        {
            List<ToolStripMenuItem> itemsToRemove = new List<ToolStripMenuItem>();
            foreach (ToolStripMenuItem item in toolStripMenuItemFileMenu.DropDownItems.OfType<ToolStripMenuItem>())
            {
                if (item.Name != null && item.Name.StartsWith("lastfile_"))
                {
                    itemsToRemove.Add(item);
                }
            }

            foreach (ToolStripMenuItem itemToRemove in itemsToRemove)
            {
                toolStripMenuItemFileMenu.DropDownItems.Remove(itemToRemove);
            }
        }

        private void AddMenuRecentFiles(List<string> recentFiles)
        {
            foreach (string filename in recentFiles)
            {
                ToolStripMenuItem filenameMenuItem = new ToolStripMenuItem(filename)
                {
                    Name = "lastfile_" + filename.Replace(" ", "_") // Use a unique name for each item
                };
                filenameMenuItem.Click += FilenameMenuItem_Click;
                toolStripMenuItemFileMenu.DropDownItems.Add(filenameMenuItem);
            }
        }

        private void UpdateRecentFilesMenu(List<string> recentFiles)
        {
            ClearMenuRecentFiles();
            AddMenuRecentFiles(recentFiles);
        }

        private void ShowMediaToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.Checked == false)
            {
                splitContainerBig.Panel2Collapsed = true;

                ClearTableLayoutPanel();
                return;
            }

            if (dataGridView1.SelectedRows.Count < 1) { return; }

            splitContainerBig.Panel2Collapsed = false;

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
                if (name == "manual")
                {
                    image = Properties.Resources.manual;
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
            }

            PictureBox pictureBox = new PictureBox
            {
                Image = image,
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = name,
                Tag = imagePath,
                ContextMenuStrip = contextMenuStripImageOptions
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
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
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
            panelMediaBackground.Controls.Add(TableLayoutPanel1);

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

            int columnCount = 0;
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);

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
            int hiddenItems = SharedData.DataSet.Tables[0].AsEnumerable()
            .Count(row => row.Field<bool?>("hidden") == true);

            // Count rows where "hidden" is false
            int visibleItems = SharedData.DataSet.Tables[0].AsEnumerable()
            .Count(row => row.Field<bool?>("hidden") != true);

            // Count rows where "hidden" is false
            int favoriteItems = SharedData.DataSet.Tables[0].AsEnumerable()
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

        private void SetupTableColumns()
        {
            if (SharedData.DataSet.Tables.Count == 0)
            {
                SharedData.DataSet.Tables.Add();
                SharedData.DataSet.Tables[0].Columns.Add("path", typeof(string));
            }

            // Standard gamelist.xml elements
            string[] columnNames = {
                "name",
                "desc",
                "rating",
                "releasedate",
                "developer",
                "publisher",
                "genre",
                "players",
                "lang",
                "region",
                "hidden",
                "favorite",
                "image",
                "video",
                "marquee",
                "thumbnail",
                "fanart",
                "titleshot",
                "manual",
                "magazine",
                "map",
                "bezel",
                "md5",
            };

            foreach (string columnName in columnNames)
            {
                if (!SharedData.DataSet.Tables[0].Columns.Contains(columnName))
                {
                    // If the column doesn't exist, add it to the DataTable
                    SharedData.DataSet.Tables[0].Columns.Add(columnName, typeof(string));
                }
            }

            SetupScrapColumns();

            //Convert true/false columns to boolean
            ConvertColumnToBoolean(SharedData.DataSet.Tables[0], "hidden");
            ConvertColumnToBoolean(SharedData.DataSet.Tables[0], "favorite");

            if (!SharedData.DataSet.Tables[0].Columns.Contains("unplayable"))
            {
                SharedData.DataSet.Tables[0].Columns.Add("unplayable", typeof(bool));
            }
            if (!SharedData.DataSet.Tables[0].Columns.Contains("missing"))
            {
                SharedData.DataSet.Tables[0].Columns.Add("missing", typeof(bool));
            }

            SetColumnOrdinals(SharedData.DataSet.Tables[0],
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

            SharedData.DataSet.AcceptChanges();
        }

        private void SetupScrapColumns()
        {
            if (SharedData.DataSet.Tables.Count == 1)
            {
                return;
            }

            // Add scrap columns to the main table using the game_id key
            foreach (DataRow mainRow in SharedData.DataSet.Tables[0].Rows)
            {
                // Extract the game_id key value
                object gameId = mainRow["game_Id"];

                // Find the corresponding scrap rows in the second table
                DataRow[] matchingScrapRows = SharedData.DataSet.Tables[1].Select($"game_Id = {gameId}");

                // Add scrap columns to the main row
                foreach (DataRow matchingScrapRow in matchingScrapRows)
                {
                    // Generate the column name with the prefix
                    string columnName = $"scrap_{matchingScrapRow["name"]}";

                    // Check if the column already exists in the main table, if not, add it
                    if (!SharedData.DataSet.Tables[0].Columns.Contains(columnName))
                    {
                        SharedData.DataSet.Tables[0].Columns.Add(columnName.ToLower(), typeof(string));
                    }

                    // Set the value in the main row
                    mainRow[columnName] = matchingScrapRow["date"];
                }
            }
        }

        private void SetupDataGridViewColumns()
        {
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.Visible = false;
                column.ReadOnly = true;

                DataGridViewCellStyle headerStyle = new DataGridViewCellStyle
                {
                    //ForeColor = Color.Black,
                    //BackColor = Color.LightBlue,
                    Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold)
                };

                column.HeaderCell.Style = headerStyle;
            }

            //Checking of checkboxes is handled by an on_click event
            //This allows the form to update right away
            //dataGridView.Columns["hidden"].ReadOnly = false;
            //dataGridView.Columns["favorite"].ReadOnly = false;
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
        }

        public bool LoadXML(string fileName)
        {

            if (SharedData.IsDataChanged == true)
            {
                bool result = SaveReminder();
                if (result == true)
                    return false;
            }

            Cursor.Current = Cursors.WaitCursor;
            dataGridView1.DataSource = null;
            SharedData.DataSet.Reset();

            try
            {
                SharedData.DataSet.ReadXml(fileName);
            }

            catch (Exception ex)
            {
                // Handle exceptions appropriately (log, show a message, etc.)
                MessageBox.Show($"An error occurred loading the file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            SharedData.XMLFilename = fileName;

            SetupTableColumns();

            dataGridView1.DataSource = SharedData.DataSet.Tables[0];

            SetupDataGridViewColumns();
            BuildCombobox();
            SetColumnTags();
            ResetForm();
            UpdateCounters();

            SharedData.ScrapedList.Clear();

            Cursor.Current = Cursors.Default;

            RegistryManager.SaveLastOpenedGamelistName(SharedData.XMLFilename);

            SharedData.IsDataChanged = false;

            return true;
        }

        private void SetColumnTags()
        {
            // Column tags are used to identify columns as temp, video or image

            // Set temp tags on columns we will discard before saving
            DataGridViewColumn tempColumn = (DataGridViewColumn)dataGridView1.Columns["missing"];
            tempColumn.Tag = "temp";

            DataGridViewColumn tempColumn2 = (DataGridViewColumn)dataGridView1.Columns["unplayable"];
            tempColumn2.Tag = "temp";

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                if (column.Name.StartsWith("scrap_"))
                {
                    column.Tag = "temp";
                }
            }

            // There's only 1 video column and it's named video.  But set the tag anyhow
            DataGridViewTextBoxColumn imageColumn = (DataGridViewTextBoxColumn)dataGridView1.Columns["video"];
            imageColumn.Tag = "video";

            // Set image column tags
            string[] imageTypes = {
            "image",
            "marquee",
            "thumbnail",
            "fanart",
            "titleshot",
            "manual",
            "magazine",
            "map",
            "bezel"
            };

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                string columnName = column.Name.ToString().ToLower();
                if (imageTypes.Contains(columnName))
                {
                    column.Tag = "image";
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

            comboBoxGenre.Items.Clear();
            comboBoxGenre.Items.Add("<All Genres>");
            comboBoxGenre.Items.Add("<Empty Genres>");
            comboBoxGenre.Items.AddRange(uniqueValues);
            comboBoxGenre.SelectedIndex = 0;

        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChangeGenreViaCombobox();
        }

        private void ChangeGenreViaCombobox()
        {
            if (comboBoxGenre.Enabled == false)
            {
                return;
            }
            int index = comboBoxGenre.SelectedIndex;
            string selectedItem = comboBoxGenre.SelectedItem as string;

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

            string visibilityFilter = GetVisibilityFilter();
            ApplyFilters(visibilityFilter, genreFilter);

            toolStripMenuItemShowAllGenres.Checked = false;
            toolStripMenuItemShowGenreOnly.Checked = true;

            UpdateCounters();

        }

        private void ResetForm()
        {
            statusBar.Text = SharedData.XMLFilename;
            toolStripMenuItemShowAllHiddenAndVisible.Checked = true;
            toolStripMenuItemShowVisibleOnly.Checked = false;
            toolStripMenuItemShowHiddenOnly.Checked = false;
            toolStripMenuItemShowAllGenres.Checked = true;
            toolStripMenuItemShowGenreOnly.Checked = false;
            toolStripMenuItemShowMedia.Checked = false;
            checkBoxCustomFilter.Enabled = true;
            comboBoxGenre.Enabled = true;
            checkBoxCustomFilter.Checked = false;

            foreach (ToolStripMenuItem item in menuStripMainMenu.Items)
            {
                item.Enabled = true;
            }

            foreach (var item in toolStripMenuItemColumnsMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem toolStripItem)
                {
                    toolStripItem.Checked = false;
                }
            }
            toolStripMenuItemDescription.Checked = true;
            toolStripMenuItemSave.Enabled = true;
            toolStripMenuItemReload.Enabled = true;

            string romPath = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));
            System.Drawing.Image image = (Bitmap)Properties.Resources.ResourceManager.GetObject(romPath);
            //Image image = LoadImageFromResource(romPath);

            if (image is System.Drawing.Image)
            {
                pictureBoxSystemLogo.Image = image;
            }
            else
            {
                pictureBoxSystemLogo.Image = Properties.Resources.gamelistmanager;
            }

        }

        private void FilenameMenuItem_Click(object sender, EventArgs e)
        {
            // Handle the click event for the filename menu item
            ToolStripMenuItem filenameMenuItem = (ToolStripMenuItem)sender;
            string selectedFilename = filenameMenuItem.Text;
            bool success = LoadXML(selectedFilename);
            if (success == true)
            {
                // This will move it to the top of the list
                RegistryManager.SaveLastOpenedGamelistName(selectedFilename);
                List<string> recentFiles = RegistryManager.GetRecentFiles();
                UpdateRecentFilesMenu(recentFiles);
            }
        }

        public static void SetColumnOrdinals(DataTable dataTable, params (string columnName, int ordinal)[] columns)
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

            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                // Check if the column has the tag 'image'
                if (column.Tag != null && (column.Tag.ToString() == "image" || column.Tag.ToString() == "video"))
                {
                    bool isColumnEmpty = SharedData.DataSet.Tables[0].AsEnumerable().All(row => row.IsNull(column.DataPropertyName) || string.IsNullOrWhiteSpace(row[column.DataPropertyName].ToString()));
                    if (!isColumnEmpty && toolStripMenuItemMediaPaths.Checked == true)
                    {
                        column.Visible = true;
                    }
                    else
                    {
                        column.Visible = false;
                    }
                }
            }

        }

        private void Updatecolumnview(object sender)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            bool visible = false;
            if (menuItem.Checked)
            {
                visible = true;
            }

            string columnName = menuItem.Text.Replace(" ", "").ToLower();

            if (dataGridView1.Columns.Contains(columnName))
            {
                dataGridView1.Columns[columnName].Visible = visible;
            }
        }

        private void FavoriteToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
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
            toolStripMenuItemShowGenreOnly.Text = string.IsNullOrEmpty(genre) ? "Show Empty Genre" : "Show Only '" + genre + "' Items";
        }

        public string ExtractFileNameWithExtension(string originalPath)
        {
            // Use regex to match the pattern and extract the desired value
            Match match = Regex.Match(originalPath, "/([^/]+\\.[^/.]+)$");
            return match.Success ? match.Groups[1].Value : originalPath;
        }

        private void ShowAllGenreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            comboBoxGenre.SelectedIndex = 0;
            toolStripMenuItemShowAllGenres.Checked = true;
            toolStripMenuItemShowGenreOnly.Checked = false;
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
                comboBoxGenre.SelectedIndex = 1;
            }
            else
            {
                comboBoxGenre.Text = genre;
            }

            toolStripMenuItemShowAllGenres.Checked = false;
            toolStripMenuItemShowGenreOnly.Checked = true;
            UpdateCounters();
        }

        private string GetGenreFromSelectedRow()
        {
            toolStripMenuItemSetAllGenreVisible.Enabled = true;
            toolStripMenuItemSetAllGenreHidden.Enabled = true;

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
                toolStripMenuItemSetAllItemsVisible.Text = "Set Item Visible";
                toolStripMenuItemSetAllItemsHidden.Text = "Set Item Hidden";
                toolStripMenuItemDelete.Text = "Delete Row";
            }
            else
            {
                toolStripMenuItemSetAllItemsVisible.Text = "Set Selected Items Visible";
                toolStripMenuItemSetAllItemsHidden.Text = "Set Selected Items Hidden";
                toolStripMenuItemDelete.Text = "Delete Selected Rows";
            }

            if (selectedRowCount == 1)
            {
                toolStripMenuItemSetAllGenreVisible.Enabled = true;
                toolStripMenuItemSetAllGenreHidden.Enabled = true;

                string genre = GetGenreFromSelectedRow();

                if (genre == string.Empty)
                {
                    genre = "Empty Genre";
                }

                toolStripMenuItemSetAllGenreHidden.Text = "Set All \"" + genre + "\" Hidden";
                toolStripMenuItemSetAllGenreVisible.Text = "Set All \"" + genre + "\" Visible";

            }
            else
            {
                toolStripMenuItemSetAllGenreVisible.Enabled = false;
                toolStripMenuItemSetAllGenreHidden.Enabled = false;
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

            List<string> selectedFileList = dataGridView1.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(selectedRow => selectedRow.Cells["path"].Value?.ToString())
            .Where(filePath => !string.IsNullOrEmpty(filePath))
            .ToList();

            var rowsToRemove = SharedData.DataSet.Tables[0].AsEnumerable()
            .Where(row => selectedFileList.Contains(row.Field<string>("path")))
            .ToList();

            foreach (var rowToRemove in rowsToRemove)
            {
                SharedData.DataSet.Tables[0].Rows.Remove(rowToRemove);
            }

            SharedData.DataSet.AcceptChanges();

            UpdateCounters();

            MessageBox.Show($"{count} {item} {has} been deleted!", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ReloadGamelistxmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show($"Do you want to reload the file '{SharedData.XMLFilename}'?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            LoadXML(SharedData.XMLFilename);

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

            DataTable dataTable = SharedData.DataSet.Tables[0];

            var rowsToUpdate = dataTable.AsEnumerable()
                .Where(row =>
            (string.Equals(row.Field<string>(colname), colvalue, StringComparison.OrdinalIgnoreCase) ||
            (string.IsNullOrEmpty(colvalue) && string.IsNullOrEmpty(row.Field<string>(colname))))
        );
            foreach (var row in rowsToUpdate)
            {
                row["hidden"] = hiddenValue;
            }
            SharedData.IsDataChanged = true;
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

            if (toolStripMenuItemEditRowData.Checked)
            {
                readonlyBoolean = false;
            }

            SetColumnsReadOnly(dataGridView1, readonlyBoolean, "name", "genre", "players", "rating", "lang", "region", "publisher");
            SharedData.IsDataChanged = true;
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {

            //This event was a HUGE PITA to do and is required
            //for proper view updating when a datagridview cell checkbox is 
            //changed and we need to update the form right away

            // Check if the clicked column is the hidden or favorite column
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

            // Find the corresponding row in the dataSet
            DataRow[] rows = SharedData.DataSet.Tables[0].Select($"path = '{path}'");

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

        private void ScraperDatesToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {

            bool isVisible = false;

            if (toolStripMenuItemScraperDates.Checked)
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
            SharedData.IsDataChanged = true;

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
                DataRow[] rowsInTable0 = SharedData.DataSet.Tables[0].Select($"path = '{pathValue}'");

                // Check if a matching row is found
                if (rowsInTable0.Length > 0)
                {
                    // Get the game_id from the matched row in table0
                    int gameId = Convert.ToInt32(rowsInTable0[0]["game_id"]);

                    // Find and update or add rows in table1 with matching game_id
                    DataRow[] rowsToUpdate = SharedData.DataSet.Tables[1].Select($"game_id = {gameId}");
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
                        DataRow newRow = SharedData.DataSet.Tables[1].NewRow();
                        newRow["game_id"] = gameId;
                        newRow["date"] = (date == string.Empty) ? DBNull.Value : (object)date;
                        SharedData.DataSet.Tables[1].Rows.Add(newRow);
                    }
                }
            }

            SharedData.DataSet.Tables[1].AcceptChanges();

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
            SharedData.IsDataChanged = true;

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

            SharedData.IsDataChanged = true;

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
            SharedData.IsDataChanged = true;
        }

        private void DescriptionToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            if (toolStripMenuItemDescription.Checked)
            {
                splitContainerSmall.Panel2Collapsed = false;
            }
            else
            {
                splitContainerSmall.Panel2Collapsed = true;
            }

        }

        private void ToolStripMenuItem_MameUnplayable_Click(object sender, EventArgs e)
        {
            string parentFolderName = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));

            if (parentFolderName != "mame")
            {
                MessageBox.Show("This doesn't appear to be a gamelist for mame!", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IdentifyUnplayable();
        }

        private async void IdentifyUnplayable()
        {

            string message = "This will identify games that are not playable according to the following rules:\n\n" +
            "isbios = yes\n" +
            "isdevice = yes\n" +
            "ismechanical = yes\n" +
            "driver status = preliminary\n" +
            "disk status = nodump\n" +
            "runnable = no\n\n" +
            "You will be prompted for the location of a current mame.exe file.  Select cancel on the file requester to abort.";

            MessageBox.Show(message, "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a mame.exe program",
                Filter = "EXE Files (*.exe)|*.exe|All Files (*.*)|*.*",
                DefaultExt = "exe"
            };

            // Display the dialog and check if the user clicked OK
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            List<string> gameNames = null;

            Cursor.Current = Cursors.WaitCursor;
            menuStripMainMenu.Enabled = false;
            panelBelowDataGridView.Enabled = false;

            try
            {
                statusBar.Text = "Started XML Import.....";
                string mameExePath = openFileDialog.FileName;
                gameNames = await Task.Run(() => GetMameUnplayable.GetFilteredGameNames(mameExePath));
            }
            catch (Exception ex)
            {
                Cursor.Current = Cursors.Default;
                statusBar.Text = SharedData.XMLFilename;
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                menuStripMainMenu.Enabled = true;
                panelBelowDataGridView.Enabled = true;
                return;
            }

            if (gameNames == null || gameNames.Count == 0)
            {
                MessageBox.Show("No data was returned!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int unplayableCount = 0;

            // Suspend the DataGridView layout to improve performance
            dataGridView1.SuspendLayout();

            statusBar.Text = "Identifying unplayable games....";
            // Loop through each row in the DataTable
            await Task.Run(() =>
            {
                List<DataRow> rowsToUpdate = new List<DataRow>();

                // Loop through each row in the DataTable
                Parallel.ForEach(SharedData.DataSet.Tables[0].Rows.Cast<DataRow>(), (row) =>
                {
                    string originalPath = row["path"].ToString();
                    string path = Path.GetFileNameWithoutExtension(originalPath);

                    // Set the value of the "unplayable" column to true or false
                    if (gameNames.Contains(path))
                    {
                        Interlocked.Increment(ref unplayableCount);
                        // Accumulate the rows that need to be updated
                        lock (rowsToUpdate)
                        {
                            rowsToUpdate.Add(row);
                        }
                    }
                });

                // Update the 'unplayable' column for all accumulated rows outside the parallel loop
                foreach (DataRow rowToUpdate in rowsToUpdate)
                {
                    rowToUpdate["unplayable"] = true;
                }
            });

            menuStripMainMenu.Enabled = true;
            panelBelowDataGridView.Enabled = true;

            SharedData.DataSet.AcceptChanges();

            // Resume the DataGridView layout
            dataGridView1.ResumeLayout();

            // Refresh the DataGridView to update the UI once all changes are made
            dataGridView1.Refresh();

            Cursor.Current = Cursors.Default;

            statusBar.Text = SharedData.XMLFilename;
            dataGridView1.Columns["unplayable"].Visible = true;
            dataGridView1.Columns["unplayable"].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns["unplayable"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;

            DialogResult result = MessageBox.Show($"There were {unplayableCount} unplayable items found.\nDo you want to set them hidden?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                UpdateCounters();
                return;
            }

            foreach (DataRow row in SharedData.DataSet.Tables[0].Rows)
            {
                // Check if column x is true
                object unplayableValue = row["unplayable"];
                if (unplayableValue != DBNull.Value && Convert.ToBoolean(unplayableValue))
                {
                    row["hidden"] = true;
                }
            }

            SharedData.DataSet.AcceptChanges();
            UpdateCounters();
            SharedData.IsDataChanged = true;
        }


        private void FileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            if (toolStripMenuItemSave.Enabled == true)
            {
                toolStripMenuItemSave.Text = $"Save '{SharedData.XMLFilename}'";
            }
        }

        private void ClearRecentFilesToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show($"Do you want clear the recent file history?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                RegistryManager.ClearRecentFiles();
                ClearMenuRecentFiles();
            }
        }

        private void ToolStripMenuItem_CheckImages_Click(object sender, EventArgs e)
        {
            MediaCheckForm mediaCheckForm = new MediaCheckForm(this)
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(this.Location.X + 50, this.Location.Y + 50)
            };
            mediaCheckForm.ShowDialog();
        }


        private void EditToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string pictureBoxName = contextMenuStripImageOptions.SourceControl.Name;
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

        private void ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            string pictureBoxName = contextMenuStripImageOptions.SourceControl.Name;
            PictureBox pictureBox = this.Controls.Find(pictureBoxName, true).OfType<PictureBox>().FirstOrDefault();

            if (pictureBox == null)
            {
                return;
            }

            string imagePath = pictureBox.Tag.ToString();
            Clipboard.SetText(imagePath);
        }

        static string ExecuteSshCommand(string command)
        {

            string hostName = RegistryManager.ReadRegistryValue("HostName");
            if (string.IsNullOrEmpty(hostName))
            {
                MessageBox.Show("The batocera hostname is not set.\nPlease run SSH setup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "connectfailed";
            }

            (string userName, string userPassword) = CredentialManager.GetCredentials(hostName);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The batocera credentials are missing.\nPlease run SSH setup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "connectfailed";
            }
            string output = SSHCommander.ExecuteSSHCommand(hostName, userName, userPassword, command);
            return output;
        }

        private void StopEmulationstationToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void RebootBatoceraHostToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void SetupSSHToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BatoceraHostSetup userControl = new BatoceraHostSetup();
            richTextBoxDescription.Hide();
            splitContainerSmall.Panel2.Controls.Add(userControl);
            userControl.Disposed += BatoceraHostSetup_Disposed;
            menuStripMainMenu.Enabled = false;
        }

        private void BatoceraHostSetup_Disposed(object sender, EventArgs e)
        {
            BatoceraHostSetup userControl = new BatoceraHostSetup();
            richTextBoxDescription.Visible = true;
            menuStripMainMenu.Enabled = true;
            userControl.Disposed -= BatoceraHostSetup_Disposed;
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string hostName = RegistryManager.ReadRegistryValue("HostName");

            if (hostName == null || hostName == string.Empty)
            {
                MessageBox.Show("The batocera hostname is not set.\nPlease run SSH setup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            (string userName, string userPassword) = CredentialManager.GetCredentials(hostName);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The batocera credentials are missing.\nPlease run SSH setup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
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
            string hostName = RegistryManager.ReadRegistryValue("HostName");
            if (string.IsNullOrEmpty(hostName))
            {
                MessageBox.Show("The batocera hostname is not set.\nPlease run SSH setup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            (string userName, string userPassword) = CredentialManager.GetCredentials(hostName);
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The batocera credentials are missing.\nPlease run SSH setup", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void CheckBox_CustomFilter_CheckedChanged(object sender, EventArgs e)
        {
            if (dataGridView1.RowCount == 0) { return; }

            if (checkBoxCustomFilter.Checked == true)
            {
                textBoxCustomFilter.Enabled = true;
                textBoxCustomFilter.BackColor = SystemColors.Info;
                comboBoxGenre.Enabled = false;
            }
            else
            {
                textBoxCustomFilter.Enabled = false;
                textBoxCustomFilter.BackColor = SystemColors.Window;
                textBoxCustomFilter.Text = "";
                comboBoxGenre.Enabled = true;
                ChangeGenreViaCombobox();

            }
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            string text = textBoxCustomFilter.Text;

            //selectedItem = selectedItem.Replace("'", "''");
            string genreFilter = $"genre LIKE '*{text}*'";
            string visibilityFilter = GetVisibilityFilter();
            ApplyFilters(visibilityFilter, genreFilter);
        }

        private void OpenScraper_Click(object sender, EventArgs e)
        {
            toolStripMenuItemShowMedia.Checked = false;
            ScraperForm scraper = new ScraperForm(this);
            scraper.FormClosed += ScraperForm_FormClosed;
            scraper.Owner = this;
            scraper.StartPosition = FormStartPosition.Manual;
            scraper.Location = new Point(this.Location.X + 50, this.Location.Y + 50);
            toolStripMenuItemShowMedia.Checked = false;
            toolStripMenuItemFileMenu.Enabled = false;
            toolStripMenuItemScraperMenu.Enabled = false;
            toolStripMenuItemToolsMenu.Enabled = false;
            toolStripMenuItemRemoteMenu.Enabled = false;
            panelBelowDataGridView.Enabled = false;
            scraper.Show();

        }

        private void ScraperForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            toolStripMenuItemFileMenu.Enabled = true;
            toolStripMenuItemScraperMenu.Enabled = true;
            toolStripMenuItemToolsMenu.Enabled = true;
            panelBelowDataGridView.Enabled = true;
            toolStripMenuItemRemoteMenu.Enabled = true;
            ((ScraperForm)sender).FormClosed -= ScraperForm_FormClosed;
        }

        private void findNewItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            DialogResult result = MessageBox.Show("This will check for additional items and add them to your gamelist.  Search criteria will be based upon file extensions used for any existing items.\n\nDo you want to contine?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            List<string> fileList = SharedData.DataSet.Tables[0].AsEnumerable()
                                            .Select(row => ExtractFileNameWithExtension(row.Field<string>("path")))
                                            .ToList();

            List<string> uniqueFileExtensions = new List<string>();
            foreach (string path in fileList)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    // Extract the file extension using Path.GetExtension
                    string extension = System.IO.Path.GetExtension(path).TrimStart('.');
                    if (!uniqueFileExtensions.Contains(extension))
                    {
                        uniqueFileExtensions.Add(extension);
                    }
                }
            }

            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);

            List<string> newFileList = uniqueFileExtensions
                .SelectMany(ext => Directory.GetFiles(parentFolderPath, $"*.{ext}"))
                .Select(file => Path.GetFileName(file))
                .ToList();

            newFileList.RemoveAll(file => fileList.Contains(file, StringComparer.OrdinalIgnoreCase));

            if (newFileList.Count == 0)
            {
                MessageBox.Show("No additional items were found", "Notice:", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (string path in newFileList)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                string fileNameWithoutPath = Path.GetFileName(path);
                DataRow newRow = SharedData.DataSet.Tables[0].NewRow();
                newRow["name"] = fileNameWithoutExtension;
                newRow["path"] = $"./{fileNameWithoutPath}";
                // Add the new row to the Rows collection of the DataTable
                SharedData.DataSet.Tables[0].Rows.Add(newRow);
            }

            SharedData.DataSet.AcceptChanges();
            UpdateCounters();
            MessageBox.Show($"{newFileList.Count} items were found and added\nRemember to save if you want to keep these additions", "Notice:", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SharedData.IsDataChanged = true;
        }

        private void ToolStripMenuItem_ClearAllData_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("This will clear all data except for path and name columns\nAre you sure you want to proceed?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }
         
            var pathList = new List<string>();
            var nameList = new List<string>();

            // Iterate through the rows of the source table and populate the lists
            foreach (DataRow row in SharedData.DataSet.Tables[0].Rows)
            {
                pathList.Add(row["path"].ToString());
                nameList.Add(row["name"].ToString());
            }

            dataGridView1.DataSource = null;

            List<string> pathValues = SharedData.DataSet.Tables[0].AsEnumerable()
              .Select(row => row.Field<string>("path"))
              .ToList();

            SharedData.DataSet.Tables[0].DefaultView.RowFilter = null;
            SharedData.DataSet.Clear();
           
            SetupTableColumns();
           
            for (int i = 0; i < pathValues.Count; i++)
            {
                DataRow newRow = SharedData.DataSet.Tables[0].NewRow();
                newRow["path"] = pathList[i];
                newRow["name"] = Path.GetFileNameWithoutExtension(pathList[i]);
                SharedData.DataSet.Tables[0].Rows.Add(newRow);
            }

            SharedData.DataSet.AcceptChanges();

            dataGridView1.DataSource = SharedData.DataSet.Tables[0];
            SetupDataGridViewColumns();
            BuildCombobox();
            SetColumnTags();
            ResetForm();
            UpdateCounters();

            SharedData.IsDataChanged = true;

        }

        private void textBox_CustomFilter_KeyPress(object sender, KeyPressEventArgs e)
        {

            if (e.KeyChar == '\b' || e.KeyChar == '\u007F' || e.KeyChar == ' ' || e.KeyChar == '/')
            {
                return;
            }

            // Allow only characters 'a' to 'z' (both upper and lower case) and digits '0' to '9'
            if (!char.IsLetterOrDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void quickScrapeToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private async void findMissingItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            int missingCount = 0;
            int totalItemCount = SharedData.DataSet.Tables[0].Rows.Count;

            Cursor.Current = Cursors.WaitCursor;

            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);

            List<DataRow> rowsToUpdate = new List<DataRow>();


            await Task.Run(() =>
               {
                   // Loop through each row in the DataTable
                   Parallel.ForEach(SharedData.DataSet.Tables[0].Rows.Cast<DataRow>(), (row) =>
                   {
                       string itemPath = row["path"].ToString();
                       string fullPath = Path.Combine(parentFolderPath, itemPath.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));

                       bool missing = File.Exists(fullPath) || Directory.Exists(fullPath);

                       if (!missing)
                       {
                           Interlocked.Increment(ref missingCount);
                           // Accumulate the rows that need to be updated
                           lock (rowsToUpdate)
                           {
                               rowsToUpdate.Add(row);
                           }
                       }
                   });

               });

            foreach (DataRow rowToUpdate in rowsToUpdate)
            {
                rowToUpdate["missing"] = true;
            }


            Cursor.Current = Cursors.Default;

            if (missingCount > 0)
            {
                MessageBox.Show($"There are {missingCount} missing items in this gamelist", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                dataGridView1.Columns["missing"].Visible = true;
                dataGridView1.Columns["missing"].SortMode = DataGridViewColumnSortMode.Automatic;
                dataGridView1.Columns["missing"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            }
            else
            {
                MessageBox.Show("There are no missing items in this gamelist", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //dataGridView.Columns["missing"].Visible = false;
            }

        }

        private void ToolStripMenuItem_EditRowData_Click(object sender, EventArgs e)
        {

        }

        private void GamelistManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SharedData.IsDataChanged == true)
            {
                bool result = SaveReminder();
                if (result == true)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}





