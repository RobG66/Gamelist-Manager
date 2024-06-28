namespace GamelistManager
{
    partial class ScraperForm
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
            this.radioButtonScrapeSelected = new System.Windows.Forms.RadioButton();
            this.panelEverything = new System.Windows.Forms.Panel();
            this.panelScraperOptions = new System.Windows.Forms.Panel();
            this.checkBoxSupressNotify = new System.Windows.Forms.CheckBox();
            this.labelScraper = new System.Windows.Forms.Label();
            this.radioButtonScrapeAll = new System.Windows.Forms.RadioButton();
            this.checkBoxSave = new System.Windows.Forms.CheckBox();
            this.checkBoxOverwriteExisting = new System.Windows.Forms.CheckBox();
            this.checkBoxDoNotScrapeHidden = new System.Windows.Forms.CheckBox();
            this.comboBoxScrapers = new System.Windows.Forms.ComboBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.labelScrapeLimitCounters = new System.Windows.Forms.Label();
            this.labelScrape = new System.Windows.Forms.Label();
            this.labelCounts = new System.Windows.Forms.Label();
            this.progressBarScrapeProgress = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.contextMenuStripLog = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ToolStripMenuItemCopyLogToClipboard = new System.Windows.Forms.ToolStripMenuItem();
            this.labelProgress = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBoxMainLogo = new System.Windows.Forms.PictureBox();
            this.buttonSetup = new System.Windows.Forms.Button();
            this.buttonStart = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.panelSmall = new System.Windows.Forms.Panel();
            this.panelCheckboxes = new System.Windows.Forms.Panel();
            this.labelMedia = new System.Windows.Forms.Label();
            this.checkboxArcadeSystemName = new System.Windows.Forms.CheckBox();
            this.checkboxBoxBack = new System.Windows.Forms.CheckBox();
            this.checkboxFanArt = new System.Windows.Forms.CheckBox();
            this.checkboxGenreID = new System.Windows.Forms.CheckBox();
            this.checkboxBezel = new System.Windows.Forms.CheckBox();
            this.labelMetadata = new System.Windows.Forms.Label();
            this.buttonSelectAll = new System.Windows.Forms.Button();
            this.checkboxManual = new System.Windows.Forms.CheckBox();
            this.checkboxName = new System.Windows.Forms.CheckBox();
            this.checkboxMap = new System.Windows.Forms.CheckBox();
            this.checkboxPublisher = new System.Windows.Forms.CheckBox();
            this.checkboxVideo = new System.Windows.Forms.CheckBox();
            this.checkboxRegion = new System.Windows.Forms.CheckBox();
            this.checkboxReleasedate = new System.Windows.Forms.CheckBox();
            this.checkboxThumbnail = new System.Windows.Forms.CheckBox();
            this.checkboxPlayers = new System.Windows.Forms.CheckBox();
            this.checkboxDesc = new System.Windows.Forms.CheckBox();
            this.checkboxImage = new System.Windows.Forms.CheckBox();
            this.checkboxDeveloper = new System.Windows.Forms.CheckBox();
            this.checkboxRating = new System.Windows.Forms.CheckBox();
            this.checkboxGenre = new System.Windows.Forms.CheckBox();
            this.buttonSelectNone = new System.Windows.Forms.Button();
            this.checkboxMarquee = new System.Windows.Forms.CheckBox();
            this.checkboxLang = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.listBoxDownloads = new System.Windows.Forms.ListBox();
            this.panelEverything.SuspendLayout();
            this.panelScraperOptions.SuspendLayout();
            this.panel1.SuspendLayout();
            this.contextMenuStripLog.SuspendLayout();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMainLogo)).BeginInit();
            this.panelSmall.SuspendLayout();
            this.panelCheckboxes.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // radioButtonScrapeSelected
            // 
            this.radioButtonScrapeSelected.AutoSize = true;
            this.radioButtonScrapeSelected.Checked = true;
            this.radioButtonScrapeSelected.Location = new System.Drawing.Point(2, 26);
            this.radioButtonScrapeSelected.Margin = new System.Windows.Forms.Padding(1);
            this.radioButtonScrapeSelected.Name = "radioButtonScrapeSelected";
            this.radioButtonScrapeSelected.Size = new System.Drawing.Size(156, 17);
            this.radioButtonScrapeSelected.TabIndex = 0;
            this.radioButtonScrapeSelected.TabStop = true;
            this.radioButtonScrapeSelected.Text = "Scrape Selected Items Only";
            this.radioButtonScrapeSelected.UseVisualStyleBackColor = true;
            // 
            // panelEverything
            // 
            this.panelEverything.BackColor = System.Drawing.Color.Transparent;
            this.panelEverything.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.panelEverything.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelEverything.Controls.Add(this.panelScraperOptions);
            this.panelEverything.Controls.Add(this.panel1);
            this.panelEverything.Controls.Add(this.panelMain);
            this.panelEverything.Controls.Add(this.panelSmall);
            this.panelEverything.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelEverything.Location = new System.Drawing.Point(0, 0);
            this.panelEverything.Margin = new System.Windows.Forms.Padding(1);
            this.panelEverything.Name = "panelEverything";
            this.panelEverything.Size = new System.Drawing.Size(504, 454);
            this.panelEverything.TabIndex = 2;
            // 
            // panelScraperOptions
            // 
            this.panelScraperOptions.Controls.Add(this.checkBoxSupressNotify);
            this.panelScraperOptions.Controls.Add(this.labelScraper);
            this.panelScraperOptions.Controls.Add(this.radioButtonScrapeAll);
            this.panelScraperOptions.Controls.Add(this.checkBoxSave);
            this.panelScraperOptions.Controls.Add(this.checkBoxOverwriteExisting);
            this.panelScraperOptions.Controls.Add(this.checkBoxDoNotScrapeHidden);
            this.panelScraperOptions.Controls.Add(this.comboBoxScrapers);
            this.panelScraperOptions.Controls.Add(this.radioButtonScrapeSelected);
            this.panelScraperOptions.Location = new System.Drawing.Point(6, 67);
            this.panelScraperOptions.Name = "panelScraperOptions";
            this.panelScraperOptions.Size = new System.Drawing.Size(195, 145);
            this.panelScraperOptions.TabIndex = 29;
            // 
            // checkBoxSupressNotify
            // 
            this.checkBoxSupressNotify.AutoSize = true;
            this.checkBoxSupressNotify.Location = new System.Drawing.Point(13, 124);
            this.checkBoxSupressNotify.Name = "checkBoxSupressNotify";
            this.checkBoxSupressNotify.Size = new System.Drawing.Size(161, 17);
            this.checkBoxSupressNotify.TabIndex = 28;
            this.checkBoxSupressNotify.Text = "Suppress Finish Notifications";
            this.checkBoxSupressNotify.UseVisualStyleBackColor = true;
            // 
            // labelScraper
            // 
            this.labelScraper.AutoSize = true;
            this.labelScraper.Location = new System.Drawing.Point(5, 6);
            this.labelScraper.Name = "labelScraper";
            this.labelScraper.Size = new System.Drawing.Size(47, 13);
            this.labelScraper.TabIndex = 26;
            this.labelScraper.Text = "Scraper:";
            // 
            // radioButtonScrapeAll
            // 
            this.radioButtonScrapeAll.AutoSize = true;
            this.radioButtonScrapeAll.Location = new System.Drawing.Point(2, 45);
            this.radioButtonScrapeAll.Name = "radioButtonScrapeAll";
            this.radioButtonScrapeAll.Size = new System.Drawing.Size(101, 17);
            this.radioButtonScrapeAll.TabIndex = 22;
            this.radioButtonScrapeAll.TabStop = true;
            this.radioButtonScrapeAll.Text = "Scrape All Items";
            this.radioButtonScrapeAll.UseVisualStyleBackColor = true;
            // 
            // checkBoxSave
            // 
            this.checkBoxSave.AutoSize = true;
            this.checkBoxSave.Checked = true;
            this.checkBoxSave.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSave.Location = new System.Drawing.Point(13, 86);
            this.checkBoxSave.Name = "checkBoxSave";
            this.checkBoxSave.Size = new System.Drawing.Size(175, 17);
            this.checkBoxSave.TabIndex = 20;
            this.checkBoxSave.Text = "Prompt for save when complete";
            this.checkBoxSave.UseVisualStyleBackColor = true;
            // 
            // checkBoxOverwriteExisting
            // 
            this.checkBoxOverwriteExisting.AutoSize = true;
            this.checkBoxOverwriteExisting.Checked = true;
            this.checkBoxOverwriteExisting.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxOverwriteExisting.Location = new System.Drawing.Point(13, 67);
            this.checkBoxOverwriteExisting.Name = "checkBoxOverwriteExisting";
            this.checkBoxOverwriteExisting.Size = new System.Drawing.Size(172, 17);
            this.checkBoxOverwriteExisting.TabIndex = 19;
            this.checkBoxOverwriteExisting.Text = "Overwrite exising data and files";
            this.checkBoxOverwriteExisting.UseVisualStyleBackColor = true;
            // 
            // checkBoxDoNotScrapeHidden
            // 
            this.checkBoxDoNotScrapeHidden.AutoSize = true;
            this.checkBoxDoNotScrapeHidden.Checked = true;
            this.checkBoxDoNotScrapeHidden.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxDoNotScrapeHidden.Location = new System.Drawing.Point(13, 105);
            this.checkBoxDoNotScrapeHidden.Name = "checkBoxDoNotScrapeHidden";
            this.checkBoxDoNotScrapeHidden.Size = new System.Drawing.Size(155, 17);
            this.checkBoxDoNotScrapeHidden.TabIndex = 27;
            this.checkBoxDoNotScrapeHidden.Text = "Do not scrape hidden items";
            this.checkBoxDoNotScrapeHidden.UseVisualStyleBackColor = true;
            // 
            // comboBoxScrapers
            // 
            this.comboBoxScrapers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxScrapers.FormattingEnabled = true;
            this.comboBoxScrapers.Items.AddRange(new object[] {
            "ArcadeDB",
            "EmuMovies",
            "ScreenScraper"});
            this.comboBoxScrapers.Location = new System.Drawing.Point(53, 3);
            this.comboBoxScrapers.Margin = new System.Windows.Forms.Padding(1);
            this.comboBoxScrapers.Name = "comboBoxScrapers";
            this.comboBoxScrapers.Size = new System.Drawing.Size(104, 21);
            this.comboBoxScrapers.Sorted = true;
            this.comboBoxScrapers.TabIndex = 2;
            this.comboBoxScrapers.SelectedIndexChanged += new System.EventHandler(this.ComboBox_SelectScraper_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.tableLayoutPanel1);
            this.panel1.Controls.Add(this.labelScrapeLimitCounters);
            this.panel1.Controls.Add(this.labelScrape);
            this.panel1.Controls.Add(this.labelCounts);
            this.panel1.Controls.Add(this.progressBarScrapeProgress);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.labelProgress);
            this.panel1.Location = new System.Drawing.Point(3, 271);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(494, 178);
            this.panel1.TabIndex = 30;
            // 
            // labelScrapeLimitCounters
            // 
            this.labelScrapeLimitCounters.AutoSize = true;
            this.labelScrapeLimitCounters.Location = new System.Drawing.Point(70, 26);
            this.labelScrapeLimitCounters.Name = "labelScrapeLimitCounters";
            this.labelScrapeLimitCounters.Size = new System.Drawing.Size(27, 13);
            this.labelScrapeLimitCounters.TabIndex = 28;
            this.labelScrapeLimitCounters.Text = "N/A";
            // 
            // labelScrape
            // 
            this.labelScrape.AutoSize = true;
            this.labelScrape.Location = new System.Drawing.Point(5, 26);
            this.labelScrape.Name = "labelScrape";
            this.labelScrape.Size = new System.Drawing.Size(68, 13);
            this.labelScrape.TabIndex = 27;
            this.labelScrape.Text = "Scrape Limit:";
            // 
            // labelCounts
            // 
            this.labelCounts.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCounts.Location = new System.Drawing.Point(278, 26);
            this.labelCounts.Name = "labelCounts";
            this.labelCounts.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.labelCounts.Size = new System.Drawing.Size(209, 14);
            this.labelCounts.TabIndex = 26;
            this.labelCounts.Text = "0/0";
            // 
            // progressBarScrapeProgress
            // 
            this.progressBarScrapeProgress.Location = new System.Drawing.Point(59, 7);
            this.progressBarScrapeProgress.Margin = new System.Windows.Forms.Padding(1);
            this.progressBarScrapeProgress.Name = "progressBarScrapeProgress";
            this.progressBarScrapeProgress.Size = new System.Drawing.Size(89, 14);
            this.progressBarScrapeProgress.TabIndex = 12;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "Progress:";
            // 
            // listBoxLog
            // 
            this.listBoxLog.ContextMenuStrip = this.contextMenuStripLog;
            this.listBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.Location = new System.Drawing.Point(3, 3);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.Size = new System.Drawing.Size(240, 127);
            this.listBoxLog.TabIndex = 21;
            // 
            // contextMenuStripLog
            // 
            this.contextMenuStripLog.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItemCopyLogToClipboard});
            this.contextMenuStripLog.Name = "contextMenuStripLog";
            this.contextMenuStripLog.Size = new System.Drawing.Size(196, 26);
            // 
            // ToolStripMenuItemCopyLogToClipboard
            // 
            this.ToolStripMenuItemCopyLogToClipboard.Name = "ToolStripMenuItemCopyLogToClipboard";
            this.ToolStripMenuItemCopyLogToClipboard.Size = new System.Drawing.Size(195, 22);
            this.ToolStripMenuItemCopyLogToClipboard.Text = "Copy Log To Clipboard";
            this.ToolStripMenuItemCopyLogToClipboard.Click += new System.EventHandler(this.ToolStripMenuItemCopyLogToClipboard_Click);
            // 
            // labelProgress
            // 
            this.labelProgress.AutoSize = true;
            this.labelProgress.Location = new System.Drawing.Point(152, 7);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(21, 13);
            this.labelProgress.TabIndex = 25;
            this.labelProgress.Text = "0%";
            // 
            // panelMain
            // 
            this.panelMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelMain.Controls.Add(this.pictureBox1);
            this.panelMain.Controls.Add(this.pictureBoxMainLogo);
            this.panelMain.Controls.Add(this.buttonSetup);
            this.panelMain.Controls.Add(this.buttonStart);
            this.panelMain.Controls.Add(this.buttonCancel);
            this.panelMain.Location = new System.Drawing.Point(4, 5);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(260, 260);
            this.panelMain.TabIndex = 29;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pictureBox1.Location = new System.Drawing.Point(0, 208);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(258, 50);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 29;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBoxMainLogo
            // 
            this.pictureBoxMainLogo.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBoxMainLogo.Image = global::GamelistManager.Properties.Resources.scraperlogo;
            this.pictureBoxMainLogo.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxMainLogo.Name = "pictureBoxMainLogo";
            this.pictureBoxMainLogo.Size = new System.Drawing.Size(258, 59);
            this.pictureBoxMainLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxMainLogo.TabIndex = 14;
            this.pictureBoxMainLogo.TabStop = false;
            // 
            // buttonSetup
            // 
            this.buttonSetup.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.buttonSetup.Location = new System.Drawing.Point(202, 118);
            this.buttonSetup.Name = "buttonSetup";
            this.buttonSetup.Size = new System.Drawing.Size(52, 23);
            this.buttonSetup.TabIndex = 28;
            this.buttonSetup.Text = "Setup";
            this.buttonSetup.UseVisualStyleBackColor = false;
            this.buttonSetup.Click += new System.EventHandler(this.ButtonSetup_Click);
            // 
            // buttonStart
            // 
            this.buttonStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.buttonStart.Location = new System.Drawing.Point(202, 67);
            this.buttonStart.Margin = new System.Windows.Forms.Padding(1);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(52, 24);
            this.buttonStart.TabIndex = 11;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = false;
            this.buttonStart.Click += new System.EventHandler(this.ButtonStartStop_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.buttonCancel.Enabled = false;
            this.buttonCancel.Location = new System.Drawing.Point(202, 93);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(52, 23);
            this.buttonCancel.TabIndex = 23;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = false;
            this.buttonCancel.Click += new System.EventHandler(this.Button_Stop_Click);
            // 
            // panelSmall
            // 
            this.panelSmall.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelSmall.Controls.Add(this.panelCheckboxes);
            this.panelSmall.Location = new System.Drawing.Point(269, 5);
            this.panelSmall.Margin = new System.Windows.Forms.Padding(1);
            this.panelSmall.Name = "panelSmall";
            this.panelSmall.Size = new System.Drawing.Size(228, 260);
            this.panelSmall.TabIndex = 10;
            // 
            // panelCheckboxes
            // 
            this.panelCheckboxes.Controls.Add(this.labelMedia);
            this.panelCheckboxes.Controls.Add(this.checkboxArcadeSystemName);
            this.panelCheckboxes.Controls.Add(this.checkboxBoxBack);
            this.panelCheckboxes.Controls.Add(this.checkboxFanArt);
            this.panelCheckboxes.Controls.Add(this.checkboxGenreID);
            this.panelCheckboxes.Controls.Add(this.checkboxBezel);
            this.panelCheckboxes.Controls.Add(this.labelMetadata);
            this.panelCheckboxes.Controls.Add(this.buttonSelectAll);
            this.panelCheckboxes.Controls.Add(this.checkboxManual);
            this.panelCheckboxes.Controls.Add(this.checkboxName);
            this.panelCheckboxes.Controls.Add(this.checkboxMap);
            this.panelCheckboxes.Controls.Add(this.checkboxPublisher);
            this.panelCheckboxes.Controls.Add(this.checkboxVideo);
            this.panelCheckboxes.Controls.Add(this.checkboxRegion);
            this.panelCheckboxes.Controls.Add(this.checkboxReleasedate);
            this.panelCheckboxes.Controls.Add(this.checkboxThumbnail);
            this.panelCheckboxes.Controls.Add(this.checkboxPlayers);
            this.panelCheckboxes.Controls.Add(this.checkboxDesc);
            this.panelCheckboxes.Controls.Add(this.checkboxImage);
            this.panelCheckboxes.Controls.Add(this.checkboxDeveloper);
            this.panelCheckboxes.Controls.Add(this.checkboxRating);
            this.panelCheckboxes.Controls.Add(this.checkboxGenre);
            this.panelCheckboxes.Controls.Add(this.buttonSelectNone);
            this.panelCheckboxes.Controls.Add(this.checkboxMarquee);
            this.panelCheckboxes.Controls.Add(this.checkboxLang);
            this.panelCheckboxes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelCheckboxes.Location = new System.Drawing.Point(0, 0);
            this.panelCheckboxes.Name = "panelCheckboxes";
            this.panelCheckboxes.Size = new System.Drawing.Size(226, 258);
            this.panelCheckboxes.TabIndex = 29;
            // 
            // labelMedia
            // 
            this.labelMedia.AutoSize = true;
            this.labelMedia.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMedia.ForeColor = System.Drawing.Color.Black;
            this.labelMedia.Location = new System.Drawing.Point(110, 5);
            this.labelMedia.Name = "labelMedia";
            this.labelMedia.Size = new System.Drawing.Size(45, 17);
            this.labelMedia.TabIndex = 29;
            this.labelMedia.Text = "Media";
            // 
            // checkboxArcadeSystemName
            // 
            this.checkboxArcadeSystemName.AutoSize = true;
            this.checkboxArcadeSystemName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxArcadeSystemName.Location = new System.Drawing.Point(12, 236);
            this.checkboxArcadeSystemName.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxArcadeSystemName.Name = "checkboxArcadeSystemName";
            this.checkboxArcadeSystemName.Size = new System.Drawing.Size(128, 17);
            this.checkboxArcadeSystemName.TabIndex = 28;
            this.checkboxArcadeSystemName.Text = "Arcade System Name";
            this.checkboxArcadeSystemName.UseVisualStyleBackColor = true;
            // 
            // checkboxBoxBack
            // 
            this.checkboxBoxBack.AutoSize = true;
            this.checkboxBoxBack.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxBoxBack.Location = new System.Drawing.Point(113, 179);
            this.checkboxBoxBack.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxBoxBack.Name = "checkboxBoxBack";
            this.checkboxBoxBack.Size = new System.Drawing.Size(72, 17);
            this.checkboxBoxBack.TabIndex = 27;
            this.checkboxBoxBack.Text = "Box Back";
            this.checkboxBoxBack.UseVisualStyleBackColor = true;
            // 
            // checkboxFanArt
            // 
            this.checkboxFanArt.AutoSize = true;
            this.checkboxFanArt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxFanArt.Location = new System.Drawing.Point(113, 160);
            this.checkboxFanArt.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxFanArt.Name = "checkboxFanArt";
            this.checkboxFanArt.Size = new System.Drawing.Size(60, 17);
            this.checkboxFanArt.TabIndex = 26;
            this.checkboxFanArt.Text = "Fan Art";
            this.checkboxFanArt.UseVisualStyleBackColor = true;
            // 
            // checkboxGenreID
            // 
            this.checkboxGenreID.AutoSize = true;
            this.checkboxGenreID.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxGenreID.Location = new System.Drawing.Point(12, 217);
            this.checkboxGenreID.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxGenreID.Name = "checkboxGenreID";
            this.checkboxGenreID.Size = new System.Drawing.Size(69, 17);
            this.checkboxGenreID.TabIndex = 25;
            this.checkboxGenreID.Text = "Genre ID";
            this.checkboxGenreID.UseVisualStyleBackColor = true;
            // 
            // checkboxBezel
            // 
            this.checkboxBezel.AutoSize = true;
            this.checkboxBezel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxBezel.Location = new System.Drawing.Point(113, 141);
            this.checkboxBezel.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxBezel.Name = "checkboxBezel";
            this.checkboxBezel.Size = new System.Drawing.Size(52, 17);
            this.checkboxBezel.TabIndex = 24;
            this.checkboxBezel.Text = "Bezel";
            this.checkboxBezel.UseVisualStyleBackColor = true;
            // 
            // labelMetadata
            // 
            this.labelMetadata.AutoSize = true;
            this.labelMetadata.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMetadata.ForeColor = System.Drawing.Color.Black;
            this.labelMetadata.Location = new System.Drawing.Point(11, 5);
            this.labelMetadata.Name = "labelMetadata";
            this.labelMetadata.Size = new System.Drawing.Size(66, 17);
            this.labelMetadata.TabIndex = 23;
            this.labelMetadata.Text = "Metadata";
            // 
            // buttonSelectAll
            // 
            this.buttonSelectAll.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.buttonSelectAll.Location = new System.Drawing.Point(142, 199);
            this.buttonSelectAll.Margin = new System.Windows.Forms.Padding(1);
            this.buttonSelectAll.Name = "buttonSelectAll";
            this.buttonSelectAll.Size = new System.Drawing.Size(76, 23);
            this.buttonSelectAll.TabIndex = 20;
            this.buttonSelectAll.Text = "Select All";
            this.buttonSelectAll.UseVisualStyleBackColor = false;
            this.buttonSelectAll.Click += new System.EventHandler(this.ButtonSelectAll_Click);
            // 
            // checkboxManual
            // 
            this.checkboxManual.AutoSize = true;
            this.checkboxManual.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxManual.Location = new System.Drawing.Point(113, 122);
            this.checkboxManual.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxManual.Name = "checkboxManual";
            this.checkboxManual.Size = new System.Drawing.Size(61, 17);
            this.checkboxManual.TabIndex = 18;
            this.checkboxManual.Text = "Manual";
            this.checkboxManual.UseVisualStyleBackColor = true;
            // 
            // checkboxName
            // 
            this.checkboxName.AutoSize = true;
            this.checkboxName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxName.Location = new System.Drawing.Point(12, 27);
            this.checkboxName.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxName.Name = "checkboxName";
            this.checkboxName.Size = new System.Drawing.Size(54, 17);
            this.checkboxName.TabIndex = 2;
            this.checkboxName.Text = "Name";
            this.checkboxName.UseVisualStyleBackColor = true;
            // 
            // checkboxMap
            // 
            this.checkboxMap.AutoSize = true;
            this.checkboxMap.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxMap.Location = new System.Drawing.Point(113, 103);
            this.checkboxMap.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxMap.Name = "checkboxMap";
            this.checkboxMap.Size = new System.Drawing.Size(47, 17);
            this.checkboxMap.TabIndex = 16;
            this.checkboxMap.Text = "Map";
            this.checkboxMap.UseVisualStyleBackColor = true;
            // 
            // checkboxPublisher
            // 
            this.checkboxPublisher.AutoSize = true;
            this.checkboxPublisher.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxPublisher.Location = new System.Drawing.Point(12, 198);
            this.checkboxPublisher.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxPublisher.Name = "checkboxPublisher";
            this.checkboxPublisher.Size = new System.Drawing.Size(69, 17);
            this.checkboxPublisher.TabIndex = 11;
            this.checkboxPublisher.Text = "Publisher";
            this.checkboxPublisher.UseVisualStyleBackColor = true;
            // 
            // checkboxVideo
            // 
            this.checkboxVideo.AutoSize = true;
            this.checkboxVideo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxVideo.Location = new System.Drawing.Point(113, 84);
            this.checkboxVideo.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxVideo.Name = "checkboxVideo";
            this.checkboxVideo.Size = new System.Drawing.Size(53, 17);
            this.checkboxVideo.TabIndex = 15;
            this.checkboxVideo.Text = "Video";
            this.checkboxVideo.UseVisualStyleBackColor = true;
            // 
            // checkboxRegion
            // 
            this.checkboxRegion.AutoSize = true;
            this.checkboxRegion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxRegion.Location = new System.Drawing.Point(12, 122);
            this.checkboxRegion.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxRegion.Name = "checkboxRegion";
            this.checkboxRegion.Size = new System.Drawing.Size(60, 17);
            this.checkboxRegion.TabIndex = 7;
            this.checkboxRegion.Text = "Region";
            this.checkboxRegion.UseVisualStyleBackColor = true;
            // 
            // checkboxReleasedate
            // 
            this.checkboxReleasedate.AutoSize = true;
            this.checkboxReleasedate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxReleasedate.Location = new System.Drawing.Point(12, 160);
            this.checkboxReleasedate.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxReleasedate.Name = "checkboxReleasedate";
            this.checkboxReleasedate.Size = new System.Drawing.Size(71, 17);
            this.checkboxReleasedate.TabIndex = 9;
            this.checkboxReleasedate.Text = "Released";
            this.checkboxReleasedate.UseVisualStyleBackColor = true;
            // 
            // checkboxThumbnail
            // 
            this.checkboxThumbnail.AutoSize = true;
            this.checkboxThumbnail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxThumbnail.Location = new System.Drawing.Point(113, 65);
            this.checkboxThumbnail.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxThumbnail.Name = "checkboxThumbnail";
            this.checkboxThumbnail.Size = new System.Drawing.Size(75, 17);
            this.checkboxThumbnail.TabIndex = 14;
            this.checkboxThumbnail.Text = "Thumbnail";
            this.checkboxThumbnail.UseVisualStyleBackColor = true;
            // 
            // checkboxPlayers
            // 
            this.checkboxPlayers.AutoSize = true;
            this.checkboxPlayers.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxPlayers.Location = new System.Drawing.Point(12, 84);
            this.checkboxPlayers.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxPlayers.Name = "checkboxPlayers";
            this.checkboxPlayers.Size = new System.Drawing.Size(60, 17);
            this.checkboxPlayers.TabIndex = 5;
            this.checkboxPlayers.Text = "Players";
            this.checkboxPlayers.UseVisualStyleBackColor = true;
            // 
            // checkboxDesc
            // 
            this.checkboxDesc.AutoSize = true;
            this.checkboxDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxDesc.Location = new System.Drawing.Point(12, 46);
            this.checkboxDesc.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxDesc.Name = "checkboxDesc";
            this.checkboxDesc.Size = new System.Drawing.Size(79, 17);
            this.checkboxDesc.TabIndex = 3;
            this.checkboxDesc.Text = "Description";
            this.checkboxDesc.UseVisualStyleBackColor = true;
            // 
            // checkboxImage
            // 
            this.checkboxImage.AutoSize = true;
            this.checkboxImage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxImage.Location = new System.Drawing.Point(113, 27);
            this.checkboxImage.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxImage.Name = "checkboxImage";
            this.checkboxImage.Size = new System.Drawing.Size(55, 17);
            this.checkboxImage.TabIndex = 12;
            this.checkboxImage.Text = "Image";
            this.checkboxImage.UseVisualStyleBackColor = true;
            // 
            // checkboxDeveloper
            // 
            this.checkboxDeveloper.AutoSize = true;
            this.checkboxDeveloper.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxDeveloper.Location = new System.Drawing.Point(12, 179);
            this.checkboxDeveloper.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxDeveloper.Name = "checkboxDeveloper";
            this.checkboxDeveloper.Size = new System.Drawing.Size(75, 17);
            this.checkboxDeveloper.TabIndex = 10;
            this.checkboxDeveloper.Text = "Developer";
            this.checkboxDeveloper.UseVisualStyleBackColor = true;
            // 
            // checkboxRating
            // 
            this.checkboxRating.AutoSize = true;
            this.checkboxRating.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxRating.Location = new System.Drawing.Point(12, 103);
            this.checkboxRating.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxRating.Name = "checkboxRating";
            this.checkboxRating.Size = new System.Drawing.Size(57, 17);
            this.checkboxRating.TabIndex = 6;
            this.checkboxRating.Text = "Rating";
            this.checkboxRating.UseVisualStyleBackColor = true;
            // 
            // checkboxGenre
            // 
            this.checkboxGenre.AutoSize = true;
            this.checkboxGenre.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxGenre.Location = new System.Drawing.Point(12, 65);
            this.checkboxGenre.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxGenre.Name = "checkboxGenre";
            this.checkboxGenre.Size = new System.Drawing.Size(55, 17);
            this.checkboxGenre.TabIndex = 4;
            this.checkboxGenre.Text = "Genre";
            this.checkboxGenre.UseVisualStyleBackColor = true;
            // 
            // buttonSelectNone
            // 
            this.buttonSelectNone.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.buttonSelectNone.Location = new System.Drawing.Point(142, 225);
            this.buttonSelectNone.Margin = new System.Windows.Forms.Padding(1);
            this.buttonSelectNone.Name = "buttonSelectNone";
            this.buttonSelectNone.Size = new System.Drawing.Size(76, 23);
            this.buttonSelectNone.TabIndex = 21;
            this.buttonSelectNone.Text = "Select None";
            this.buttonSelectNone.UseVisualStyleBackColor = false;
            this.buttonSelectNone.Click += new System.EventHandler(this.ButtonSelectNone_Click);
            // 
            // checkboxMarquee
            // 
            this.checkboxMarquee.AutoSize = true;
            this.checkboxMarquee.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxMarquee.Location = new System.Drawing.Point(113, 46);
            this.checkboxMarquee.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxMarquee.Name = "checkboxMarquee";
            this.checkboxMarquee.Size = new System.Drawing.Size(68, 17);
            this.checkboxMarquee.TabIndex = 13;
            this.checkboxMarquee.Text = "Marquee";
            this.checkboxMarquee.UseVisualStyleBackColor = true;
            // 
            // checkboxLang
            // 
            this.checkboxLang.AutoSize = true;
            this.checkboxLang.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkboxLang.Location = new System.Drawing.Point(12, 141);
            this.checkboxLang.Margin = new System.Windows.Forms.Padding(1);
            this.checkboxLang.Name = "checkboxLang";
            this.checkboxLang.Size = new System.Drawing.Size(74, 17);
            this.checkboxLang.TabIndex = 8;
            this.checkboxLang.Text = "Language";
            this.checkboxLang.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.listBoxLog, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.listBoxDownloads, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 43);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(492, 133);
            this.tableLayoutPanel1.TabIndex = 29;
            // 
            // listBoxDownloads
            // 
            this.listBoxDownloads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxDownloads.FormattingEnabled = true;
            this.listBoxDownloads.Location = new System.Drawing.Point(249, 3);
            this.listBoxDownloads.Name = "listBoxDownloads";
            this.listBoxDownloads.Size = new System.Drawing.Size(240, 127);
            this.listBoxDownloads.TabIndex = 22;
            // 
            // ScraperForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GamelistManager.Properties.Resources.background2;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(504, 454);
            this.Controls.Add(this.panelEverything);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Margin = new System.Windows.Forms.Padding(1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(465, 334);
            this.Name = "ScraperForm";
            this.Text = "Scraper";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ScraperForm_FormClosing);
            this.Load += new System.EventHandler(this.Scraper_Load);
            this.panelEverything.ResumeLayout(false);
            this.panelScraperOptions.ResumeLayout(false);
            this.panelScraperOptions.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.contextMenuStripLog.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxMainLogo)).EndInit();
            this.panelSmall.ResumeLayout(false);
            this.panelCheckboxes.ResumeLayout(false);
            this.panelCheckboxes.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RadioButton radioButtonScrapeSelected;
        private System.Windows.Forms.Panel panelEverything;
        private System.Windows.Forms.ComboBox comboBoxScrapers;
        private System.Windows.Forms.CheckBox checkboxName;
        private System.Windows.Forms.CheckBox checkboxDesc;
        private System.Windows.Forms.CheckBox checkboxGenre;
        private System.Windows.Forms.CheckBox checkboxPlayers;
        private System.Windows.Forms.CheckBox checkboxRating;
        private System.Windows.Forms.CheckBox checkboxRegion;
        private System.Windows.Forms.CheckBox checkboxLang;
        private System.Windows.Forms.Panel panelSmall;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.ProgressBar progressBarScrapeProgress;
        private System.Windows.Forms.Button buttonSelectAll;
        private System.Windows.Forms.PictureBox pictureBoxMainLogo;
        private System.Windows.Forms.CheckBox checkboxImage;
        private System.Windows.Forms.CheckBox checkboxMarquee;
        private System.Windows.Forms.CheckBox checkboxThumbnail;
        private System.Windows.Forms.CheckBox checkboxVideo;
        private System.Windows.Forms.CheckBox checkboxReleasedate;
        private System.Windows.Forms.Button buttonSelectNone;
        private System.Windows.Forms.CheckBox checkBoxSave;
        private System.Windows.Forms.CheckBox checkBoxOverwriteExisting;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.CheckBox checkboxMap;
        private System.Windows.Forms.CheckBox checkboxManual;
        private System.Windows.Forms.CheckBox checkboxPublisher;
        private System.Windows.Forms.CheckBox checkboxDeveloper;
        private System.Windows.Forms.RadioButton radioButtonScrapeAll;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelScraper;
        private System.Windows.Forms.CheckBox checkBoxDoNotScrapeHidden;
        private System.Windows.Forms.Button buttonSetup;
        private System.Windows.Forms.Label labelMetadata;
        private System.Windows.Forms.Panel panelCheckboxes;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labelCounts;
        private System.Windows.Forms.Panel panelScraperOptions;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.CheckBox checkboxBezel;
        private System.Windows.Forms.CheckBox checkBoxSupressNotify;
        private System.Windows.Forms.CheckBox checkboxGenreID;
        private System.Windows.Forms.CheckBox checkboxArcadeSystemName;
        private System.Windows.Forms.CheckBox checkboxBoxBack;
        private System.Windows.Forms.CheckBox checkboxFanArt;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripLog;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemCopyLogToClipboard;
        private System.Windows.Forms.Label labelMedia;
        private System.Windows.Forms.Label labelScrapeLimitCounters;
        private System.Windows.Forms.Label labelScrape;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ListBox listBoxDownloads;
    }
}