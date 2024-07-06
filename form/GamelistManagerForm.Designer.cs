namespace GamelistManager
{
    partial class GamelistManagerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GamelistManagerForm));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.richTextBoxDescription = new System.Windows.Forms.RichTextBox();
            this.comboBoxGenre = new System.Windows.Forms.ComboBox();
            this.labelShowingCount = new System.Windows.Forms.Label();
            this.labelShowing = new System.Windows.Forms.Label();
            this.labelHiddenCount = new System.Windows.Forms.Label();
            this.labelVisibleCount = new System.Windows.Forms.Label();
            this.labelHidden = new System.Windows.Forms.Label();
            this.splitContainerBig = new System.Windows.Forms.SplitContainer();
            this.panelBelowDataGridView = new System.Windows.Forms.Panel();
            this.comboBoxFilterItem = new System.Windows.Forms.ComboBox();
            this.textBoxCustomFilter = new System.Windows.Forms.TextBox();
            this.checkBoxCustomFilter = new System.Windows.Forms.CheckBox();
            this.labelGenrePicker = new System.Windows.Forms.Label();
            this.labelVisible = new System.Windows.Forms.Label();
            this.pictureBoxSystemLogo = new System.Windows.Forms.PictureBox();
            this.labelFavoriteCount = new System.Windows.Forms.Label();
            this.labelFavorite = new System.Windows.Forms.Label();
            this.splitContainerSmall = new System.Windows.Forms.SplitContainer();
            this.panelMediaBackground = new System.Windows.Forms.Panel();
            this.menuStripMainMenu = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItemFileMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripFile = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.loadGamelistToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadGamelistToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveGamelistToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator25 = new System.Windows.Forms.ToolStripSeparator();
            this.exportToCSVToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator26 = new System.Windows.Forms.ToolStripSeparator();
            this.clearRecentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItemViewMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.showAllHiddenAndVisibleItemsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showVisibleItemsOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showHiddenItemsOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
            this.showAllGenresToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showGenreOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator21 = new System.Windows.Forms.ToolStripSeparator();
            this.showMediaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator27 = new System.Windows.Forms.ToolStripSeparator();
            this.ToolStripMenuItemAlwaysOnTop = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator14 = new System.Windows.Forms.ToolStripSeparator();
            this.resetViewToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator15 = new System.Windows.Forms.ToolStripSeparator();
            this.dataGridColoringToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripColorComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripMenuItemEditMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripEdit = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.setAllItemsVisibleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setAllItemsHiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.setAllGenreVisibleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setAllGenreHiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.editRowDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator24 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteRowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator28 = new System.Windows.Forms.ToolStripSeparator();
            this.clearScraperDateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateScraperDateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator29 = new System.Windows.Forms.ToolStripSeparator();
            this.resetNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator30 = new System.Windows.Forms.ToolStripSeparator();
            this.clearAllDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemColumnsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripColumns = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.DeveloperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FavoriteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.GameTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.GenreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LangToolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.LastPlayedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PublisherToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PlayCountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PlayersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.RatingToolStripMenuItem10 = new System.Windows.Forms.ToolStripMenuItem();
            this.RegionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ReleaseDateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.DescriptionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem14 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.MediaPathsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.ScraperDatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemToolsMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripTools = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.MameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MediaCheckToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.FindNewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.FindMissingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.CreateM3UToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemRemoteMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripRemote = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.removeBatoceraHostKeyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator18 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator19 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemScraperMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripScraper = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openScraperToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripImageOptions = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItemCopyToClipboard = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemEditImage = new System.Windows.Forms.ToolStripMenuItem();
            this.statusBar = new System.Windows.Forms.StatusBar();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBig)).BeginInit();
            this.splitContainerBig.Panel1.SuspendLayout();
            this.splitContainerBig.Panel2.SuspendLayout();
            this.splitContainerBig.SuspendLayout();
            this.panelBelowDataGridView.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSystemLogo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerSmall)).BeginInit();
            this.splitContainerSmall.Panel1.SuspendLayout();
            this.splitContainerSmall.Panel2.SuspendLayout();
            this.splitContainerSmall.SuspendLayout();
            this.menuStripMainMenu.SuspendLayout();
            this.contextMenuStripFile.SuspendLayout();
            this.contextMenuStripView.SuspendLayout();
            this.contextMenuStripEdit.SuspendLayout();
            this.contextMenuStripColumns.SuspendLayout();
            this.contextMenuStripTools.SuspendLayout();
            this.contextMenuStripRemote.SuspendLayout();
            this.contextMenuStripScraper.SuspendLayout();
            this.contextMenuStripImageOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.dataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridView1.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            this.dataGridView1.Name = "dataGridView1";
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridView1.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowHeadersWidth = 123;
            this.dataGridView1.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dataGridView1.RowTemplate.Height = 20;
            this.dataGridView1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(538, 137);
            this.dataGridView1.TabIndex = 1;
            this.dataGridView1.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGridView1_CellClick);
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.DataGridView1_SelectionChanged);
            // 
            // richTextBoxDescription
            // 
            this.richTextBoxDescription.BackColor = System.Drawing.Color.WhiteSmoke;
            this.richTextBoxDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxDescription.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxDescription.Name = "richTextBoxDescription";
            this.richTextBoxDescription.ReadOnly = true;
            this.richTextBoxDescription.Size = new System.Drawing.Size(159, 137);
            this.richTextBoxDescription.TabIndex = 0;
            this.richTextBoxDescription.Text = "";
            this.richTextBoxDescription.Leave += new System.EventHandler(this.richTextBoxDescription_Leave);
            // 
            // comboBoxGenre
            // 
            this.comboBoxGenre.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.comboBoxGenre.BackColor = System.Drawing.Color.LightCyan;
            this.comboBoxGenre.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxGenre.Enabled = false;
            this.comboBoxGenre.FormattingEnabled = true;
            this.comboBoxGenre.ItemHeight = 13;
            this.comboBoxGenre.Location = new System.Drawing.Point(460, 2);
            this.comboBoxGenre.Name = "comboBoxGenre";
            this.comboBoxGenre.Size = new System.Drawing.Size(215, 21);
            this.comboBoxGenre.TabIndex = 10;
            this.comboBoxGenre.SelectedIndexChanged += new System.EventHandler(this.ComboBox1_SelectedIndexChanged);
            // 
            // labelShowingCount
            // 
            this.labelShowingCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelShowingCount.AutoSize = true;
            this.labelShowingCount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelShowingCount.ForeColor = System.Drawing.Color.Red;
            this.labelShowingCount.Location = new System.Drawing.Point(329, 4);
            this.labelShowingCount.Name = "labelShowingCount";
            this.labelShowingCount.Size = new System.Drawing.Size(13, 15);
            this.labelShowingCount.TabIndex = 9;
            this.labelShowingCount.Text = "0";
            // 
            // labelShowing
            // 
            this.labelShowing.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelShowing.AutoSize = true;
            this.labelShowing.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelShowing.Location = new System.Drawing.Point(276, 4);
            this.labelShowing.Name = "labelShowing";
            this.labelShowing.Size = new System.Drawing.Size(56, 15);
            this.labelShowing.TabIndex = 8;
            this.labelShowing.Text = "Showing:";
            // 
            // labelHiddenCount
            // 
            this.labelHiddenCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelHiddenCount.AutoSize = true;
            this.labelHiddenCount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHiddenCount.ForeColor = System.Drawing.Color.Red;
            this.labelHiddenCount.Location = new System.Drawing.Point(232, 26);
            this.labelHiddenCount.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.labelHiddenCount.Name = "labelHiddenCount";
            this.labelHiddenCount.Size = new System.Drawing.Size(13, 15);
            this.labelHiddenCount.TabIndex = 6;
            this.labelHiddenCount.Text = "0";
            // 
            // labelVisibleCount
            // 
            this.labelVisibleCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelVisibleCount.AutoSize = true;
            this.labelVisibleCount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelVisibleCount.ForeColor = System.Drawing.Color.Red;
            this.labelVisibleCount.Location = new System.Drawing.Point(232, 4);
            this.labelVisibleCount.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.labelVisibleCount.Name = "labelVisibleCount";
            this.labelVisibleCount.Size = new System.Drawing.Size(13, 15);
            this.labelVisibleCount.TabIndex = 5;
            this.labelVisibleCount.Text = "0";
            // 
            // labelHidden
            // 
            this.labelHidden.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelHidden.AutoSize = true;
            this.labelHidden.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHidden.Location = new System.Drawing.Point(186, 26);
            this.labelHidden.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.labelHidden.Name = "labelHidden";
            this.labelHidden.Size = new System.Drawing.Size(49, 15);
            this.labelHidden.TabIndex = 4;
            this.labelHidden.Text = "Hidden:";
            // 
            // splitContainerBig
            // 
            this.splitContainerBig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerBig.Location = new System.Drawing.Point(0, 24);
            this.splitContainerBig.Name = "splitContainerBig";
            this.splitContainerBig.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerBig.Panel1
            // 
            this.splitContainerBig.Panel1.Controls.Add(this.panelBelowDataGridView);
            this.splitContainerBig.Panel1.Controls.Add(this.splitContainerSmall);
            // 
            // splitContainerBig.Panel2
            // 
            this.splitContainerBig.Panel2.Controls.Add(this.panelMediaBackground);
            this.splitContainerBig.Panel2MinSize = 125;
            this.splitContainerBig.Size = new System.Drawing.Size(704, 317);
            this.splitContainerBig.SplitterDistance = 187;
            this.splitContainerBig.TabIndex = 3;
            // 
            // panelBelowDataGridView
            // 
            this.panelBelowDataGridView.BackColor = System.Drawing.Color.LightGray;
            this.panelBelowDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelBelowDataGridView.Controls.Add(this.comboBoxFilterItem);
            this.panelBelowDataGridView.Controls.Add(this.textBoxCustomFilter);
            this.panelBelowDataGridView.Controls.Add(this.checkBoxCustomFilter);
            this.panelBelowDataGridView.Controls.Add(this.comboBoxGenre);
            this.panelBelowDataGridView.Controls.Add(this.labelGenrePicker);
            this.panelBelowDataGridView.Controls.Add(this.labelVisible);
            this.panelBelowDataGridView.Controls.Add(this.pictureBoxSystemLogo);
            this.panelBelowDataGridView.Controls.Add(this.labelHidden);
            this.panelBelowDataGridView.Controls.Add(this.labelHiddenCount);
            this.panelBelowDataGridView.Controls.Add(this.labelShowingCount);
            this.panelBelowDataGridView.Controls.Add(this.labelVisibleCount);
            this.panelBelowDataGridView.Controls.Add(this.labelFavoriteCount);
            this.panelBelowDataGridView.Controls.Add(this.labelFavorite);
            this.panelBelowDataGridView.Controls.Add(this.labelShowing);
            this.panelBelowDataGridView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBelowDataGridView.Location = new System.Drawing.Point(0, 139);
            this.panelBelowDataGridView.Name = "panelBelowDataGridView";
            this.panelBelowDataGridView.Size = new System.Drawing.Size(704, 48);
            this.panelBelowDataGridView.TabIndex = 12;
            // 
            // comboBoxFilterItem
            // 
            this.comboBoxFilterItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFilterItem.Enabled = false;
            this.comboBoxFilterItem.FormattingEnabled = true;
            this.comboBoxFilterItem.Items.AddRange(new object[] {
            "Name",
            "Path",
            "Genre",
            "Description"});
            this.comboBoxFilterItem.Location = new System.Drawing.Point(460, 24);
            this.comboBoxFilterItem.Name = "comboBoxFilterItem";
            this.comboBoxFilterItem.Size = new System.Drawing.Size(85, 21);
            this.comboBoxFilterItem.TabIndex = 17;
            this.comboBoxFilterItem.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged_1);
            // 
            // textBoxCustomFilter
            // 
            this.textBoxCustomFilter.Enabled = false;
            this.textBoxCustomFilter.Location = new System.Drawing.Point(551, 24);
            this.textBoxCustomFilter.MaxLength = 20;
            this.textBoxCustomFilter.Name = "textBoxCustomFilter";
            this.textBoxCustomFilter.Size = new System.Drawing.Size(124, 20);
            this.textBoxCustomFilter.TabIndex = 16;
            this.textBoxCustomFilter.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TextBox_CustomFilter_KeyPress);
            this.textBoxCustomFilter.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TextBox1_KeyUp);
            // 
            // checkBoxCustomFilter
            // 
            this.checkBoxCustomFilter.AutoSize = true;
            this.checkBoxCustomFilter.Enabled = false;
            this.checkBoxCustomFilter.ForeColor = System.Drawing.Color.DarkBlue;
            this.checkBoxCustomFilter.Location = new System.Drawing.Point(405, 26);
            this.checkBoxCustomFilter.Name = "checkBoxCustomFilter";
            this.checkBoxCustomFilter.Size = new System.Drawing.Size(51, 17);
            this.checkBoxCustomFilter.TabIndex = 15;
            this.checkBoxCustomFilter.Text = "Filter:";
            this.checkBoxCustomFilter.UseVisualStyleBackColor = true;
            this.checkBoxCustomFilter.CheckedChanged += new System.EventHandler(this.CheckBox_CustomFilter_CheckedChanged);
            // 
            // labelGenrePicker
            // 
            this.labelGenrePicker.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelGenrePicker.AutoSize = true;
            this.labelGenrePicker.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelGenrePicker.ForeColor = System.Drawing.Color.DarkBlue;
            this.labelGenrePicker.Location = new System.Drawing.Point(411, 4);
            this.labelGenrePicker.Name = "labelGenrePicker";
            this.labelGenrePicker.Size = new System.Drawing.Size(41, 15);
            this.labelGenrePicker.TabIndex = 11;
            this.labelGenrePicker.Text = "Genre:";
            // 
            // labelVisible
            // 
            this.labelVisible.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelVisible.AutoSize = true;
            this.labelVisible.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelVisible.Location = new System.Drawing.Point(186, 4);
            this.labelVisible.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.labelVisible.Name = "labelVisible";
            this.labelVisible.Size = new System.Drawing.Size(44, 15);
            this.labelVisible.TabIndex = 3;
            this.labelVisible.Text = "Visible:";
            // 
            // pictureBoxSystemLogo
            // 
            this.pictureBoxSystemLogo.Location = new System.Drawing.Point(2, 2);
            this.pictureBoxSystemLogo.Name = "pictureBoxSystemLogo";
            this.pictureBoxSystemLogo.Size = new System.Drawing.Size(180, 43);
            this.pictureBoxSystemLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxSystemLogo.TabIndex = 14;
            this.pictureBoxSystemLogo.TabStop = false;
            // 
            // labelFavoriteCount
            // 
            this.labelFavoriteCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelFavoriteCount.AutoSize = true;
            this.labelFavoriteCount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFavoriteCount.ForeColor = System.Drawing.Color.Red;
            this.labelFavoriteCount.Location = new System.Drawing.Point(329, 26);
            this.labelFavoriteCount.Name = "labelFavoriteCount";
            this.labelFavoriteCount.Size = new System.Drawing.Size(13, 15);
            this.labelFavoriteCount.TabIndex = 13;
            this.labelFavoriteCount.Text = "0";
            // 
            // labelFavorite
            // 
            this.labelFavorite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelFavorite.AutoSize = true;
            this.labelFavorite.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFavorite.Location = new System.Drawing.Point(276, 26);
            this.labelFavorite.Name = "labelFavorite";
            this.labelFavorite.Size = new System.Drawing.Size(52, 15);
            this.labelFavorite.TabIndex = 12;
            this.labelFavorite.Text = "Favorite:";
            // 
            // splitContainerSmall
            // 
            this.splitContainerSmall.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainerSmall.Location = new System.Drawing.Point(0, 0);
            this.splitContainerSmall.Name = "splitContainerSmall";
            // 
            // splitContainerSmall.Panel1
            // 
            this.splitContainerSmall.Panel1.Controls.Add(this.dataGridView1);
            // 
            // splitContainerSmall.Panel2
            // 
            this.splitContainerSmall.Panel2.Controls.Add(this.richTextBoxDescription);
            this.splitContainerSmall.Size = new System.Drawing.Size(701, 137);
            this.splitContainerSmall.SplitterDistance = 538;
            this.splitContainerSmall.TabIndex = 2;
            // 
            // panelMediaBackground
            // 
            this.panelMediaBackground.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panelMediaBackground.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelMediaBackground.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMediaBackground.Location = new System.Drawing.Point(0, 0);
            this.panelMediaBackground.Name = "panelMediaBackground";
            this.panelMediaBackground.Size = new System.Drawing.Size(704, 126);
            this.panelMediaBackground.TabIndex = 0;
            // 
            // menuStripMainMenu
            // 
            this.menuStripMainMenu.BackColor = System.Drawing.Color.WhiteSmoke;
            this.menuStripMainMenu.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.menuStripMainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemFileMenu,
            this.toolStripMenuItemViewMenu,
            this.toolStripMenuItemEditMenu,
            this.toolStripMenuItemColumnsMenu,
            this.toolStripMenuItemToolsMenu,
            this.toolStripMenuItemRemoteMenu,
            this.toolStripMenuItemScraperMenu});
            this.menuStripMainMenu.Location = new System.Drawing.Point(0, 0);
            this.menuStripMainMenu.Name = "menuStripMainMenu";
            this.menuStripMainMenu.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.menuStripMainMenu.Size = new System.Drawing.Size(704, 24);
            this.menuStripMainMenu.TabIndex = 5;
            this.menuStripMainMenu.Text = "menuStrip1";
            // 
            // toolStripMenuItemFileMenu
            // 
            this.toolStripMenuItemFileMenu.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripMenuItemFileMenu.DropDown = this.contextMenuStripFile;
            this.toolStripMenuItemFileMenu.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItemFileMenu.Name = "toolStripMenuItemFileMenu";
            this.toolStripMenuItemFileMenu.ShowShortcutKeys = false;
            this.toolStripMenuItemFileMenu.Size = new System.Drawing.Size(37, 20);
            this.toolStripMenuItemFileMenu.Text = "File";
            this.toolStripMenuItemFileMenu.DropDownOpened += new System.EventHandler(this.FileToolStripMenuItem_DropDownOpened);
            // 
            // contextMenuStripFile
            // 
            this.contextMenuStripFile.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.contextMenuStripFile.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadGamelistToolStripMenuItem,
            this.reloadGamelistToolStripMenuItem,
            this.saveGamelistToolStripMenuItem,
            this.toolStripSeparator25,
            this.exportToCSVToolStripMenuItem,
            this.toolStripSeparator26,
            this.clearRecentFilesToolStripMenuItem,
            this.toolStripSeparator12});
            this.contextMenuStripFile.Name = "MenuStripFile";
            this.contextMenuStripFile.OwnerItem = this.toolStripMenuItemFileMenu;
            this.contextMenuStripFile.ShowImageMargin = false;
            this.contextMenuStripFile.Size = new System.Drawing.Size(142, 132);
            // 
            // loadGamelistToolStripMenuItem
            // 
            this.loadGamelistToolStripMenuItem.Name = "loadGamelistToolStripMenuItem";
            this.loadGamelistToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.loadGamelistToolStripMenuItem.Text = "Load Gamelist";
            this.loadGamelistToolStripMenuItem.Click += new System.EventHandler(this.LoadGamelistXMLToolStripMenuItem_Click);
            // 
            // reloadGamelistToolStripMenuItem
            // 
            this.reloadGamelistToolStripMenuItem.Name = "reloadGamelistToolStripMenuItem";
            this.reloadGamelistToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.reloadGamelistToolStripMenuItem.Text = "Reload Gamelist";
            this.reloadGamelistToolStripMenuItem.Click += new System.EventHandler(this.ReloadGamelistxmlToolStripMenuItem_Click);
            // 
            // saveGamelistToolStripMenuItem
            // 
            this.saveGamelistToolStripMenuItem.Name = "saveGamelistToolStripMenuItem";
            this.saveGamelistToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.saveGamelistToolStripMenuItem.Text = "Save Gamelist";
            this.saveGamelistToolStripMenuItem.Click += new System.EventHandler(this.SaveFileToolStripMenuItem_Click);
            // 
            // toolStripSeparator25
            // 
            this.toolStripSeparator25.Name = "toolStripSeparator25";
            this.toolStripSeparator25.Size = new System.Drawing.Size(138, 6);
            // 
            // exportToCSVToolStripMenuItem
            // 
            this.exportToCSVToolStripMenuItem.Name = "exportToCSVToolStripMenuItem";
            this.exportToCSVToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.exportToCSVToolStripMenuItem.Text = "Export To CSV";
            this.exportToCSVToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItemExportToCSV_Click);
            // 
            // toolStripSeparator26
            // 
            this.toolStripSeparator26.Name = "toolStripSeparator26";
            this.toolStripSeparator26.Size = new System.Drawing.Size(138, 6);
            // 
            // clearRecentFilesToolStripMenuItem
            // 
            this.clearRecentFilesToolStripMenuItem.Name = "clearRecentFilesToolStripMenuItem";
            this.clearRecentFilesToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.clearRecentFilesToolStripMenuItem.Text = "Clear Recent Files";
            this.clearRecentFilesToolStripMenuItem.Click += new System.EventHandler(this.ClearRecentFilesToolStripMenuItem_Click_1);
            // 
            // toolStripSeparator12
            // 
            this.toolStripSeparator12.Name = "toolStripSeparator12";
            this.toolStripSeparator12.Size = new System.Drawing.Size(138, 6);
            // 
            // toolStripMenuItemViewMenu
            // 
            this.toolStripMenuItemViewMenu.DropDown = this.contextMenuStripView;
            this.toolStripMenuItemViewMenu.Name = "toolStripMenuItemViewMenu";
            this.toolStripMenuItemViewMenu.Size = new System.Drawing.Size(44, 20);
            this.toolStripMenuItemViewMenu.Text = "View";
            this.toolStripMenuItemViewMenu.DropDownOpening += new System.EventHandler(this.ViewToolStripMenuItem_DropDownOpening);
            // 
            // contextMenuStripView
            // 
            this.contextMenuStripView.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.contextMenuStripView.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showAllHiddenAndVisibleItemsToolStripMenuItem,
            this.showVisibleItemsOnlyToolStripMenuItem,
            this.showHiddenItemsOnlyToolStripMenuItem,
            this.toolStripSeparator13,
            this.showAllGenresToolStripMenuItem,
            this.showGenreOnlyToolStripMenuItem,
            this.toolStripSeparator21,
            this.showMediaToolStripMenuItem,
            this.toolStripSeparator27,
            this.ToolStripMenuItemAlwaysOnTop,
            this.toolStripSeparator14,
            this.resetViewToolStripMenuItem1,
            this.toolStripSeparator15,
            this.dataGridColoringToolStripMenuItem});
            this.contextMenuStripView.Name = "contextMenuStripView";
            this.contextMenuStripView.OwnerItem = this.toolStripMenuItemViewMenu;
            this.contextMenuStripView.ShowCheckMargin = true;
            this.contextMenuStripView.ShowImageMargin = false;
            this.contextMenuStripView.Size = new System.Drawing.Size(257, 232);
            // 
            // showAllHiddenAndVisibleItemsToolStripMenuItem
            // 
            this.showAllHiddenAndVisibleItemsToolStripMenuItem.Name = "showAllHiddenAndVisibleItemsToolStripMenuItem";
            this.showAllHiddenAndVisibleItemsToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.showAllHiddenAndVisibleItemsToolStripMenuItem.Text = "Show All Hidden And Visible Items";
            this.showAllHiddenAndVisibleItemsToolStripMenuItem.Click += new System.EventHandler(this.ShowAllItemsToolStripMenuItem_Click);
            // 
            // showVisibleItemsOnlyToolStripMenuItem
            // 
            this.showVisibleItemsOnlyToolStripMenuItem.Name = "showVisibleItemsOnlyToolStripMenuItem";
            this.showVisibleItemsOnlyToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.showVisibleItemsOnlyToolStripMenuItem.Text = "Show Visible Items Only";
            this.showVisibleItemsOnlyToolStripMenuItem.Click += new System.EventHandler(this.ShowVisibleItemsOnlyToolStripMenuItem_Click);
            // 
            // showHiddenItemsOnlyToolStripMenuItem
            // 
            this.showHiddenItemsOnlyToolStripMenuItem.Name = "showHiddenItemsOnlyToolStripMenuItem";
            this.showHiddenItemsOnlyToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.showHiddenItemsOnlyToolStripMenuItem.Text = "Show Hidden Items Only";
            this.showHiddenItemsOnlyToolStripMenuItem.Click += new System.EventHandler(this.ShowHiddenItemsOnlyToolStripMenuItem_Click);
            // 
            // toolStripSeparator13
            // 
            this.toolStripSeparator13.Name = "toolStripSeparator13";
            this.toolStripSeparator13.Size = new System.Drawing.Size(253, 6);
            // 
            // showAllGenresToolStripMenuItem
            // 
            this.showAllGenresToolStripMenuItem.Name = "showAllGenresToolStripMenuItem";
            this.showAllGenresToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.showAllGenresToolStripMenuItem.Text = "Show All Genres";
            this.showAllGenresToolStripMenuItem.Click += new System.EventHandler(this.ShowAllGenreToolStripMenuItem_Click);
            // 
            // showGenreOnlyToolStripMenuItem
            // 
            this.showGenreOnlyToolStripMenuItem.Name = "showGenreOnlyToolStripMenuItem";
            this.showGenreOnlyToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.showGenreOnlyToolStripMenuItem.Text = "Show Genre Only";
            this.showGenreOnlyToolStripMenuItem.Click += new System.EventHandler(this.ShowGenreOnlyToolStripMenuItem_Click);
            // 
            // toolStripSeparator21
            // 
            this.toolStripSeparator21.Name = "toolStripSeparator21";
            this.toolStripSeparator21.Size = new System.Drawing.Size(253, 6);
            // 
            // showMediaToolStripMenuItem
            // 
            this.showMediaToolStripMenuItem.CheckOnClick = true;
            this.showMediaToolStripMenuItem.Name = "showMediaToolStripMenuItem";
            this.showMediaToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.showMediaToolStripMenuItem.Text = "Show Media";
            this.showMediaToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.ShowMediaToolStripMenuItem_CheckStateChanged);
            // 
            // toolStripSeparator27
            // 
            this.toolStripSeparator27.Name = "toolStripSeparator27";
            this.toolStripSeparator27.Size = new System.Drawing.Size(253, 6);
            // 
            // ToolStripMenuItemAlwaysOnTop
            // 
            this.ToolStripMenuItemAlwaysOnTop.CheckOnClick = true;
            this.ToolStripMenuItemAlwaysOnTop.Name = "ToolStripMenuItemAlwaysOnTop";
            this.ToolStripMenuItemAlwaysOnTop.Size = new System.Drawing.Size(256, 22);
            this.ToolStripMenuItemAlwaysOnTop.Text = "Always On Top";
            this.ToolStripMenuItemAlwaysOnTop.CheckedChanged += new System.EventHandler(this.ToolStripMenuItemAlwaysOnTop_CheckedChanged);
            // 
            // toolStripSeparator14
            // 
            this.toolStripSeparator14.Name = "toolStripSeparator14";
            this.toolStripSeparator14.Size = new System.Drawing.Size(253, 6);
            // 
            // resetViewToolStripMenuItem1
            // 
            this.resetViewToolStripMenuItem1.Name = "resetViewToolStripMenuItem1";
            this.resetViewToolStripMenuItem1.Size = new System.Drawing.Size(256, 22);
            this.resetViewToolStripMenuItem1.Text = "Reset View";
            this.resetViewToolStripMenuItem1.Click += new System.EventHandler(this.resetViewToolStripMenuItem_Click);
            // 
            // toolStripSeparator15
            // 
            this.toolStripSeparator15.Name = "toolStripSeparator15";
            this.toolStripSeparator15.Size = new System.Drawing.Size(253, 6);
            // 
            // dataGridColoringToolStripMenuItem
            // 
            this.dataGridColoringToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripColorComboBox});
            this.dataGridColoringToolStripMenuItem.Name = "dataGridColoringToolStripMenuItem";
            this.dataGridColoringToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.dataGridColoringToolStripMenuItem.Text = "DataGrid Alternating Row Color";
            // 
            // toolStripColorComboBox
            // 
            this.toolStripColorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.toolStripColorComboBox.Name = "toolStripColorComboBox";
            this.toolStripColorComboBox.Size = new System.Drawing.Size(121, 23);
            this.toolStripColorComboBox.SelectedIndexChanged += new System.EventHandler(this.toolStripColorComboBox_SelectedIndexChanged);
            // 
            // toolStripMenuItemEditMenu
            // 
            this.toolStripMenuItemEditMenu.DropDown = this.contextMenuStripEdit;
            this.toolStripMenuItemEditMenu.Name = "toolStripMenuItemEditMenu";
            this.toolStripMenuItemEditMenu.Size = new System.Drawing.Size(39, 20);
            this.toolStripMenuItemEditMenu.Text = "Edit";
            this.toolStripMenuItemEditMenu.DropDownOpening += new System.EventHandler(this.EditToolStripMenuItem_DropDownOpening);
            // 
            // contextMenuStripEdit
            // 
            this.contextMenuStripEdit.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.contextMenuStripEdit.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.setAllItemsVisibleToolStripMenuItem,
            this.setAllItemsHiddenToolStripMenuItem,
            this.toolStripSeparator3,
            this.setAllGenreVisibleToolStripMenuItem,
            this.setAllGenreHiddenToolStripMenuItem,
            this.toolStripSeparator4,
            this.editRowDataToolStripMenuItem,
            this.toolStripSeparator24,
            this.deleteRowToolStripMenuItem,
            this.toolStripSeparator28,
            this.clearScraperDateToolStripMenuItem,
            this.updateScraperDateToolStripMenuItem,
            this.toolStripSeparator29,
            this.resetNameToolStripMenuItem,
            this.toolStripSeparator30,
            this.clearAllDataToolStripMenuItem});
            this.contextMenuStripEdit.Name = "contextMenuStripEdit";
            this.contextMenuStripEdit.OwnerItem = this.toolStripMenuItemEditMenu;
            this.contextMenuStripEdit.ShowImageMargin = false;
            this.contextMenuStripEdit.Size = new System.Drawing.Size(159, 260);
            // 
            // setAllItemsVisibleToolStripMenuItem
            // 
            this.setAllItemsVisibleToolStripMenuItem.Name = "setAllItemsVisibleToolStripMenuItem";
            this.setAllItemsVisibleToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.setAllItemsVisibleToolStripMenuItem.Text = "Set All Items Visible";
            this.setAllItemsVisibleToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItemSetVisble_Click);
            // 
            // setAllItemsHiddenToolStripMenuItem
            // 
            this.setAllItemsHiddenToolStripMenuItem.Name = "setAllItemsHiddenToolStripMenuItem";
            this.setAllItemsHiddenToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.setAllItemsHiddenToolStripMenuItem.Text = "Set All Items Hidden";
            this.setAllItemsHiddenToolStripMenuItem.Click += new System.EventHandler(this.SetAllItemsHiddenToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(155, 6);
            // 
            // setAllGenreVisibleToolStripMenuItem
            // 
            this.setAllGenreVisibleToolStripMenuItem.Name = "setAllGenreVisibleToolStripMenuItem";
            this.setAllGenreVisibleToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.setAllGenreVisibleToolStripMenuItem.Text = "Set All Genre Visible";
            this.setAllGenreVisibleToolStripMenuItem.Click += new System.EventHandler(this.SetAllGenreVisibleToolStripMenuItem_Click);
            // 
            // setAllGenreHiddenToolStripMenuItem
            // 
            this.setAllGenreHiddenToolStripMenuItem.Name = "setAllGenreHiddenToolStripMenuItem";
            this.setAllGenreHiddenToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.setAllGenreHiddenToolStripMenuItem.Text = "Set All Genre Hidden";
            this.setAllGenreHiddenToolStripMenuItem.Click += new System.EventHandler(this.SetAllGenreHiddenToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(155, 6);
            // 
            // editRowDataToolStripMenuItem
            // 
            this.editRowDataToolStripMenuItem.Name = "editRowDataToolStripMenuItem";
            this.editRowDataToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.editRowDataToolStripMenuItem.Text = "Edit Data";
            this.editRowDataToolStripMenuItem.Click += new System.EventHandler(this.editRowDataToolStripMenuItem_Click);
            // 
            // toolStripSeparator24
            // 
            this.toolStripSeparator24.Name = "toolStripSeparator24";
            this.toolStripSeparator24.Size = new System.Drawing.Size(155, 6);
            // 
            // deleteRowToolStripMenuItem
            // 
            this.deleteRowToolStripMenuItem.Name = "deleteRowToolStripMenuItem";
            this.deleteRowToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.deleteRowToolStripMenuItem.Text = "Remove Item";
            this.deleteRowToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItemDeleteRows_Click);
            // 
            // toolStripSeparator28
            // 
            this.toolStripSeparator28.Name = "toolStripSeparator28";
            this.toolStripSeparator28.Size = new System.Drawing.Size(155, 6);
            // 
            // clearScraperDateToolStripMenuItem
            // 
            this.clearScraperDateToolStripMenuItem.Name = "clearScraperDateToolStripMenuItem";
            this.clearScraperDateToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.clearScraperDateToolStripMenuItem.Text = "Clear Scraper Date";
            this.clearScraperDateToolStripMenuItem.Click += new System.EventHandler(this.ClearScraperDateToolStripMenuItem_Click);
            // 
            // updateScraperDateToolStripMenuItem
            // 
            this.updateScraperDateToolStripMenuItem.Name = "updateScraperDateToolStripMenuItem";
            this.updateScraperDateToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.updateScraperDateToolStripMenuItem.Text = "Update Scraper Date";
            this.updateScraperDateToolStripMenuItem.Click += new System.EventHandler(this.UpdateScraperDateToolStripMenuItem_Click);
            // 
            // toolStripSeparator29
            // 
            this.toolStripSeparator29.Name = "toolStripSeparator29";
            this.toolStripSeparator29.Size = new System.Drawing.Size(155, 6);
            // 
            // resetNameToolStripMenuItem
            // 
            this.resetNameToolStripMenuItem.Name = "resetNameToolStripMenuItem";
            this.resetNameToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.resetNameToolStripMenuItem.Text = "Reset Name";
            this.resetNameToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItemResetNames_Click);
            // 
            // toolStripSeparator30
            // 
            this.toolStripSeparator30.Name = "toolStripSeparator30";
            this.toolStripSeparator30.Size = new System.Drawing.Size(155, 6);
            // 
            // clearAllDataToolStripMenuItem
            // 
            this.clearAllDataToolStripMenuItem.Name = "clearAllDataToolStripMenuItem";
            this.clearAllDataToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.clearAllDataToolStripMenuItem.Text = "Clear All Data";
            this.clearAllDataToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItem_ClearAllData_Click);
            // 
            // toolStripMenuItemColumnsMenu
            // 
            this.toolStripMenuItemColumnsMenu.DropDown = this.contextMenuStripColumns;
            this.toolStripMenuItemColumnsMenu.Name = "toolStripMenuItemColumnsMenu";
            this.toolStripMenuItemColumnsMenu.Size = new System.Drawing.Size(67, 20);
            this.toolStripMenuItemColumnsMenu.Text = "Columns";
            // 
            // contextMenuStripColumns
            // 
            this.contextMenuStripColumns.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.contextMenuStripColumns.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.DeveloperToolStripMenuItem,
            this.FavoriteToolStripMenuItem,
            this.GameTimeToolStripMenuItem,
            this.GenreToolStripMenuItem,
            this.LangToolStripMenuItem5,
            this.LastPlayedToolStripMenuItem,
            this.PublisherToolStripMenuItem,
            this.PlayCountToolStripMenuItem,
            this.PlayersToolStripMenuItem,
            this.RatingToolStripMenuItem10,
            this.RegionToolStripMenuItem,
            this.ReleaseDateToolStripMenuItem,
            this.toolStripSeparator5,
            this.DescriptionToolStripMenuItem,
            this.toolStripSeparator6,
            this.toolStripMenuItem14,
            this.toolStripSeparator7,
            this.MediaPathsToolStripMenuItem,
            this.toolStripSeparator8,
            this.ScraperDatesToolStripMenuItem});
            this.contextMenuStripColumns.Name = "contextMenuStripColumns";
            this.contextMenuStripColumns.ShowCheckMargin = true;
            this.contextMenuStripColumns.ShowImageMargin = false;
            this.contextMenuStripColumns.Size = new System.Drawing.Size(146, 380);
            // 
            // DeveloperToolStripMenuItem
            // 
            this.DeveloperToolStripMenuItem.CheckOnClick = true;
            this.DeveloperToolStripMenuItem.Name = "DeveloperToolStripMenuItem";
            this.DeveloperToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.DeveloperToolStripMenuItem.Text = "Developer";
            this.DeveloperToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.DeveloperToolStripMenuItem_CheckedChanged);
            // 
            // FavoriteToolStripMenuItem
            // 
            this.FavoriteToolStripMenuItem.CheckOnClick = true;
            this.FavoriteToolStripMenuItem.Name = "FavoriteToolStripMenuItem";
            this.FavoriteToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.FavoriteToolStripMenuItem.Text = "Favorite";
            this.FavoriteToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.FavoriteToolStripMenuItem_CheckStateChanged);
            // 
            // GameTimeToolStripMenuItem
            // 
            this.GameTimeToolStripMenuItem.CheckOnClick = true;
            this.GameTimeToolStripMenuItem.Name = "GameTimeToolStripMenuItem";
            this.GameTimeToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.GameTimeToolStripMenuItem.Text = "Game Time";
            this.GameTimeToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.GametimeToolStripMenuItem_CheckStateChanged);
            // 
            // GenreToolStripMenuItem
            // 
            this.GenreToolStripMenuItem.CheckOnClick = true;
            this.GenreToolStripMenuItem.Name = "GenreToolStripMenuItem";
            this.GenreToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.GenreToolStripMenuItem.Text = "Genre";
            this.GenreToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.genreToolStripMenuItem_CheckedChanged);
            // 
            // LangToolStripMenuItem5
            // 
            this.LangToolStripMenuItem5.CheckOnClick = true;
            this.LangToolStripMenuItem5.Name = "LangToolStripMenuItem5";
            this.LangToolStripMenuItem5.Size = new System.Drawing.Size(145, 22);
            this.LangToolStripMenuItem5.Text = "Lang";
            this.LangToolStripMenuItem5.CheckStateChanged += new System.EventHandler(this.ToolStripMenuItemLanguage_CheckedChanged);
            // 
            // LastPlayedToolStripMenuItem
            // 
            this.LastPlayedToolStripMenuItem.CheckOnClick = true;
            this.LastPlayedToolStripMenuItem.Name = "LastPlayedToolStripMenuItem";
            this.LastPlayedToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.LastPlayedToolStripMenuItem.Text = "Last Played";
            this.LastPlayedToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.LastplayedToolStripMenuItem_CheckStateChanged);
            // 
            // PublisherToolStripMenuItem
            // 
            this.PublisherToolStripMenuItem.CheckOnClick = true;
            this.PublisherToolStripMenuItem.Name = "PublisherToolStripMenuItem";
            this.PublisherToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.PublisherToolStripMenuItem.Text = "Publisher";
            this.PublisherToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.PublisherToolStripMenuItem_CheckedChanged);
            // 
            // PlayCountToolStripMenuItem
            // 
            this.PlayCountToolStripMenuItem.CheckOnClick = true;
            this.PlayCountToolStripMenuItem.Name = "PlayCountToolStripMenuItem";
            this.PlayCountToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.PlayCountToolStripMenuItem.Text = "Play Count";
            this.PlayCountToolStripMenuItem.Click += new System.EventHandler(this.PlaycountToolStripMenuItem_CheckedChanged);
            // 
            // PlayersToolStripMenuItem
            // 
            this.PlayersToolStripMenuItem.CheckOnClick = true;
            this.PlayersToolStripMenuItem.Name = "PlayersToolStripMenuItem";
            this.PlayersToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.PlayersToolStripMenuItem.Text = "Players";
            this.PlayersToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.ToolStripMenuItemPlayers_CheckedChanged);
            // 
            // RatingToolStripMenuItem10
            // 
            this.RatingToolStripMenuItem10.CheckOnClick = true;
            this.RatingToolStripMenuItem10.Name = "RatingToolStripMenuItem10";
            this.RatingToolStripMenuItem10.Size = new System.Drawing.Size(145, 22);
            this.RatingToolStripMenuItem10.Text = "Rating";
            this.RatingToolStripMenuItem10.CheckStateChanged += new System.EventHandler(this.ToolStripMenuItemRating_CheckedChanged);
            // 
            // RegionToolStripMenuItem
            // 
            this.RegionToolStripMenuItem.CheckOnClick = true;
            this.RegionToolStripMenuItem.Name = "RegionToolStripMenuItem";
            this.RegionToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.RegionToolStripMenuItem.Text = "Region";
            this.RegionToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.RegionToolStripMenuItem_CheckedChanged);
            // 
            // ReleaseDateToolStripMenuItem
            // 
            this.ReleaseDateToolStripMenuItem.CheckOnClick = true;
            this.ReleaseDateToolStripMenuItem.Name = "ReleaseDateToolStripMenuItem";
            this.ReleaseDateToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.ReleaseDateToolStripMenuItem.Text = "Release Date";
            this.ReleaseDateToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.ReleaseDateToolStripMenuItem_CheckedChanged);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(142, 6);
            // 
            // DescriptionToolStripMenuItem
            // 
            this.DescriptionToolStripMenuItem.CheckOnClick = true;
            this.DescriptionToolStripMenuItem.Name = "DescriptionToolStripMenuItem";
            this.DescriptionToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.DescriptionToolStripMenuItem.Text = "Description";
            this.DescriptionToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.DescriptionToolStripMenuItem_CheckStateChanged);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(142, 6);
            // 
            // toolStripMenuItem14
            // 
            this.toolStripMenuItem14.CheckOnClick = true;
            this.toolStripMenuItem14.Name = "toolStripMenuItem14";
            this.toolStripMenuItem14.Size = new System.Drawing.Size(145, 22);
            this.toolStripMenuItem14.Text = "ID";
            this.toolStripMenuItem14.CheckStateChanged += new System.EventHandler(this.ToolStripMenuItemID_CheckedChanged);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(142, 6);
            // 
            // MediaPathsToolStripMenuItem
            // 
            this.MediaPathsToolStripMenuItem.CheckOnClick = true;
            this.MediaPathsToolStripMenuItem.Name = "MediaPathsToolStripMenuItem";
            this.MediaPathsToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.MediaPathsToolStripMenuItem.Text = "Media Paths";
            this.MediaPathsToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.MediaPathsToolStripMenuItem_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(142, 6);
            // 
            // ScraperDatesToolStripMenuItem
            // 
            this.ScraperDatesToolStripMenuItem.CheckOnClick = true;
            this.ScraperDatesToolStripMenuItem.Name = "ScraperDatesToolStripMenuItem";
            this.ScraperDatesToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
            this.ScraperDatesToolStripMenuItem.Text = "Scraper Dates";
            this.ScraperDatesToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.ScraperDatesToolStripMenuItem_CheckStateChanged);
            // 
            // toolStripMenuItemToolsMenu
            // 
            this.toolStripMenuItemToolsMenu.DropDown = this.contextMenuStripTools;
            this.toolStripMenuItemToolsMenu.Name = "toolStripMenuItemToolsMenu";
            this.toolStripMenuItemToolsMenu.Size = new System.Drawing.Size(46, 20);
            this.toolStripMenuItemToolsMenu.Text = "Tools";
            // 
            // contextMenuStripTools
            // 
            this.contextMenuStripTools.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.contextMenuStripTools.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MameToolStripMenuItem,
            this.toolStripSeparator1,
            this.MediaCheckToolStripMenuItem,
            this.toolStripSeparator2,
            this.FindNewToolStripMenuItem,
            this.FindMissingToolStripMenuItem,
            this.toolStripSeparator9,
            this.CreateM3UToolStripMenuItem});
            this.contextMenuStripTools.Name = "contextMenuStripTools";
            this.contextMenuStripTools.OwnerItem = this.toolStripMenuItemToolsMenu;
            this.contextMenuStripTools.ShowImageMargin = false;
            this.contextMenuStripTools.Size = new System.Drawing.Size(277, 132);
            // 
            // MameToolStripMenuItem
            // 
            this.MameToolStripMenuItem.Name = "MameToolStripMenuItem";
            this.MameToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.MameToolStripMenuItem.Text = "MAME: Identify Unplayable";
            this.MameToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItem_MameUnplayable_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(273, 6);
            // 
            // MediaCheckToolStripMenuItem
            // 
            this.MediaCheckToolStripMenuItem.Name = "MediaCheckToolStripMenuItem";
            this.MediaCheckToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.MediaCheckToolStripMenuItem.Text = "Check For Bad, Missing And Unused Media";
            this.MediaCheckToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItem_CheckImages_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(273, 6);
            // 
            // FindNewToolStripMenuItem
            // 
            this.FindNewToolStripMenuItem.Name = "FindNewToolStripMenuItem";
            this.FindNewToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.FindNewToolStripMenuItem.Text = "Find New Items";
            this.FindNewToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItemFindItems_Click);
            // 
            // FindMissingToolStripMenuItem
            // 
            this.FindMissingToolStripMenuItem.Name = "FindMissingToolStripMenuItem";
            this.FindMissingToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.FindMissingToolStripMenuItem.Text = "Find Missing Items";
            this.FindMissingToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItemFindMissing_Click);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            this.toolStripSeparator9.Size = new System.Drawing.Size(273, 6);
            // 
            // CreateM3UToolStripMenuItem
            // 
            this.CreateM3UToolStripMenuItem.Name = "CreateM3UToolStripMenuItem";
            this.CreateM3UToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
            this.CreateM3UToolStripMenuItem.Text = "Create M3U From Selected Items";
            this.CreateM3UToolStripMenuItem.Click += new System.EventHandler(this.ToolStripMenuItemCreateM3UFile_Click);
            // 
            // toolStripMenuItemRemoteMenu
            // 
            this.toolStripMenuItemRemoteMenu.DropDown = this.contextMenuStripRemote;
            this.toolStripMenuItemRemoteMenu.Name = "toolStripMenuItemRemoteMenu";
            this.toolStripMenuItemRemoteMenu.Size = new System.Drawing.Size(60, 20);
            this.toolStripMenuItemRemoteMenu.Text = "Remote";
            // 
            // contextMenuStripRemote
            // 
            this.contextMenuStripRemote.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.contextMenuStripRemote.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripSeparator10,
            this.removeBatoceraHostKeyToolStripMenuItem,
            this.toolStripMenuItem3,
            this.toolStripSeparator11,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripSeparator18,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7,
            this.toolStripSeparator19,
            this.toolStripMenuItem8,
            this.toolStripMenuItem9});
            this.contextMenuStripRemote.Name = "contextMenuStripRemote";
            this.contextMenuStripRemote.OwnerItem = this.toolStripMenuItemRemoteMenu;
            this.contextMenuStripRemote.ShowImageMargin = false;
            this.contextMenuStripRemote.Size = new System.Drawing.Size(219, 248);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(218, 22);
            this.toolStripMenuItem1.Text = "Setup Batocera Host Credentials";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.SetupSSHToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(218, 22);
            this.toolStripMenuItem2.Text = "Map A Network Drive";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.ToolStripMenuItemMapDrive_ClickAsync);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            this.toolStripSeparator10.Size = new System.Drawing.Size(215, 6);
            // 
            // removeBatoceraHostKeyToolStripMenuItem
            // 
            this.removeBatoceraHostKeyToolStripMenuItem.Name = "removeBatoceraHostKeyToolStripMenuItem";
            this.removeBatoceraHostKeyToolStripMenuItem.Size = new System.Drawing.Size(218, 22);
            this.removeBatoceraHostKeyToolStripMenuItem.Text = "Remove Batocera Host Key";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(218, 22);
            this.toolStripMenuItem3.Text = "Open Terminal To Batocera Host";
            this.toolStripMenuItem3.Click += new System.EventHandler(this.ToolStripMenuItemConnect_Click);
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            this.toolStripSeparator11.Size = new System.Drawing.Size(215, 6);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(218, 22);
            this.toolStripMenuItem4.Text = "Get Batocera Version";
            this.toolStripMenuItem4.Click += new System.EventHandler(this.ToolStripMenuItemGetVersion_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(218, 22);
            this.toolStripMenuItem5.Text = "Show Available Updates";
            this.toolStripMenuItem5.Click += new System.EventHandler(this.ToolStripMenuItemShowUpdates_Click);
            // 
            // toolStripSeparator18
            // 
            this.toolStripSeparator18.Name = "toolStripSeparator18";
            this.toolStripSeparator18.Size = new System.Drawing.Size(215, 6);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(218, 22);
            this.toolStripMenuItem6.Text = "Stop Running Emulators";
            this.toolStripMenuItem6.Click += new System.EventHandler(this.ToolStripMenuItemStopEmulators_Click);
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(218, 22);
            this.toolStripMenuItem7.Text = "Stop Emulationstation";
            this.toolStripMenuItem7.Click += new System.EventHandler(this.StopEmulationstationToolStripMenuItem_Click);
            // 
            // toolStripSeparator19
            // 
            this.toolStripSeparator19.Name = "toolStripSeparator19";
            this.toolStripSeparator19.Size = new System.Drawing.Size(215, 6);
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(218, 22);
            this.toolStripMenuItem8.Text = "Reboot Batocera Host";
            this.toolStripMenuItem8.Click += new System.EventHandler(this.RebootBatoceraHostToolStripMenuItem_Click);
            // 
            // toolStripMenuItem9
            // 
            this.toolStripMenuItem9.Name = "toolStripMenuItem9";
            this.toolStripMenuItem9.Size = new System.Drawing.Size(218, 22);
            this.toolStripMenuItem9.Text = "Shutdown Batocera Host";
            this.toolStripMenuItem9.Click += new System.EventHandler(this.ToolStripMenuItemShutdownHost_Click);
            // 
            // toolStripMenuItemScraperMenu
            // 
            this.toolStripMenuItemScraperMenu.DropDown = this.contextMenuStripScraper;
            this.toolStripMenuItemScraperMenu.Name = "toolStripMenuItemScraperMenu";
            this.toolStripMenuItemScraperMenu.Size = new System.Drawing.Size(58, 20);
            this.toolStripMenuItemScraperMenu.Text = "Scraper";
            // 
            // contextMenuStripScraper
            // 
            this.contextMenuStripScraper.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.contextMenuStripScraper.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openScraperToolStripMenuItem});
            this.contextMenuStripScraper.Name = "contextMenuStripScraper";
            this.contextMenuStripScraper.ShowImageMargin = false;
            this.contextMenuStripScraper.Size = new System.Drawing.Size(121, 26);
            // 
            // openScraperToolStripMenuItem
            // 
            this.openScraperToolStripMenuItem.Name = "openScraperToolStripMenuItem";
            this.openScraperToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
            this.openScraperToolStripMenuItem.Text = "Open Scraper";
            this.openScraperToolStripMenuItem.Click += new System.EventHandler(this.OpenScraper_Click);
            // 
            // contextMenuStripImageOptions
            // 
            this.contextMenuStripImageOptions.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.contextMenuStripImageOptions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemCopyToClipboard,
            this.toolStripMenuItemEditImage});
            this.contextMenuStripImageOptions.Name = "contextMenuStrip1";
            this.contextMenuStripImageOptions.ShowImageMargin = false;
            this.contextMenuStripImageOptions.Size = new System.Drawing.Size(175, 48);
            // 
            // toolStripMenuItemCopyToClipboard
            // 
            this.toolStripMenuItemCopyToClipboard.Name = "toolStripMenuItemCopyToClipboard";
            this.toolStripMenuItemCopyToClipboard.Size = new System.Drawing.Size(174, 22);
            this.toolStripMenuItemCopyToClipboard.Text = "Copy Path To Clipboard";
            this.toolStripMenuItemCopyToClipboard.Click += new System.EventHandler(this.ToolStripMenuItem2_Click);
            // 
            // toolStripMenuItemEditImage
            // 
            this.toolStripMenuItemEditImage.Name = "toolStripMenuItemEditImage";
            this.toolStripMenuItemEditImage.Size = new System.Drawing.Size(174, 22);
            this.toolStripMenuItemEditImage.Text = "View / Edit Item";
            this.toolStripMenuItemEditImage.Click += new System.EventHandler(this.EditToolStripMenuItem1_Click);
            // 
            // statusBar
            // 
            this.statusBar.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusBar.Location = new System.Drawing.Point(0, 341);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(704, 20);
            this.statusBar.TabIndex = 6;
            this.statusBar.Text = "Ready";
            // 
            // GamelistManagerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(704, 361);
            this.Controls.Add(this.splitContainerBig);
            this.Controls.Add(this.menuStripMainMenu);
            this.Controls.Add(this.statusBar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStripMainMenu;
            this.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            this.MinimumSize = new System.Drawing.Size(720, 400);
            this.Name = "GamelistManagerForm";
            this.Text = "Gamelist Manager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GamelistManagerForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GamelistManagerForm_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.splitContainerBig.Panel1.ResumeLayout(false);
            this.splitContainerBig.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerBig)).EndInit();
            this.splitContainerBig.ResumeLayout(false);
            this.panelBelowDataGridView.ResumeLayout(false);
            this.panelBelowDataGridView.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSystemLogo)).EndInit();
            this.splitContainerSmall.Panel1.ResumeLayout(false);
            this.splitContainerSmall.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerSmall)).EndInit();
            this.splitContainerSmall.ResumeLayout(false);
            this.menuStripMainMenu.ResumeLayout(false);
            this.menuStripMainMenu.PerformLayout();
            this.contextMenuStripFile.ResumeLayout(false);
            this.contextMenuStripView.ResumeLayout(false);
            this.contextMenuStripEdit.ResumeLayout(false);
            this.contextMenuStripColumns.ResumeLayout(false);
            this.contextMenuStripTools.ResumeLayout(false);
            this.contextMenuStripRemote.ResumeLayout(false);
            this.contextMenuStripScraper.ResumeLayout(false);
            this.contextMenuStripImageOptions.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.SplitContainer splitContainerBig;
        private System.Windows.Forms.MenuStrip menuStripMainMenu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemFileMenu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemViewMenu;
        private System.Windows.Forms.RichTextBox richTextBoxDescription;
        private System.Windows.Forms.Label labelHiddenCount;
        private System.Windows.Forms.Label labelVisibleCount;
        private System.Windows.Forms.Label labelHidden;
        private System.Windows.Forms.Label labelShowing;
        private System.Windows.Forms.Label labelShowingCount;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemColumnsMenu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemToolsMenu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemEditMenu;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripImageOptions;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemCopyToClipboard;
        private System.Windows.Forms.Panel panelMediaBackground;
        private System.Windows.Forms.ComboBox comboBoxGenre;
        private System.Windows.Forms.Label labelGenrePicker;
        private System.Windows.Forms.Panel panelBelowDataGridView;
        private System.Windows.Forms.Label labelVisible;
        private System.Windows.Forms.Label labelFavoriteCount;
        private System.Windows.Forms.Label labelFavorite;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemEditImage;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemRemoteMenu;
        private System.Windows.Forms.SplitContainer splitContainerSmall;
        private System.Windows.Forms.StatusBar statusBar;
        private System.Windows.Forms.PictureBox pictureBoxSystemLogo;
        private System.Windows.Forms.CheckBox checkBoxCustomFilter;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemScraperMenu;
        private System.Windows.Forms.TextBox textBoxCustomFilter;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripFile;
        private System.Windows.Forms.ToolStripMenuItem loadGamelistToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reloadGamelistToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveGamelistToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator25;
        private System.Windows.Forms.ToolStripMenuItem exportToCSVToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator26;
        private System.Windows.Forms.ToolStripMenuItem clearRecentFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripView;
        private System.Windows.Forms.ToolStripMenuItem showAllHiddenAndVisibleItemsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showVisibleItemsOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showHiddenItemsOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
        private System.Windows.Forms.ToolStripMenuItem showAllGenresToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showGenreOnlyToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator21;
        private System.Windows.Forms.ToolStripMenuItem showMediaToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator27;
        private System.Windows.Forms.ToolStripMenuItem resetViewToolStripMenuItem1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripEdit;
        private System.Windows.Forms.ToolStripMenuItem setAllItemsVisibleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setAllItemsHiddenToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem setAllGenreVisibleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setAllGenreHiddenToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem editRowDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator24;
        private System.Windows.Forms.ToolStripMenuItem deleteRowToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator28;
        private System.Windows.Forms.ToolStripMenuItem clearScraperDateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateScraperDateToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator29;
        private System.Windows.Forms.ToolStripMenuItem resetNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearAllDataToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator30;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripColumns;
        public System.Windows.Forms.ToolStripMenuItem DeveloperToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FavoriteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem GameTimeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem GenreToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem LangToolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem LastPlayedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem PublisherToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem PlayCountToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem PlayersToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem RatingToolStripMenuItem10;
        private System.Windows.Forms.ToolStripMenuItem RegionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ReleaseDateToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem DescriptionToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem14;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem MediaPathsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem ScraperDatesToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripTools;
        private System.Windows.Forms.ToolStripMenuItem MameToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem MediaCheckToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem FindNewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem FindMissingToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripMenuItem CreateM3UToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripScraper;
        private System.Windows.Forms.ToolStripMenuItem openScraperToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripRemote;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator18;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator19;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem9;
        private System.Windows.Forms.ComboBox comboBoxFilterItem;
        private System.Windows.Forms.ToolStripMenuItem removeBatoceraHostKeyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemAlwaysOnTop;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator14;
        private System.Windows.Forms.ToolStripMenuItem dataGridColoringToolStripMenuItem;
        private System.Windows.Forms.ToolStripComboBox toolStripColorComboBox;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator15;
    }
}

