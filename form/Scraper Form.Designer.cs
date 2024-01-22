namespace GamelistManager.form
{
    partial class Scraper
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
            this.RadioButton_ScrapeSelected = new System.Windows.Forms.RadioButton();
            this.panel_Everything = new System.Windows.Forms.Panel();
            this.RadioButton_ScrapeAll = new System.Windows.Forms.RadioButton();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.checkBox_Save = new System.Windows.Forms.CheckBox();
            this.checkBox_OverwriteExisting = new System.Windows.Forms.CheckBox();
            this.pictureBox_MainLogo = new System.Windows.Forms.PictureBox();
            this.progressBar_ScrapeProgress = new System.Windows.Forms.ProgressBar();
            this.comboBox_Scrapers = new System.Windows.Forms.ComboBox();
            this.panel_CheckboxGroup = new System.Windows.Forms.Panel();
            this.label_Note = new System.Windows.Forms.Label();
            this.checkbox_publisher = new System.Windows.Forms.CheckBox();
            this.checkbox_developer = new System.Windows.Forms.CheckBox();
            this.button_SelectNone = new System.Windows.Forms.Button();
            this.checkbox_releasedate = new System.Windows.Forms.CheckBox();
            this.label_Media = new System.Windows.Forms.Label();
            this.label_Metadata = new System.Windows.Forms.Label();
            this.button_SelectAll = new System.Windows.Forms.Button();
            this.checkbox_name = new System.Windows.Forms.CheckBox();
            this.checkbox_lang = new System.Windows.Forms.CheckBox();
            this.checkbox_desc = new System.Windows.Forms.CheckBox();
            this.checkbox_region = new System.Windows.Forms.CheckBox();
            this.checkbox_genre = new System.Windows.Forms.CheckBox();
            this.checkbox_rating = new System.Windows.Forms.CheckBox();
            this.checkbox_players = new System.Windows.Forms.CheckBox();
            this.checkbox_map = new System.Windows.Forms.CheckBox();
            this.checkbox_manual = new System.Windows.Forms.CheckBox();
            this.checkbox_video = new System.Windows.Forms.CheckBox();
            this.checkbox_image = new System.Windows.Forms.CheckBox();
            this.checkbox_marquee = new System.Windows.Forms.CheckBox();
            this.checkbox_thumbnail = new System.Windows.Forms.CheckBox();
            this.button_StartStop = new System.Windows.Forms.Button();
            this.button_Cancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel_Everything.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_MainLogo)).BeginInit();
            this.panel_CheckboxGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // RadioButton_ScrapeSelected
            // 
            this.RadioButton_ScrapeSelected.AutoSize = true;
            this.RadioButton_ScrapeSelected.Checked = true;
            this.RadioButton_ScrapeSelected.Location = new System.Drawing.Point(8, 95);
            this.RadioButton_ScrapeSelected.Margin = new System.Windows.Forms.Padding(1);
            this.RadioButton_ScrapeSelected.Name = "RadioButton_ScrapeSelected";
            this.RadioButton_ScrapeSelected.Size = new System.Drawing.Size(128, 17);
            this.RadioButton_ScrapeSelected.TabIndex = 0;
            this.RadioButton_ScrapeSelected.TabStop = true;
            this.RadioButton_ScrapeSelected.Text = "Scrape Selected Only";
            this.RadioButton_ScrapeSelected.UseVisualStyleBackColor = true;
            this.RadioButton_ScrapeSelected.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // panel_Everything
            // 
            this.panel_Everything.BackColor = System.Drawing.Color.Transparent;
            this.panel_Everything.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.panel_Everything.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel_Everything.Controls.Add(this.label2);
            this.panel_Everything.Controls.Add(this.label);
            this.panel_Everything.Controls.Add(this.label1);
            this.panel_Everything.Controls.Add(this.button_Cancel);
            this.panel_Everything.Controls.Add(this.RadioButton_ScrapeAll);
            this.panel_Everything.Controls.Add(this.listBoxLog);
            this.panel_Everything.Controls.Add(this.checkBox_Save);
            this.panel_Everything.Controls.Add(this.checkBox_OverwriteExisting);
            this.panel_Everything.Controls.Add(this.pictureBox_MainLogo);
            this.panel_Everything.Controls.Add(this.progressBar_ScrapeProgress);
            this.panel_Everything.Controls.Add(this.comboBox_Scrapers);
            this.panel_Everything.Controls.Add(this.panel_CheckboxGroup);
            this.panel_Everything.Controls.Add(this.RadioButton_ScrapeSelected);
            this.panel_Everything.Controls.Add(this.button_StartStop);
            this.panel_Everything.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_Everything.Location = new System.Drawing.Point(0, 0);
            this.panel_Everything.Margin = new System.Windows.Forms.Padding(1);
            this.panel_Everything.Name = "panel_Everything";
            this.panel_Everything.Size = new System.Drawing.Size(449, 369);
            this.panel_Everything.TabIndex = 2;
            // 
            // RadioButton_ScrapeAll
            // 
            this.RadioButton_ScrapeAll.AutoSize = true;
            this.RadioButton_ScrapeAll.Location = new System.Drawing.Point(8, 114);
            this.RadioButton_ScrapeAll.Name = "RadioButton_ScrapeAll";
            this.RadioButton_ScrapeAll.Size = new System.Drawing.Size(101, 17);
            this.RadioButton_ScrapeAll.TabIndex = 22;
            this.RadioButton_ScrapeAll.TabStop = true;
            this.RadioButton_ScrapeAll.Text = "Scrape All Items";
            this.RadioButton_ScrapeAll.UseVisualStyleBackColor = true;
            // 
            // listBoxLog
            // 
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.Location = new System.Drawing.Point(3, 250);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.Size = new System.Drawing.Size(441, 108);
            this.listBoxLog.TabIndex = 21;
            // 
            // checkBox_Save
            // 
            this.checkBox_Save.AutoSize = true;
            this.checkBox_Save.Location = new System.Drawing.Point(8, 156);
            this.checkBox_Save.Name = "checkBox_Save";
            this.checkBox_Save.Size = new System.Drawing.Size(156, 17);
            this.checkBox_Save.TabIndex = 20;
            this.checkBox_Save.Text = "Prompt for save when done";
            this.checkBox_Save.UseVisualStyleBackColor = true;
            // 
            // checkBox_OverwriteExisting
            // 
            this.checkBox_OverwriteExisting.AutoSize = true;
            this.checkBox_OverwriteExisting.Location = new System.Drawing.Point(8, 137);
            this.checkBox_OverwriteExisting.Name = "checkBox_OverwriteExisting";
            this.checkBox_OverwriteExisting.Size = new System.Drawing.Size(195, 17);
            this.checkBox_OverwriteExisting.TabIndex = 19;
            this.checkBox_OverwriteExisting.Text = "Overwrite exising metadata and files";
            this.checkBox_OverwriteExisting.UseVisualStyleBackColor = true;
            // 
            // pictureBox_MainLogo
            // 
            this.pictureBox_MainLogo.Image = global::GamelistManager.Properties.Resources.scraperlogo;
            this.pictureBox_MainLogo.Location = new System.Drawing.Point(-1, -1);
            this.pictureBox_MainLogo.Name = "pictureBox_MainLogo";
            this.pictureBox_MainLogo.Size = new System.Drawing.Size(221, 64);
            this.pictureBox_MainLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox_MainLogo.TabIndex = 14;
            this.pictureBox_MainLogo.TabStop = false;
            // 
            // progressBar_ScrapeProgress
            // 
            this.progressBar_ScrapeProgress.Location = new System.Drawing.Point(59, 234);
            this.progressBar_ScrapeProgress.Margin = new System.Windows.Forms.Padding(1);
            this.progressBar_ScrapeProgress.Name = "progressBar_ScrapeProgress";
            this.progressBar_ScrapeProgress.Size = new System.Drawing.Size(122, 14);
            this.progressBar_ScrapeProgress.TabIndex = 12;
            // 
            // comboBox_Scrapers
            // 
            this.comboBox_Scrapers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Scrapers.FormattingEnabled = true;
            this.comboBox_Scrapers.Items.AddRange(new object[] {
            "ArcadeDB"});
            this.comboBox_Scrapers.Location = new System.Drawing.Point(59, 70);
            this.comboBox_Scrapers.Margin = new System.Windows.Forms.Padding(1);
            this.comboBox_Scrapers.Name = "comboBox_Scrapers";
            this.comboBox_Scrapers.Size = new System.Drawing.Size(92, 21);
            this.comboBox_Scrapers.Sorted = true;
            this.comboBox_Scrapers.TabIndex = 2;
            this.comboBox_Scrapers.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // panel_CheckboxGroup
            // 
            this.panel_CheckboxGroup.Controls.Add(this.label_Note);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_publisher);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_developer);
            this.panel_CheckboxGroup.Controls.Add(this.button_SelectNone);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_releasedate);
            this.panel_CheckboxGroup.Controls.Add(this.label_Media);
            this.panel_CheckboxGroup.Controls.Add(this.label_Metadata);
            this.panel_CheckboxGroup.Controls.Add(this.button_SelectAll);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_name);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_lang);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_desc);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_region);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_genre);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_rating);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_players);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_map);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_manual);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_video);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_image);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_marquee);
            this.panel_CheckboxGroup.Controls.Add(this.checkbox_thumbnail);
            this.panel_CheckboxGroup.Location = new System.Drawing.Point(224, 1);
            this.panel_CheckboxGroup.Margin = new System.Windows.Forms.Padding(1);
            this.panel_CheckboxGroup.Name = "panel_CheckboxGroup";
            this.panel_CheckboxGroup.Size = new System.Drawing.Size(202, 245);
            this.panel_CheckboxGroup.TabIndex = 10;
            // 
            // label_Note
            // 
            this.label_Note.Location = new System.Drawing.Point(86, 198);
            this.label_Note.Name = "label_Note";
            this.label_Note.Size = new System.Drawing.Size(113, 44);
            this.label_Note.TabIndex = 22;
            this.label_Note.Text = "Note: Element availability will vary between scrapers.";
            // 
            // checkbox_publisher
            // 
            this.checkbox_publisher.AutoSize = true;
            this.checkbox_publisher.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_publisher.Location = new System.Drawing.Point(6, 212);
            this.checkbox_publisher.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_publisher.Name = "checkbox_publisher";
            this.checkbox_publisher.Size = new System.Drawing.Size(69, 17);
            this.checkbox_publisher.TabIndex = 11;
            this.checkbox_publisher.Text = "Publisher";
            this.checkbox_publisher.UseVisualStyleBackColor = true;
            // 
            // checkbox_developer
            // 
            this.checkbox_developer.AutoSize = true;
            this.checkbox_developer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_developer.Location = new System.Drawing.Point(6, 191);
            this.checkbox_developer.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_developer.Name = "checkbox_developer";
            this.checkbox_developer.Size = new System.Drawing.Size(75, 17);
            this.checkbox_developer.TabIndex = 10;
            this.checkbox_developer.Text = "Developer";
            this.checkbox_developer.UseVisualStyleBackColor = true;
            // 
            // button_SelectNone
            // 
            this.button_SelectNone.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.button_SelectNone.Location = new System.Drawing.Point(89, 172);
            this.button_SelectNone.Margin = new System.Windows.Forms.Padding(1);
            this.button_SelectNone.Name = "button_SelectNone";
            this.button_SelectNone.Size = new System.Drawing.Size(76, 23);
            this.button_SelectNone.TabIndex = 21;
            this.button_SelectNone.Text = "Select None";
            this.button_SelectNone.UseVisualStyleBackColor = false;
            this.button_SelectNone.Click += new System.EventHandler(this.button3_Click);
            // 
            // checkbox_releasedate
            // 
            this.checkbox_releasedate.AutoSize = true;
            this.checkbox_releasedate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_releasedate.Location = new System.Drawing.Point(6, 170);
            this.checkbox_releasedate.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_releasedate.Name = "checkbox_releasedate";
            this.checkbox_releasedate.Size = new System.Drawing.Size(71, 17);
            this.checkbox_releasedate.TabIndex = 9;
            this.checkbox_releasedate.Text = "Released";
            this.checkbox_releasedate.UseVisualStyleBackColor = true;
            // 
            // label_Media
            // 
            this.label_Media.AutoSize = true;
            this.label_Media.Font = new System.Drawing.Font("Segoe UI", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Media.ForeColor = System.Drawing.Color.Green;
            this.label_Media.Location = new System.Drawing.Point(87, 4);
            this.label_Media.Name = "label_Media";
            this.label_Media.Size = new System.Drawing.Size(46, 17);
            this.label_Media.TabIndex = 12;
            this.label_Media.Text = "Media";
            // 
            // label_Metadata
            // 
            this.label_Metadata.AutoSize = true;
            this.label_Metadata.Font = new System.Drawing.Font("Segoe UI", 9.75F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Metadata.ForeColor = System.Drawing.Color.Green;
            this.label_Metadata.Location = new System.Drawing.Point(3, 4);
            this.label_Metadata.Name = "label_Metadata";
            this.label_Metadata.Size = new System.Drawing.Size(66, 17);
            this.label_Metadata.TabIndex = 1;
            this.label_Metadata.Text = "Metadata";
            // 
            // button_SelectAll
            // 
            this.button_SelectAll.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
            this.button_SelectAll.Location = new System.Drawing.Point(89, 149);
            this.button_SelectAll.Margin = new System.Windows.Forms.Padding(1);
            this.button_SelectAll.Name = "button_SelectAll";
            this.button_SelectAll.Size = new System.Drawing.Size(76, 23);
            this.button_SelectAll.TabIndex = 20;
            this.button_SelectAll.Text = "Select All";
            this.button_SelectAll.UseVisualStyleBackColor = false;
            this.button_SelectAll.Click += new System.EventHandler(this.button2_Click);
            // 
            // checkbox_name
            // 
            this.checkbox_name.AutoSize = true;
            this.checkbox_name.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_name.Location = new System.Drawing.Point(6, 23);
            this.checkbox_name.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_name.Name = "checkbox_name";
            this.checkbox_name.Size = new System.Drawing.Size(54, 17);
            this.checkbox_name.TabIndex = 2;
            this.checkbox_name.Text = "Name";
            this.checkbox_name.UseVisualStyleBackColor = true;
            // 
            // checkbox_lang
            // 
            this.checkbox_lang.AutoSize = true;
            this.checkbox_lang.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_lang.Location = new System.Drawing.Point(6, 149);
            this.checkbox_lang.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_lang.Name = "checkbox_lang";
            this.checkbox_lang.Size = new System.Drawing.Size(74, 17);
            this.checkbox_lang.TabIndex = 8;
            this.checkbox_lang.Text = "Language";
            this.checkbox_lang.UseVisualStyleBackColor = true;
            // 
            // checkbox_desc
            // 
            this.checkbox_desc.AutoSize = true;
            this.checkbox_desc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_desc.Location = new System.Drawing.Point(6, 44);
            this.checkbox_desc.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_desc.Name = "checkbox_desc";
            this.checkbox_desc.Size = new System.Drawing.Size(79, 17);
            this.checkbox_desc.TabIndex = 3;
            this.checkbox_desc.Text = "Description";
            this.checkbox_desc.UseVisualStyleBackColor = true;
            // 
            // checkbox_region
            // 
            this.checkbox_region.AutoSize = true;
            this.checkbox_region.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_region.Location = new System.Drawing.Point(6, 128);
            this.checkbox_region.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_region.Name = "checkbox_region";
            this.checkbox_region.Size = new System.Drawing.Size(60, 17);
            this.checkbox_region.TabIndex = 7;
            this.checkbox_region.Text = "Region";
            this.checkbox_region.UseVisualStyleBackColor = true;
            // 
            // checkbox_genre
            // 
            this.checkbox_genre.AutoSize = true;
            this.checkbox_genre.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_genre.Location = new System.Drawing.Point(6, 65);
            this.checkbox_genre.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_genre.Name = "checkbox_genre";
            this.checkbox_genre.Size = new System.Drawing.Size(55, 17);
            this.checkbox_genre.TabIndex = 4;
            this.checkbox_genre.Text = "Genre";
            this.checkbox_genre.UseVisualStyleBackColor = true;
            // 
            // checkbox_rating
            // 
            this.checkbox_rating.AutoSize = true;
            this.checkbox_rating.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_rating.Location = new System.Drawing.Point(6, 107);
            this.checkbox_rating.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_rating.Name = "checkbox_rating";
            this.checkbox_rating.Size = new System.Drawing.Size(57, 17);
            this.checkbox_rating.TabIndex = 6;
            this.checkbox_rating.Text = "Rating";
            this.checkbox_rating.UseVisualStyleBackColor = true;
            // 
            // checkbox_players
            // 
            this.checkbox_players.AutoSize = true;
            this.checkbox_players.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_players.Location = new System.Drawing.Point(6, 86);
            this.checkbox_players.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_players.Name = "checkbox_players";
            this.checkbox_players.Size = new System.Drawing.Size(60, 17);
            this.checkbox_players.TabIndex = 5;
            this.checkbox_players.Text = "Players";
            this.checkbox_players.UseVisualStyleBackColor = true;
            // 
            // checkbox_map
            // 
            this.checkbox_map.AutoSize = true;
            this.checkbox_map.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_map.Location = new System.Drawing.Point(90, 107);
            this.checkbox_map.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_map.Name = "checkbox_map";
            this.checkbox_map.Size = new System.Drawing.Size(47, 17);
            this.checkbox_map.TabIndex = 16;
            this.checkbox_map.Text = "Map";
            this.checkbox_map.UseVisualStyleBackColor = true;
            // 
            // checkbox_manual
            // 
            this.checkbox_manual.AutoSize = true;
            this.checkbox_manual.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_manual.Location = new System.Drawing.Point(90, 127);
            this.checkbox_manual.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_manual.Name = "checkbox_manual";
            this.checkbox_manual.Size = new System.Drawing.Size(61, 17);
            this.checkbox_manual.TabIndex = 18;
            this.checkbox_manual.Text = "Manual";
            this.checkbox_manual.UseVisualStyleBackColor = true;
            // 
            // checkbox_video
            // 
            this.checkbox_video.AutoSize = true;
            this.checkbox_video.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_video.Location = new System.Drawing.Point(90, 87);
            this.checkbox_video.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_video.Name = "checkbox_video";
            this.checkbox_video.Size = new System.Drawing.Size(53, 17);
            this.checkbox_video.TabIndex = 15;
            this.checkbox_video.Text = "Video";
            this.checkbox_video.UseVisualStyleBackColor = true;
            // 
            // checkbox_image
            // 
            this.checkbox_image.AutoSize = true;
            this.checkbox_image.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_image.Location = new System.Drawing.Point(90, 23);
            this.checkbox_image.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_image.Name = "checkbox_image";
            this.checkbox_image.Size = new System.Drawing.Size(55, 17);
            this.checkbox_image.TabIndex = 12;
            this.checkbox_image.Text = "Image";
            this.checkbox_image.UseVisualStyleBackColor = true;
            // 
            // checkbox_marquee
            // 
            this.checkbox_marquee.AutoSize = true;
            this.checkbox_marquee.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_marquee.Location = new System.Drawing.Point(90, 44);
            this.checkbox_marquee.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_marquee.Name = "checkbox_marquee";
            this.checkbox_marquee.Size = new System.Drawing.Size(68, 17);
            this.checkbox_marquee.TabIndex = 13;
            this.checkbox_marquee.Text = "Marquee";
            this.checkbox_marquee.UseVisualStyleBackColor = true;
            // 
            // checkbox_thumbnail
            // 
            this.checkbox_thumbnail.AutoSize = true;
            this.checkbox_thumbnail.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_thumbnail.Location = new System.Drawing.Point(90, 65);
            this.checkbox_thumbnail.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_thumbnail.Name = "checkbox_thumbnail";
            this.checkbox_thumbnail.Size = new System.Drawing.Size(75, 17);
            this.checkbox_thumbnail.TabIndex = 14;
            this.checkbox_thumbnail.Text = "Thumbnail";
            this.checkbox_thumbnail.UseVisualStyleBackColor = true;
            // 
            // button_StartStop
            // 
            this.button_StartStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.button_StartStop.Location = new System.Drawing.Point(157, 67);
            this.button_StartStop.Margin = new System.Windows.Forms.Padding(1);
            this.button_StartStop.Name = "button_StartStop";
            this.button_StartStop.Size = new System.Drawing.Size(65, 24);
            this.button_StartStop.TabIndex = 11;
            this.button_StartStop.Text = "Start";
            this.button_StartStop.UseVisualStyleBackColor = false;
            this.button_StartStop.Click += new System.EventHandler(this.button1_Click);
            // 
            // button_Cancel
            // 
            this.button_Cancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.button_Cancel.Enabled = false;
            this.button_Cancel.Location = new System.Drawing.Point(157, 92);
            this.button_Cancel.Name = "button_Cancel";
            this.button_Cancel.Size = new System.Drawing.Size(63, 23);
            this.button_Cancel.TabIndex = 23;
            this.button_Cancel.Text = "Cancel";
            this.button_Cancel.UseVisualStyleBackColor = false;
            this.button_Cancel.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 234);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "Progress:";
            // 
            // label
            // 
            this.label.AutoSize = true;
            this.label.Location = new System.Drawing.Point(185, 234);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(21, 13);
            this.label.TabIndex = 25;
            this.label.Text = "0%";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 13);
            this.label2.TabIndex = 26;
            this.label2.Text = "Scraper:";
            // 
            // Scraper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GamelistManager.Properties.Resources.background11;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(449, 369);
            this.Controls.Add(this.panel_Everything);
            this.Margin = new System.Windows.Forms.Padding(1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(465, 334);
            this.Name = "Scraper";
            this.Text = "Scraper";
            this.Load += new System.EventHandler(this.Scraper_Load);
            this.panel_Everything.ResumeLayout(false);
            this.panel_Everything.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_MainLogo)).EndInit();
            this.panel_CheckboxGroup.ResumeLayout(false);
            this.panel_CheckboxGroup.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RadioButton RadioButton_ScrapeSelected;
        private System.Windows.Forms.Panel panel_Everything;
        private System.Windows.Forms.ComboBox comboBox_Scrapers;
        private System.Windows.Forms.CheckBox checkbox_name;
        private System.Windows.Forms.CheckBox checkbox_desc;
        private System.Windows.Forms.CheckBox checkbox_genre;
        private System.Windows.Forms.CheckBox checkbox_players;
        private System.Windows.Forms.CheckBox checkbox_rating;
        private System.Windows.Forms.CheckBox checkbox_region;
        private System.Windows.Forms.CheckBox checkbox_lang;
        private System.Windows.Forms.Panel panel_CheckboxGroup;
        private System.Windows.Forms.Button button_StartStop;
        private System.Windows.Forms.ProgressBar progressBar_ScrapeProgress;
        private System.Windows.Forms.Button button_SelectAll;
        private System.Windows.Forms.PictureBox pictureBox_MainLogo;
        private System.Windows.Forms.Label label_Metadata;
        private System.Windows.Forms.CheckBox checkbox_image;
        private System.Windows.Forms.CheckBox checkbox_marquee;
        private System.Windows.Forms.CheckBox checkbox_thumbnail;
        private System.Windows.Forms.Label label_Media;
        private System.Windows.Forms.CheckBox checkbox_video;
        private System.Windows.Forms.CheckBox checkbox_releasedate;
        private System.Windows.Forms.Button button_SelectNone;
        private System.Windows.Forms.CheckBox checkBox_Save;
        private System.Windows.Forms.CheckBox checkBox_OverwriteExisting;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.CheckBox checkbox_map;
        private System.Windows.Forms.CheckBox checkbox_manual;
        private System.Windows.Forms.CheckBox checkbox_publisher;
        private System.Windows.Forms.CheckBox checkbox_developer;
        private System.Windows.Forms.RadioButton RadioButton_ScrapeAll;
        private System.Windows.Forms.Label label_Note;
        private System.Windows.Forms.Button button_Cancel;
        private System.Windows.Forms.Label label;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}