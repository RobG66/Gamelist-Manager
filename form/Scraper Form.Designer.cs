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
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.checkBox14 = new System.Windows.Forms.CheckBox();
            this.checkBox_OverwriteExisting = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.checkbox_publisher = new System.Windows.Forms.CheckBox();
            this.checkbox_developer = new System.Windows.Forms.CheckBox();
            this.button3 = new System.Windows.Forms.Button();
            this.checkbox_releasedate = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
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
            this.button1 = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Location = new System.Drawing.Point(8, 69);
            this.radioButton1.Margin = new System.Windows.Forms.Padding(1);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(65, 17);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Scraper:";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.listBoxLog);
            this.panel1.Controls.Add(this.checkBox14);
            this.panel1.Controls.Add(this.checkBox_OverwriteExisting);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Controls.Add(this.progressBar1);
            this.panel1.Controls.Add(this.comboBox1);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Controls.Add(this.radioButton1);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Location = new System.Drawing.Point(10, 14);
            this.panel1.Margin = new System.Windows.Forms.Padding(1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(429, 271);
            this.panel1.TabIndex = 2;
            // 
            // listBoxLog
            // 
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.Location = new System.Drawing.Point(6, 179);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.Size = new System.Drawing.Size(204, 82);
            this.listBoxLog.TabIndex = 21;
            // 
            // checkBox14
            // 
            this.checkBox14.AutoSize = true;
            this.checkBox14.Location = new System.Drawing.Point(75, 115);
            this.checkBox14.Name = "checkBox14";
            this.checkBox14.Size = new System.Drawing.Size(119, 17);
            this.checkBox14.TabIndex = 20;
            this.checkBox14.Text = "Save when finished";
            this.checkBox14.UseVisualStyleBackColor = true;
            // 
            // checkBox_OverwriteExisting
            // 
            this.checkBox_OverwriteExisting.AutoSize = true;
            this.checkBox_OverwriteExisting.Location = new System.Drawing.Point(75, 92);
            this.checkBox_OverwriteExisting.Name = "checkBox_OverwriteExisting";
            this.checkBox_OverwriteExisting.Size = new System.Drawing.Size(130, 17);
            this.checkBox_OverwriteExisting.TabIndex = 19;
            this.checkBox_OverwriteExisting.Text = "Overwrite exising data";
            this.checkBox_OverwriteExisting.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 137);
            this.label1.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "Scraping: None";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::GamelistManager.Properties.Resources.scraperlogo;
            this.pictureBox1.Location = new System.Drawing.Point(-1, -1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(221, 64);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 14;
            this.pictureBox1.TabStop = false;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(6, 151);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(1);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(204, 24);
            this.progressBar1.TabIndex = 12;
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "ArcadeDB"});
            this.comboBox1.Location = new System.Drawing.Point(75, 67);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(1);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(135, 21);
            this.comboBox1.Sorted = true;
            this.comboBox1.TabIndex = 2;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.checkbox_publisher);
            this.panel2.Controls.Add(this.checkbox_developer);
            this.panel2.Controls.Add(this.button3);
            this.panel2.Controls.Add(this.checkbox_releasedate);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.button2);
            this.panel2.Controls.Add(this.checkbox_name);
            this.panel2.Controls.Add(this.checkbox_lang);
            this.panel2.Controls.Add(this.checkbox_desc);
            this.panel2.Controls.Add(this.checkbox_region);
            this.panel2.Controls.Add(this.checkbox_genre);
            this.panel2.Controls.Add(this.checkbox_rating);
            this.panel2.Controls.Add(this.checkbox_players);
            this.panel2.Controls.Add(this.checkbox_map);
            this.panel2.Controls.Add(this.checkbox_manual);
            this.panel2.Controls.Add(this.checkbox_video);
            this.panel2.Controls.Add(this.checkbox_image);
            this.panel2.Controls.Add(this.checkbox_marquee);
            this.panel2.Controls.Add(this.checkbox_thumbnail);
            this.panel2.Location = new System.Drawing.Point(224, 1);
            this.panel2.Margin = new System.Windows.Forms.Padding(1);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(202, 267);
            this.panel2.TabIndex = 10;
            // 
            // checkbox_publisher
            // 
            this.checkbox_publisher.AutoSize = true;
            this.checkbox_publisher.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_publisher.Location = new System.Drawing.Point(6, 220);
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
            this.checkbox_developer.Location = new System.Drawing.Point(6, 199);
            this.checkbox_developer.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_developer.Name = "checkbox_developer";
            this.checkbox_developer.Size = new System.Drawing.Size(75, 17);
            this.checkbox_developer.TabIndex = 10;
            this.checkbox_developer.Text = "Developer";
            this.checkbox_developer.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.AutoSize = true;
            this.button3.Location = new System.Drawing.Point(97, 182);
            this.button3.Margin = new System.Windows.Forms.Padding(1);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(76, 23);
            this.button3.TabIndex = 21;
            this.button3.Text = "Select None";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // checkbox_releasedate
            // 
            this.checkbox_releasedate.AutoSize = true;
            this.checkbox_releasedate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_releasedate.Location = new System.Drawing.Point(6, 178);
            this.checkbox_releasedate.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_releasedate.Name = "checkbox_releasedate";
            this.checkbox_releasedate.Size = new System.Drawing.Size(71, 17);
            this.checkbox_releasedate.TabIndex = 9;
            this.checkbox_releasedate.Text = "Released";
            this.checkbox_releasedate.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Green;
            this.label3.Location = new System.Drawing.Point(87, 4);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(46, 17);
            this.label3.TabIndex = 12;
            this.label3.Text = "Media";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Green;
            this.label2.Location = new System.Drawing.Point(3, 4);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "Metadata";
            // 
            // button2
            // 
            this.button2.AutoSize = true;
            this.button2.Location = new System.Drawing.Point(97, 157);
            this.button2.Margin = new System.Windows.Forms.Padding(1);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(61, 23);
            this.button2.TabIndex = 20;
            this.button2.Text = "Select All";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // checkbox_name
            // 
            this.checkbox_name.AutoSize = true;
            this.checkbox_name.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.checkbox_name.Location = new System.Drawing.Point(6, 31);
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
            this.checkbox_lang.Location = new System.Drawing.Point(6, 157);
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
            this.checkbox_desc.Location = new System.Drawing.Point(6, 52);
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
            this.checkbox_region.Location = new System.Drawing.Point(6, 136);
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
            this.checkbox_genre.Location = new System.Drawing.Point(6, 73);
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
            this.checkbox_rating.Location = new System.Drawing.Point(6, 115);
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
            this.checkbox_players.Location = new System.Drawing.Point(6, 94);
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
            this.checkbox_map.Location = new System.Drawing.Point(90, 115);
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
            this.checkbox_manual.Location = new System.Drawing.Point(90, 135);
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
            this.checkbox_video.Location = new System.Drawing.Point(90, 95);
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
            this.checkbox_image.Location = new System.Drawing.Point(90, 31);
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
            this.checkbox_marquee.Location = new System.Drawing.Point(90, 52);
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
            this.checkbox_thumbnail.Location = new System.Drawing.Point(90, 73);
            this.checkbox_thumbnail.Margin = new System.Windows.Forms.Padding(1);
            this.checkbox_thumbnail.Name = "checkbox_thumbnail";
            this.checkbox_thumbnail.Size = new System.Drawing.Size(75, 17);
            this.checkbox_thumbnail.TabIndex = 14;
            this.checkbox_thumbnail.Text = "Thumbnail";
            this.checkbox_thumbnail.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(8, 91);
            this.button1.Margin = new System.Windows.Forms.Padding(1);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(65, 24);
            this.button1.TabIndex = 11;
            this.button1.Text = "Scrape!";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Scraper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GamelistManager.Properties.Resources.background11;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ClientSize = new System.Drawing.Size(449, 295);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(1);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(465, 334);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(465, 334);
            this.Name = "Scraper";
            this.Text = "Scraper";
            this.Load += new System.EventHandler(this.Scraper_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.CheckBox checkbox_name;
        private System.Windows.Forms.CheckBox checkbox_desc;
        private System.Windows.Forms.CheckBox checkbox_genre;
        private System.Windows.Forms.CheckBox checkbox_players;
        private System.Windows.Forms.CheckBox checkbox_rating;
        private System.Windows.Forms.CheckBox checkbox_region;
        private System.Windows.Forms.CheckBox checkbox_lang;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkbox_image;
        private System.Windows.Forms.CheckBox checkbox_marquee;
        private System.Windows.Forms.CheckBox checkbox_thumbnail;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkbox_video;
        private System.Windows.Forms.CheckBox checkbox_releasedate;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.CheckBox checkBox14;
        private System.Windows.Forms.CheckBox checkBox_OverwriteExisting;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.CheckBox checkbox_map;
        private System.Windows.Forms.CheckBox checkbox_manual;
        private System.Windows.Forms.CheckBox checkbox_publisher;
        private System.Windows.Forms.CheckBox checkbox_developer;
    }
}