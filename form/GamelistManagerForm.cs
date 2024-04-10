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

namespace GamelistManager
{

    public partial class GamelistManagerForm : Form
    {
        private string visibilityFilter;
        private string genreFilter;
        private TableLayoutPanel TableLayoutPanel1;
        private VideoView videoView1;
        private LibVLC libVLC;
        private MediaPlayer mediaPlayer;
        public DataGridView DataGridView
        {
            get { return dataGridView1; }
        }
        public ComboBox ComboBoxGenre1
        {
            get { return comboBoxGenre; }
            set { comboBoxGenre = value; }
        }

        public GamelistManagerForm()
        {
            InitializeComponent();
            genreFilter = string.Empty;
            visibilityFilter = string.Empty;
        }

        private void SaveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile(SharedData.XMLFilename);
        }

        public void SaveFile(string filename)
        {
            string oldFilename = Path.ChangeExtension(filename, "old");

            DialogResult result = MessageBox.Show($"Do you want to save the file '{filename}'?\nA backup will be saved as {oldFilename}.\n\nNote: It is recommended to stop EmulationStation before or just after saving any gamelist changes.  Reboot or shutdown the Batocera host when you are finished.", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            ScraperDatesToolStripMenuItem.Checked = false;
            this.Cursor = Cursors.WaitCursor;

            // Temporarily remove this event to prevent triggering during save
            dataGridView1.SelectionChanged -= DataGridView1_SelectionChanged;

            DataSet copiedDataSet = SharedData.DataSet.Copy();

            // Set a few ordinals to tidy up
            SetColumnOrdinals(copiedDataSet.Tables["game"],
               ("name", 0),
                ("path", 1),
                ("genre", 2),
                ("hidden", 3)
            );

            try
            {
                File.Copy(filename, oldFilename, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while copying the file: {ex.Message}\n\nFile save aborted!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            GamelistUtility.ExportDataSetToGameList(copiedDataSet, filename);

            copiedDataSet.Dispose();

            this.Cursor = Cursors.Default;

            SharedData.IsDataChanged = false;

            // Restore event
            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;

            UpdateStatusBar();

            MessageBox.Show("File save completed!", "Notification", MessageBoxButtons.OK);
        }


        private bool SaveReminder()
        {
            DialogResult result = MessageBox.Show("There are unsaved changes, do you want to save them now?", "Confirmation", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            switch (result)
            {
                case DialogResult.Yes:
                    SaveFile(SharedData.XMLFilename);
                    break; 
                case DialogResult.Cancel:
                    return true;
             }
            return false;
        }

        private void LoadGamelistXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SharedData.IsDataChanged)
            {
                if (SaveReminder())
                {
                    return; // canceled
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

            if (successfulLoad)
            {
                RegistryManager.SaveLastOpenedGamelistName(filename);
                List<string> recentFiles = RegistryManager.GetRecentFiles();
                UpdateRecentFilesMenu(recentFiles);
            }
        }

        private void ApplyFilters()
        {
            // Merge the genre filter with the visibility filter using "AND" if both are not empty
            string mergedFilter;
            if (!string.IsNullOrEmpty(genreFilter) && !string.IsNullOrEmpty(visibilityFilter))
            {
                mergedFilter = $"{genreFilter} AND {visibilityFilter}";
            }
            else
            {
                mergedFilter = (!string.IsNullOrEmpty(genreFilter) ? genreFilter : visibilityFilter);
            }
            // Set the modified RowFilter
            SharedData.DataSet.Tables["game"].DefaultView.RowFilter = mergedFilter;
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {

            if (dataGridView1.SelectedRows.Count != 1 || dataGridView1.DataSource == null)
            {
                return;
            }

            var rowIndex = dataGridView1.SelectedRows[0].Index;
            object cellValue = dataGridView1.Rows[rowIndex].Cells["desc"].Value;
            string itemDescription = (cellValue != DBNull.Value) ? Convert.ToString(cellValue) : string.Empty;
            richTextBoxDescription.Text = itemDescription;
            richTextBoxDescription.Tag = rowIndex;

            // If media is being shown, update that view
            if (splitContainerBig.Panel2Collapsed != true)
            {
                ShowMedia();
            }
              
        }

        private void ShowVisibleItemsOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showAllHiddenAndVisibleItemsToolStripMenuItem.Checked = false;
            showHiddenItemsOnlyToolStripMenuItem.Checked = false;    
            showVisibleItemsOnlyToolStripMenuItem.Checked = true;
            visibilityFilter = "(hidden = false OR hidden IS NULL)";
            ApplyFilters();
            UpdateCounters();
        }

        private void ShowHiddenItemsOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showVisibleItemsOnlyToolStripMenuItem.Checked = false;
            showAllHiddenAndVisibleItemsToolStripMenuItem.Checked = false;
            showHiddenItemsOnlyToolStripMenuItem.Checked = true;
            visibilityFilter = "hidden = true";
            ApplyFilters();
            UpdateCounters();
        }

        private void ShowAllItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showVisibleItemsOnlyToolStripMenuItem.Checked = false;
            showHiddenItemsOnlyToolStripMenuItem.Checked= false;
            showAllHiddenAndVisibleItemsToolStripMenuItem.Checked = true;
            visibilityFilter = string.Empty;
            ApplyFilters();

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
            reloadGamelistToolStripMenuItem.Enabled = false;
            exportToCSVToolStripMenuItem.Enabled = false;
            saveGamelistToolStripMenuItem.Enabled = false;
            toolStripMenuItemRemoteMenu.Enabled = true;

            List<string> recentFiles = RegistryManager.GetRecentFiles();
            UpdateRecentFilesMenu(recentFiles);
            SharedData.IsDataChanged = false;

            comboBoxFilterItem.SelectedIndex = 0;

        }

        private void ClearMenuRecentFiles()
        {
            List<ToolStripMenuItem> itemsToRemove = new List<ToolStripMenuItem>();
            foreach (ToolStripMenuItem item in contextMenuStripFile.Items.OfType<ToolStripMenuItem>())
            {
                if (item.Name != null && item.Name.StartsWith("lastfile_"))
                {
                    itemsToRemove.Add(item);
                }
            }

            foreach (ToolStripMenuItem itemToRemove in itemsToRemove)
            {
                contextMenuStripFile.Items.Remove(itemToRemove);
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
                contextMenuStripFile.Items.Add(filenameMenuItem);
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
                Font = new Font("Sego UI", 10, FontStyle.Regular),
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
                //AutoScroll = true,
                RowCount = 2,
                ColumnCount = 1,
            };

            panelMediaBackground.Controls.Add(TableLayoutPanel1);
           
            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

            int columnIndex = 0;
            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);

            foreach (DataGridViewCell cell in selectedRow.Cells.Cast<DataGridViewCell>().OrderBy(c => c.OwningColumn.Name))
            {
          
                if (!SharedData.MediaTypes.Contains(cell.OwningColumn.Name))
                {
                    continue;
                }

                if (cell.Value == null || string.IsNullOrEmpty(cell.Value.ToString()))
                {
                    continue;
                }

                string cellValue = cell.Value.ToString();
                string filePath = Path.Combine(parentFolderPath, cellValue.Replace("./", "").Replace("/", Path.DirectorySeparatorChar.ToString()));
                string columnName = cell.OwningColumn.Name;

                if (columnName != "video")
                {
                    PictureBox pictureBox = MakePictureBox(filePath, columnName);
                    TableLayoutPanel1.Controls.Add(pictureBox, columnIndex, 1);

                    string labelName = char.ToUpper(columnName[0]) + columnName.Substring(1);
                    Label label = MakeLabel(labelName);
                    TableLayoutPanel1.Controls.Add(label, columnIndex, 0);

                    TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent));
                    columnIndex++;
                    TableLayoutPanel1.ColumnCount = columnIndex;
                }
                else
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        TableLayoutPanel1.ColumnCount = columnIndex;

                        Core.Initialize();
                        libVLC = new LibVLC();
                        mediaPlayer = new MediaPlayer(libVLC);

                        videoView1 = new VideoView
                        {
                            Dock = DockStyle.Fill
                        };

                        TableLayoutPanel1.Controls.Add(videoView1, columnIndex, 1);

                        Label videoLabel = MakeLabel("Video");
                        TableLayoutPanel1.Controls.Add(videoLabel, columnIndex, 0);
                        // Add event show the video loops!
                        mediaPlayer.EndReached += MediaPlayer_EndReached;

                        try
                        {
                            mediaPlayer.Play(new Media(libVLC, new Uri("file:///" + filePath)));
                            videoView1.MediaPlayer = mediaPlayer;
                        }
                        catch (Exception ex)
                        {
                            // Handle exceptions appropriately (log, show a message, etc.)
                            MessageBox.Show($"An error occurred loading the video: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent));
                        columnIndex++;
                        TableLayoutPanel1.ColumnCount = columnIndex;
                    }
                }
            }

            // Just in case there were no images or videos, show a 'no media' image 
            if (columnIndex == 0)
            {
                // We use the video item since it has no context menu
                PictureBox picturebox = MakePictureBox(string.Empty, "video");
                picturebox.Image = Properties.Resources.nomedia;
                TableLayoutPanel1.Controls.Add(picturebox, 0, 1);
                TableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent));
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
                    control.ContextMenuStrip = null;
                    control.Dispose();
                    // Dispose of additional resources
                    mediaPlayer.Dispose();
                    videoView1.Dispose();
                    libVLC.Dispose();
                    continue;
                }

                if (control is PictureBox pictureBox && pictureBox.Image != null)
                {
                    pictureBox.Image.Dispose();
                    pictureBox.Image = null; // Remove association
                    control.ContextMenuStrip = null;
                    control.Dispose();
                    continue;
                }

                control.ContextMenuStrip = null;
                control.Dispose();

            }

            TableLayoutPanel1.Controls.Clear();
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
            int hiddenItems = SharedData.DataSet.Tables["game"].AsEnumerable()
            .Count(row => row.Field<bool?>("hidden") == true);

            // Count rows where hidden is not true
            int visibleItems = SharedData.DataSet.Tables["game"].AsEnumerable()
            .Count(row => row.Field<bool?>("hidden") != true);

            // Count rows where favorite is true
            int favoriteItems = SharedData.DataSet.Tables["game"].AsEnumerable()
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
                SharedData.DataSet.Tables["game"].Columns.Add("path", typeof(string));
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
                "id",
                "boxback",
                "genreid",
                "arcadesystemname",
                "fanart"
            };

            foreach (string columnName in columnNames)
            {
                if (!SharedData.DataSet.Tables["game"].Columns.Contains(columnName))
                {
                    // If the column doesn't exist, add it to the DataTable
                    SharedData.DataSet.Tables["game"].Columns.Add(columnName, typeof(string));
                }
            }

            //Convert true/false columns to boolean
            ConvertColumnToBoolean(SharedData.DataSet.Tables["game"], "hidden");
            ConvertColumnToBoolean(SharedData.DataSet.Tables["game"], "favorite");

            if (!SharedData.DataSet.Tables["game"].Columns.Contains("unplayable"))
            {
                SharedData.DataSet.Tables["game"].Columns.Add("unplayable", typeof(bool));
            }
            if (!SharedData.DataSet.Tables["game"].Columns.Contains("missing"))
            {
                SharedData.DataSet.Tables["game"].Columns.Add("missing", typeof(bool));
            }

            SetColumnOrdinals(SharedData.DataSet.Tables["game"],
                ("missing", 0),
                ("unplayable", 1),
                ("hidden", 2),
                ("favorite", 3),
                ("path", 4),
                ("id", 5),
                ("name", 6),
                ("genre", 7),
                ("releasedate", 8),
                ("players", 9),
                ("rating", 10),
                ("lang", 11),
                ("region", 12),
                ("publisher", 13),
                ("developer", 14),
                ("playcount", 15),
                ("gametime", 16),
                ("lastplayed", 17)
            );

            SharedData.DataSet.AcceptChanges();
        }

        private void SetupScrapColumns()
        {
            if (!SharedData.DataSet.Tables.Contains("scrap"))
            {
                return;
            }

            var uniqueValues = SharedData.DataSet.Tables["scrap"].AsEnumerable()
              .Where(row => !string.IsNullOrEmpty(row.Field<string>("name")))
              .Select(row => row.Field<string>("name"))
              .Distinct();

            foreach (var uniqueValue in uniqueValues)
            {
                if (!SharedData.DataSet.Tables["game"].Columns.Contains(uniqueValue))
                {
                    DataColumn newColumn = new DataColumn(uniqueValue, typeof(string));
                    SharedData.DataSet.Tables["game"].Columns.Add($"scrap_{newColumn}");
                }
            }

            SharedData.DataSet.AcceptChanges();

            DataRow[] matchingScrapRows;

            // Add scrap columns to the main table using the game_id key
            foreach (DataRow mainRow in SharedData.DataSet.Tables["game"].Rows)
            {
                // Find the corresponding scrap rows in the second table
                try

                {
                    object gameId = mainRow["game_Id"];
                    matchingScrapRows = SharedData.DataSet.Tables["scrap"].Select($"game_Id = {gameId}");
                }
                catch
                {
                    continue;
                }

                foreach (DataRow matchingScrapRow in matchingScrapRows)
                {
                    string columnName = $"scrap_{matchingScrapRow["name"]}";
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
            //dataGridView1.Columns["players"].Visible = true;
            //dataGridView1.Columns["rating"].Visible = true;

            dataGridView1.Columns["favorite"].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns["hidden"].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns["missing"].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns["unplayable"].SortMode = DataGridViewColumnSortMode.Automatic;

            dataGridView1.Columns["hidden"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridView1.Columns["favorite"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridView1.Columns["players"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridView1.Columns["rating"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridView1.Columns["path"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridView1.Columns["name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Columns["genre"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        public bool LoadXML(string fileName)
        {
            this.Cursor = Cursors.WaitCursor;
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
                this.Cursor = Cursors.Default;
                return false;
            }

            // Extract scraper data
            SetupScrapColumns();

            // Delete all tables except for Game table
            DeleteUnwantedTables();

            SharedData.XMLFilename = fileName;

            SetupTableColumns();

            dataGridView1.DataSource = SharedData.DataSet.Tables["game"];

            SetupDataGridViewColumns();
            BuildCombobox();
            ResetForm();
            UpdateCounters();

            this.Cursor = Cursors.Default;

            RegistryManager.SaveLastOpenedGamelistName(SharedData.XMLFilename);

            SharedData.IsDataChanged = false;

            return true;
        }

        private void DeleteUnwantedTables()
        {

            List<string> tablesToDelete = new List<string>();
            foreach (DataTable table in SharedData.DataSet.Tables)
            {
                if (table.TableName.ToLower() != "game")
                {
                    tablesToDelete.Add(table.TableName);
                }
            }

            if (tablesToDelete.Count > 0)
            {
                foreach (string tableToDelete in tablesToDelete)
                {
                    // Remove foreign key constraints
                    List<ForeignKeyConstraint> constraintsToRemove = SharedData.DataSet.Tables[tableToDelete].Constraints
                        .OfType<ForeignKeyConstraint>()
                        .ToList();

                    foreach (ForeignKeyConstraint constraint in constraintsToRemove)
                    {
                        SharedData.DataSet.Tables[tableToDelete].Constraints.Remove(constraint);
                    }

                    // Remove data relations
                    List<DataRelation> relationsToRemove = SharedData.DataSet.Relations.Cast<DataRelation>()
                        .Where(r => r.ChildTable.TableName == tableToDelete || r.ParentTable.TableName == tableToDelete)
                        .ToList();

                    foreach (DataRelation relation in relationsToRemove)
                    {
                        SharedData.DataSet.Relations.Remove(relation);
                    }

                    // Remove the table itself
                    SharedData.DataSet.Tables.Remove(tableToDelete);
                }
            }

            if (SharedData.DataSet.Tables["game"].PrimaryKey != null)
            {
                SharedData.DataSet.Tables["game"].PrimaryKey = null;
            }
            if (SharedData.DataSet.Tables["game"].Columns.Contains("game_id"))
            {
                SharedData.DataSet.Tables["game"].Columns.Remove("game_id");
            }
        }

        public void BuildCombobox()
        {
            // setup combobox values
            var uniqueValues = SharedData.DataSet.Tables["game"].AsEnumerable()
            .Select(row => row.Field<string>("genre"))
            .Where(value => !string.IsNullOrEmpty(value))
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
           
            int index = comboBoxGenre.SelectedIndex;

            if (index == -1) { return; }

            string selectedItem = index == 0 ? string.Empty : index == 1 ? "genre IS NULL" : $"genre='{(comboBoxGenre.SelectedItem as string)?.Replace("'", "''")}'";

            genreFilter = selectedItem;
            showAllGenresToolStripMenuItem.Checked = index == 0;
            showGenreOnlyToolStripMenuItem.Checked = index == 1 || index > 1;

            ApplyFilters();
            UpdateCounters();

        }

        private void UpdateStatusBar()
        {
            DateTime lastModifiedTime = File.GetLastWriteTime(SharedData.XMLFilename);
            statusBar.Text = $"{SharedData.XMLFilename}  ({lastModifiedTime})";
        }

        public void ResetForm()
        {
            UpdateStatusBar();
            showAllHiddenAndVisibleItemsToolStripMenuItem.Checked = true;
            showVisibleItemsOnlyToolStripMenuItem.Checked = false;
            showHiddenItemsOnlyToolStripMenuItem.Checked = false;
            showAllGenresToolStripMenuItem.Checked = true;
            showGenreOnlyToolStripMenuItem.Checked = false;
            showMediaToolStripMenuItem.Checked = false; 
            checkBoxCustomFilter.Enabled = true;
            comboBoxGenre.Enabled = true;
            checkBoxCustomFilter.Checked = false;
            editRowDataToolStripMenuItem.Checked = false;
            
            DisableEditing(true);
            
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
            DescriptionToolStripMenuItem.Checked = true;
            GenreToolStripMenuItem.Checked = true;
            //toolStripMenuItemSave.Enabled = true;
            //toolStripMenuItemReload.Enabled = true;
            //ToolStripMenuItemExportToCSV.Enabled = true;
            exportToCSVToolStripMenuItem.Enabled = true;
            reloadGamelistToolStripMenuItem.Enabled = true;
            saveGamelistToolStripMenuItem.Enabled = true;
            string romPath = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));
            System.Drawing.Image image = (Bitmap)Properties.Resources.ResourceManager.GetObject(romPath);
            //image image = LoadImageFromResource(romPath);

            if (image is System.Drawing.Image)
            {
                pictureBoxSystemLogo.Image = image;
            }
            else
            {
                pictureBoxSystemLogo.Image = Properties.Resources.gamelistmanager;
            }

            comboBoxGenre.SelectedIndex = 0;

        }

        private void FilenameMenuItem_Click(object sender, EventArgs e)
        {
            if (SharedData.IsDataChanged == true)
            {
                bool saveResult = SaveReminder();
                if (saveResult == true)
                    // true is set for cancel.
                    return;
            }

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
                // Check if the column is image
                if (!SharedData.MediaTypes.Contains(column.Name.ToString()))
                {
                    continue;
                }
                
                bool isColumnEmpty = SharedData.DataSet.Tables["game"].AsEnumerable().All(row => row.IsNull(column.DataPropertyName) || string.IsNullOrWhiteSpace(row[column.DataPropertyName].ToString()));
                if (isColumnEmpty)
                {
                    column.Visible = false;
                    continue;
                }

                column.Visible = MediaPathsToolStripMenuItem.Checked;

            }
        }

        private void Updatecolumnview(object sender)
        {
                ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

                bool visible = menuItem.Checked;

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


        private void ToolStripMenuItemRating_CheckedChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void ToolStripMenuItemPlayers_CheckedChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void ToolStripMenuItemLanguage_CheckedChanged(object sender, EventArgs e)
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

        private void genreToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void LastplayedToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            Updatecolumnview(sender);
        }

        private void ToolStripMenuItemID_CheckedChanged(object sender, EventArgs e)
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
        
            if (checkBoxCustomFilter.Checked)
            {
                showGenreOnlyToolStripMenuItem.Enabled = false;
                showAllGenresToolStripMenuItem.Enabled = false;
            }
            else
            {
                showGenreOnlyToolStripMenuItem.Enabled = true;
                showAllGenresToolStripMenuItem.Enabled = true;
            }
        
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
            showAllGenresToolStripMenuItem.Checked = true;
            showGenreOnlyToolStripMenuItem.Checked = false;
        }

        private void ShowGenreOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count < 1)
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

            showAllGenresToolStripMenuItem.Checked = false;
            showGenreOnlyToolStripMenuItem.Checked = true;
            UpdateCounters();
        }

        private string GetGenreFromSelectedRow()
        {
            // For review
            // toolStripMenuItemSetAllGenreVisible.Enabled = true;
            // toolStripMenuItemSetAllGenreHidden.Enabled = true;

            if (dataGridView1.SelectedRows.Count > 0)
            {
                object cellValue = dataGridView1.SelectedRows[0].Cells["genre"].Value;
                return (cellValue != DBNull.Value) ? Convert.ToString(cellValue) : string.Empty;
            }

            return string.Empty;
        }

        private void EditToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {

            bool readOnly = dataGridView1.Columns["name"].ReadOnly;
            editRowDataToolStripMenuItem.Text = (readOnly == true) ? "Edit Row Data" : "Stop Editing Row Data";


            int selectedRowCount = dataGridView1.SelectedRows.Count;

            setAllItemsVisibleToolStripMenuItem.Text = (selectedRowCount < 2) ? "Set Item Visible" : "Set Selected Items Visible";
            setAllItemsHiddenToolStripMenuItem.Text = (selectedRowCount < 2) ? "Set Item Hidden" : "Set Selected Items Hidden";
            deleteRowToolStripMenuItem.Text = (selectedRowCount < 2) ? "Remove Item" : "Remove Selected Items";
            resetNameToolStripMenuItem.Text = (selectedRowCount < 2) ? "Reset Name" : "Reset Selected Names";

            clearScraperDateToolStripMenuItem.Text = (selectedRowCount < 2) ? "Clear Scraper Date" : "Clear Selected Scraper Dates";
            updateScraperDateToolStripMenuItem.Text = (selectedRowCount < 2) ? "Update Scraper Date" : "Update Selected Scraper Dates";

            if (selectedRowCount == 1)
            {
                setAllGenreVisibleToolStripMenuItem.Enabled = true;
                setAllGenreHiddenToolStripMenuItem.Enabled = true;

                string genre = GetGenreFromSelectedRow();
                genre = string.IsNullOrEmpty(genre) ? "Empty Genre" : genre;

                setAllGenreHiddenToolStripMenuItem.Text = $"Set All \"{genre}\" Hidden";
                setAllGenreVisibleToolStripMenuItem.Text = $"Set All \"{genre}\" Visible";
            }
            else
            {
                setAllGenreVisibleToolStripMenuItem.Enabled = false;
                setAllGenreHiddenToolStripMenuItem.Enabled = false;
            }
        }



        private void ReloadGamelistxmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show($"Do you want to reload the file '{SharedData.XMLFilename}'?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            if (SharedData.IsDataChanged == true)
            {
                bool saveResult = SaveReminder();
                if (saveResult)
                    // true is set for cancel.
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

            DataTable dataTable = SharedData.DataSet.Tables["game"];

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

        private void SetColumnsReadOnly(bool readOnly, params string[] columnNames)
        {
            foreach (string name in columnNames)
            {
                if (dataGridView1.Columns.Contains(name))
                {
                    dataGridView1.Columns[name].ReadOnly = readOnly;
                    if (!readOnly)
                    {
                        dataGridView1.Columns[name].DefaultCellStyle.ForeColor = Color.Blue;
                    }
                    else
                    {
                        dataGridView1.Columns[name].DefaultCellStyle.ForeColor = Color.Black;
                    }
                }
            }
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
                return;
            }

            if (dataGridView1.SelectedRows.Count != 1 || e.RowIndex < 0)
            {
                return;
            }

            ChangeHiddenBoolValue(columnName, e.RowIndex);
            SharedData.IsDataChanged = true;
            UpdateCounters();
        }

        private void ChangeHiddenBoolValue(string columnName, int columnIndex)
        {
            var hiddenValue = dataGridView1.Rows[columnIndex].Cells[columnName].Value;
            //Get the path value so we can lookup the row in the table and change it there
            var pathValue = dataGridView1.Rows[columnIndex].Cells["path"].Value;

            bool currentValue = hiddenValue as bool? ?? false;

            string path = (string)pathValue;

            // Find the corresponding row in the dataSet
            DataRow[] rows = SharedData.DataSet.Tables["game"].Select($"path = '{path.Replace("'", "''")}'");

            if (rows.Length > 0)
            {
                DataRow tabledata = rows[0];
                tabledata[columnName] = currentValue ? (object)DBNull.Value : true;
            }

        }

        private void ScraperDatesToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            bool isVisible = ScraperDatesToolStripMenuItem.Checked;
            
            dataGridView1.Columns.Cast<DataGridViewColumn>()
            .Where(column => column.Name.StartsWith("scrap_"))
            .ToList()
            .ForEach(column => column.Visible = isVisible);
        }

        private void ClearScraperDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string items = (dataGridView1.SelectedRows.Count == 1) ? "item?" : "items?";

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
           List<string> allPathValues = DataGridViewSelectedRowsToPathsList();

            if (allPathValues == null )
            {
                return;
            }

            var matchingRows = from DataRow row in SharedData.DataSet.Tables["game"].Rows
                               let pathValue = row.Field<string>("path")
                               where allPathValues.Contains(pathValue)
                               select row;

            foreach (var row in matchingRows)
            {
                foreach (DataColumn column in row.Table.Columns)
                {
                    if (column.ColumnName.StartsWith("scrap_"))
                    {
                        row[column] = string.IsNullOrEmpty(date) ? (object)DBNull.Value : date;
                    }
                }
            }

            SharedData.DataSet.AcceptChanges();

            MessageBox.Show("Scraper dates have been updated!", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateScraperDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string items = (dataGridView1.SelectedRows.Count == 1) ? "item?" : "items?";

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

        private void ToolStripMenuItemSetVisble_Click(object sender, EventArgs e)
        {
            SetSelectedVisibility(false);
        }

        private void SetAllItemsHiddenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSelectedVisibility(true);
        }

        private void SetSelectedVisibility(bool visible)
        {
            if (dataGridView1.SelectedRows.Count < 1)
            {
                return;
            }

            string item = (dataGridView1.SelectedRows.Count > 1) ? "items" : "item";
            string visibility = visible ? "hidden" : "visible";


            DialogResult result = MessageBox.Show($"Do you want to set the selected {item} {visibility}?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            List<string> pathList = DataGridViewSelectedRowsToPathsList();

            var matchingRows = from DataRow row in SharedData.DataSet.Tables["game"].AsEnumerable()
                               let pathValue = row.Field<string>("path")
                               where pathList.Contains(pathValue)
                               select row;

            foreach (var row in matchingRows)
            {
                row["hidden"] = visible;
            }

            SharedData.IsDataChanged = true;
            UpdateCounters();
        }


        private void DescriptionToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            splitContainerSmall.Panel2Collapsed = !DescriptionToolStripMenuItem.Checked;
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

            this.Cursor = Cursors.WaitCursor;

            dataGridView1.Enabled = false;
            menuStripMainMenu.Enabled = false;
            panelBelowDataGridView.Enabled = false;

            try
            {
                statusBar.Text = "Started XML Import, please wait.....";
                string mameExePath = openFileDialog.FileName;
                gameNames = await Task.Run(() => GetMameUnplayable.GetFilteredGameNames(mameExePath));
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                UpdateStatusBar();
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dataGridView1.Enabled = true;
                menuStripMainMenu.Enabled = true;
                panelBelowDataGridView.Enabled = true;
                return;
            }

            if (gameNames == null || gameNames.Count == 0)
            {
                MessageBox.Show("No data was returned!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dataGridView1.Enabled = true;
                menuStripMainMenu.Enabled = true;
                panelBelowDataGridView.Enabled = true;
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
                Parallel.ForEach(SharedData.DataSet.Tables["game"].Rows.Cast<DataRow>(), (row) =>
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

            dataGridView1.Enabled = true;
            menuStripMainMenu.Enabled = true;
            panelBelowDataGridView.Enabled = true;

            SharedData.DataSet.AcceptChanges();

            // Resume the DataGridView layout
            dataGridView1.ResumeLayout();

            // Refresh the DataGridView to update the UI once all changes are made
            dataGridView1.Refresh();

            this.Cursor = Cursors.Default;

            UpdateStatusBar();
            dataGridView1.Columns["unplayable"].Visible = true;
            dataGridView1.Columns["unplayable"].SortMode = DataGridViewColumnSortMode.Automatic;
            dataGridView1.Columns["unplayable"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;

            DialogResult result = MessageBox.Show($"There were {unplayableCount} unplayable items found.\nDo you want to set them hidden?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.No)
            {
                UpdateCounters();
                return;
            }

            foreach (DataRow row in SharedData.DataSet.Tables["game"].Rows)
            {
                // Check if column is true
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
            if (saveGamelistToolStripMenuItem.Enabled)
            {
                saveGamelistToolStripMenuItem.Text = $"Save '{SharedData.XMLFilename}'";
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
            MediaCheckForm mediaCheckForm = new MediaCheckForm()
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
            if (string.IsNullOrEmpty(imagePath))
            {
                Clipboard.SetText(imagePath);
            }
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

        private void ToolStripMenuItemConnect_Click(object sender, EventArgs e)
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

            string sshPath = "C:\\Windows\\System32\\OpenSSH\\ssh.exe"; // path to ssh.exe on Windows

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

        private void ToolStripMenuItemGetVersion_Click(object sender, EventArgs e)
        {
            string command = "batocera-es-swissknife --version"; // Replace with your desired command
            string output = ExecuteSshCommand(command) as string;

            if (output == "connectfailed")
            {
                return;
            }

            MessageBox.Show($"Your Batocera is version {output}", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ToolStripMenuItemStopEmulators_Click(object sender, EventArgs e)
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

        private void ToolStripMenuItemShowUpdates_Click(object sender, EventArgs e)
        {
            string command = "batocera-es-swissknife --update"; // Replace with your desired command
            string output = ExecuteSshCommand(command) as string;

            if (output == "connectfailed")
            {
                return;
            }

            MessageBox.Show($"{output}", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ToolStripMenuItemShutdownHost_Click(object sender, EventArgs e)
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

        private void ToolStripMenuItemMapDrive_ClickAsync(object sender, EventArgs e)
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

            if (checkBoxCustomFilter.Checked)
            {
                textBoxCustomFilter.Enabled = true;
                textBoxCustomFilter.BackColor = SystemColors.Info;
                comboBoxGenre.Enabled = false;
                comboBoxFilterItem.Enabled = true;
                comboBoxGenre.SelectedIndex = 0;
            }
            else
            {
                comboBoxFilterItem.Enabled=false;
                textBoxCustomFilter.Enabled = false;
                textBoxCustomFilter.BackColor = SystemColors.Window;
                textBoxCustomFilter.Text = "";
                comboBoxGenre.Enabled = true;
            }

            ChangeGenreViaCombobox();

        }

        private void TextBox1_KeyUp(object sender, KeyEventArgs e)
        {
            string filterText = textBoxCustomFilter.Text;

            string filterItem = comboBoxFilterItem.Text.ToLower();
            if (filterItem == "description")
            {
                filterItem = "desc";
            }

            //selectedItem = selectedItem.Replace("'", "''");
            genreFilter = $"{filterItem} LIKE '*{filterText}*'";
            ApplyFilters();
        }

        private void OpenScraper_Click(object sender, EventArgs e)
        {
            showMediaToolStripMenuItem.Checked = false;
            ScraperForm scraper = new ScraperForm(this);
            scraper.FormClosed += ScraperForm_FormClosed;
            scraper.Owner = this;
            scraper.StartPosition = FormStartPosition.Manual;
            scraper.Location = new Point(this.Location.X + 50, this.Location.Y + 50);
            toolStripMenuItemFileMenu.Enabled = false;
            toolStripMenuItemScraperMenu.Enabled = false;
            toolStripMenuItemToolsMenu.Enabled = false;
            toolStripMenuItemRemoteMenu.Enabled = false;
            //panelBelowDataGridView.Enabled = false;
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

        private void ToolStripMenuItemFindItems_Click(object sender, EventArgs e)
        {

            DialogResult result = MessageBox.Show("This will check for additional items and add them to your gamelist.  Search criteria will be based upon file extensions used for any existing items.\n\n" +
                "Items listed in m3u files will be ignored.\n\nDo you want to continue?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);

            string[] m3uFiles = Directory.GetFiles(parentFolderPath, "*.m3u");

            // List to store contents of all M3U files
            List<string> m3uContents = new List<string>();
            // Read contents of each M3U file
            foreach (var m3uFile in m3uFiles)
            {
                // Read the contents of the file and add to the list
                string[] fileLines = File.ReadAllLines(m3uFile);
                m3uContents.AddRange(fileLines);
            }

            List<string> fileList = SharedData.DataSet.Tables["game"].AsEnumerable()
                                  .Select(row => ExtractFileNameWithExtension(row.Field<string>("path")))
                                  .Select(path => path.StartsWith("./") ? path.Substring(2) : path)
                                  .ToList();

            List<string> uniqueFileExtensions = new List<string>();
            foreach (string path in fileList)
            {
                // Extract the file extension using path.GetExtension
                string extension = System.IO.Path.GetExtension(path).TrimStart('.');
                if (!uniqueFileExtensions.Contains(extension))
                {
                    uniqueFileExtensions.Add(extension);
                }
            }

            List<string> newFileList = uniqueFileExtensions
               .SelectMany(ext => Directory.GetFiles(parentFolderPath, $"*.{ext}"))
               .Select(file => Path.GetFileName(file))
               .ToList();

            string m3uContentsString = string.Join(Environment.NewLine, fileList);

            newFileList.RemoveAll(file => fileList.Contains(file, StringComparer.OrdinalIgnoreCase));
            newFileList.RemoveAll(file => m3uContents.Contains(file, StringComparer.OrdinalIgnoreCase));

            if (newFileList.Count == 0)
            {
                MessageBox.Show("No additional items were found", "Notice:", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (string fileName in newFileList)
            {
                string newName = MakeName(fileName);
                DataRow newRow = SharedData.DataSet.Tables["game"].NewRow();
                newRow["name"] = newName;
                newRow["path"] = $"./{fileName}";
                // Add the new row to the Rows collection of the DataTable
                SharedData.DataSet.Tables["game"].Rows.Add(newRow);
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
            foreach (DataRow row in SharedData.DataSet.Tables["game"].Rows)
            {
                pathList.Add(row["path"].ToString());
                nameList.Add(row["name"].ToString());
            }

            dataGridView1.DataSource = null;

            List<string> pathValues = SharedData.DataSet.Tables["game"].AsEnumerable()
              .Select(row => row.Field<string>("path"))
              .ToList();

            SharedData.DataSet.Tables["game"].DefaultView.RowFilter = null;
            SharedData.DataSet.Clear();

            SetupTableColumns();

            for (int i = 0; i < pathValues.Count; i++)
            {
                DataRow newRow = SharedData.DataSet.Tables["game"].NewRow();
                newRow["path"] = pathList[i];
                newRow["name"] = MakeName(pathList[i]);
                SharedData.DataSet.Tables["game"].Rows.Add(newRow);
            }

            SharedData.DataSet.AcceptChanges();

            dataGridView1.DataSource = SharedData.DataSet.Tables["game"];
            SetupDataGridViewColumns();
            BuildCombobox();
            ResetForm();
            UpdateCounters();

            SharedData.IsDataChanged = true;

        }

        private void TextBox_CustomFilter_KeyPress(object sender, KeyPressEventArgs e)
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

        private async void ToolStripMenuItemFindMissing_Click(object sender, EventArgs e)
        {

            int missingCount = 0;
            int totalItemCount = SharedData.DataSet.Tables["game"].Rows.Count;

            this.Cursor = Cursors.WaitCursor;

            string parentFolderPath = Path.GetDirectoryName(SharedData.XMLFilename);

            List<DataRow> rowsToUpdate = new List<DataRow>();


            await Task.Run(() =>
               {
                   // Loop through each row in the DataTable
                   Parallel.ForEach(SharedData.DataSet.Tables["game"].Rows.Cast<DataRow>(), (row) =>
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


            this.Cursor = Cursors.Default;

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

        private void GamelistManagerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (SharedData.IsDataChanged)
            {
                bool result = SaveReminder();
                if (result)
                {
                    e.Cancel = true;
                }
            }
        }

        private void ToolStripMenuItemResetNames_Click(object sender, EventArgs e)
        {
            string message = "This will reset the names of selected items and remove special characters.\n\nDo you want to proceed?";

            DialogResult result = MessageBox.Show(message, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                string newName = MakeName(row.Cells["path"].Value.ToString());
                row.Cells["name"].Value = newName;
            }
        }

        private string MakeName(string oldName)
        {
            int lastDotIndex = oldName.LastIndexOf('.');
            if (lastDotIndex == -1)
            {
                lastDotIndex = oldName.Length;
            }
            string newName = oldName.Substring(0, lastDotIndex).Trim();
            newName = Regex.Replace(newName, @"[^\w\s]+", ""); // Remove non-alphanumeric characters
            newName = Regex.Replace(newName, @"\s+", " "); // Replace multiple spaces with a single space
            return newName;
        }


        private void ToolStripMenuItemExportToCSV_Click(object sender, EventArgs e)
        {
            string parentFolderName = Path.GetFileName(Path.GetDirectoryName(SharedData.XMLFilename));
            string csvFileName = Directory.GetCurrentDirectory() + "\\" + $"{parentFolderName}_export.csv";

            try
            {
                using (var csvContent = new StreamWriter(csvFileName))
                {
                    DataTable dataTable = SharedData.DataSet.Tables["game"];

                    List<string> excludedColumns = new List<string> { "desc", "missing", "unplayable" };

                    // Write the header (column names)
                    var columnNames = dataTable.Columns.Cast<DataColumn>()
                        .Where(col => !excludedColumns.Contains(col.ColumnName.ToLower()))
                        .Select(col => EscapeCsvField(col.ColumnName));
                    csvContent.WriteLine(string.Join(",", columnNames));

                    // Write the data rows
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var fields = row.Table.Columns.Cast<DataColumn>()
                            .Where(col => !excludedColumns.Contains(col.ColumnName.ToLower()))
                            .Select(col => EscapeCsvField(row[col].ToString()));

                        csvContent.WriteLine(string.Join(",", fields));
                    }

                    MessageBox.Show($"The file '{csvFileName}' was successfully saved", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    string output = CommandExecutor.ExecuteCommand("explorer.exe", $"/select, \"{csvFileName}\"");

                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"UnauthorizedAccessException: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"IOException: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string EscapeCsvField(string field)
        {
            // If the field contains a comma, double-quote, or newline, enclose it in double-quotes
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        private void ToolStripMenuItemCreateM3UFile_Click(object sender, EventArgs e)
        {

            List<string> pathValues = DataGridViewSelectedRowsToPathsList();

            if (pathValues == null)
            {
                return;
            }

            string m3uFileName = pathValues[0].ToString() + ".m3u";

            string[] fileArray = pathValues.ToArray();
            string fileNames = (string.Join(Environment.NewLine, fileArray)).Replace("./", "");

            DialogResult result = MessageBox.Show($"Do you want create the following M3U file?\n\n" +
                $"Filename: {m3uFileName}\n\nWith Files:\n{fileNames}", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string filePath = Path.GetDirectoryName(SharedData.XMLFilename) + "\\" + m3uFileName;
            File.WriteAllText(filePath, fileNames);

            DataRow[] firstRow = SharedData.DataSet.Tables["game"].Select($"path = '{fileArray[0]}'");
            DataRow newRow = SharedData.DataSet.Tables["game"].NewRow();
            newRow.ItemArray = firstRow[0].ItemArray;
            newRow["path"] = $"./{m3uFileName}";
            SharedData.DataSet.Tables["game"].Rows.Add(newRow);

            DeleteGameRowsByPath(pathValues);
            UpdateCounters();

        }

        private void DeleteGameRowsByPath(List<string> romPaths)
        {
            List<DataRow> rowsToRemove = new List<DataRow>();

            foreach (string romPath in romPaths)
            {
                var matchingRows = SharedData.DataSet.Tables["game"]
                    .AsEnumerable()
                    .Where(row => row.Field<string>("path") == romPath.Replace("'", "''"))
                    .ToList();  // Convert to list to avoid deferred execution

                rowsToRemove.AddRange(matchingRows);
            }

            foreach (var rowToRemove in rowsToRemove)
            {
                rowToRemove.Delete();
            }

            SharedData.DataSet.AcceptChanges();
        }


        private List<string> DataGridViewSelectedRowsToPathsList()
        {
            // This is used to get the paths from datagridvieiw selected rows
            // The paths are then used to locate data in the dataset table
            // As opposed to modifying the datagridview itself.  
            // Just to keep it all consistent
            DataGridViewSelectedRowCollection rows = dataGridView1.SelectedRows;

            if (rows.Count < 1)
            {
                return null;
            }

            List<string> pathValues = rows
             .Cast<DataGridViewRow>()
             .Select(row => row.Cells["path"].Value?.ToString())
            .ToList();

            return pathValues;
        }

        private void ToolStripMenuItemDeleteRows_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count < 1) { return; }

            int count = dataGridView1.SelectedRows.Count;
            string item = (count == 1) ? "item" : "items";
            string has = (count == 1) ? "has" : "have";

            DialogResult result = MessageBox.Show($"Do you want remove the selected {item} from the gamelist?\n\nNo files are deleted.", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            List<string> pathValues = DataGridViewSelectedRowsToPathsList();

            DeleteGameRowsByPath(pathValues);

            UpdateCounters();

            MessageBox.Show($"{count} {item} {has} been removed!", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);

            SharedData.IsDataChanged = true;

        }

        private void resetViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetForm();
        }

        private void editRowDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool readOnly = dataGridView1.Columns["name"].ReadOnly;
            // Flip the bool
            readOnly = !readOnly;
            DisableEditing(readOnly);
        }

        private void DisableEditing(bool readOnly)
        {
            SetColumnsReadOnly(readOnly, "id", "name", "genre", "players", "rating", "lang", "region", "publisher");
            richTextBoxDescription.ReadOnly = readOnly;
            if (!readOnly)
            {
                richTextBoxDescription.ForeColor = Color.Blue;
                richTextBoxDescription.ReadOnly = false;
                //dataGridView1.MultiSelect = false;
            }
            else
            {
                richTextBoxDescription.ForeColor = Color.Black;
                richTextBoxDescription.ReadOnly = true;
                //dataGridView1.MultiSelect = true;
            }

            SharedData.IsDataChanged = true;

        }

        private void comboBox1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            textBoxCustomFilter.Text = "";
        }

        private void richTextBoxDescription_Leave(object sender, EventArgs e)
        {
            object tagValue = richTextBoxDescription.Tag;

            if (tagValue == null)
            {
                return;
            }

            int index;

            if (!int.TryParse(tagValue.ToString(), out index))
            {
                return;
            }
               
            string displayedDescription = richTextBoxDescription.Text;
            object cellValue = dataGridView1.Rows[index].Cells["desc"].Value;
            string currentDescription = (cellValue != DBNull.Value) ? Convert.ToString(cellValue) : string.Empty;
            if (displayedDescription != currentDescription)
            {
                dataGridView1.Rows[index].Cells["desc"].Value = displayedDescription;
            }
            
        }
    }
}





