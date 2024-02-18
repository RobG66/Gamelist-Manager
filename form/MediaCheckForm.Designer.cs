namespace GamelistManager
{
    partial class MediaCheckForm
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
            this.buttonCleanup = new System.Windows.Forms.Button();
            this.radioButtonRename = new System.Windows.Forms.RadioButton();
            this.radioButtonDelete = new System.Windows.Forms.RadioButton();
            this.radioButtonExportCSV = new System.Windows.Forms.RadioButton();
            this.buttonStart = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.panelManageMedia = new System.Windows.Forms.Panel();
            this.labelManageBadMedia = new System.Windows.Forms.Label();
            this.labelInstruction = new System.Windows.Forms.Label();
            this.labelProgress = new System.Windows.Forms.Label();
            this.labelMissingImages = new System.Windows.Forms.Label();
            this.labelMissingImageCount = new System.Windows.Forms.Label();
            this.labelCorruptImageCount = new System.Windows.Forms.Label();
            this.labelCorruptImages = new System.Windows.Forms.Label();
            this.labelSingleColorImageCount = new System.Windows.Forms.Label();
            this.labelSingleColorImages = new System.Windows.Forms.Label();
            this.panelCheckMedia = new System.Windows.Forms.Panel();
            this.labelMissingVideos = new System.Windows.Forms.Label();
            this.labelMissingVideosCount = new System.Windows.Forms.Label();
            this.labelVideos = new System.Windows.Forms.Label();
            this.labelTotalVideosCount = new System.Windows.Forms.Label();
            this.labelImages = new System.Windows.Forms.Label();
            this.labelTotalImagesCount = new System.Windows.Forms.Label();
            this.buttonStop = new System.Windows.Forms.Button();
            this.listBoxLog = new System.Windows.Forms.ListBox();
            this.panelManageMedia.SuspendLayout();
            this.panelCheckMedia.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonCleanup
            // 
            this.buttonCleanup.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(128)))));
            this.buttonCleanup.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.buttonCleanup.Location = new System.Drawing.Point(237, 8);
            this.buttonCleanup.Name = "buttonCleanup";
            this.buttonCleanup.Size = new System.Drawing.Size(75, 23);
            this.buttonCleanup.TabIndex = 9;
            this.buttonCleanup.Text = "Cleanup";
            this.buttonCleanup.UseVisualStyleBackColor = false;
            this.buttonCleanup.Click += new System.EventHandler(this.button1_Click);
            // 
            // radioButtonRename
            // 
            this.radioButtonRename.AutoSize = true;
            this.radioButtonRename.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonRename.Location = new System.Drawing.Point(3, 78);
            this.radioButtonRename.Name = "radioButtonRename";
            this.radioButtonRename.Size = new System.Drawing.Size(248, 17);
            this.radioButtonRename.TabIndex = 8;
            this.radioButtonRename.Text = "Rename with a \'bad-\' prefix and clear paths";
            this.radioButtonRename.UseVisualStyleBackColor = true;
            // 
            // radioButtonDelete
            // 
            this.radioButtonDelete.AutoSize = true;
            this.radioButtonDelete.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonDelete.Location = new System.Drawing.Point(3, 54);
            this.radioButtonDelete.Name = "radioButtonDelete";
            this.radioButtonDelete.Size = new System.Drawing.Size(140, 17);
            this.radioButtonDelete.TabIndex = 7;
            this.radioButtonDelete.Text = "Delete and clear paths";
            this.radioButtonDelete.UseVisualStyleBackColor = true;
            // 
            // radioButtonExportCSV
            // 
            this.radioButtonExportCSV.AutoSize = true;
            this.radioButtonExportCSV.Checked = true;
            this.radioButtonExportCSV.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonExportCSV.Location = new System.Drawing.Point(3, 30);
            this.radioButtonExportCSV.Name = "radioButtonExportCSV";
            this.radioButtonExportCSV.Size = new System.Drawing.Size(113, 17);
            this.radioButtonExportCSV.TabIndex = 6;
            this.radioButtonExportCSV.TabStop = true;
            this.radioButtonExportCSV.Text = "Export list to CSV";
            this.radioButtonExportCSV.UseVisualStyleBackColor = true;
            // 
            // buttonStart
            // 
            this.buttonStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.buttonStart.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.buttonStart.Location = new System.Drawing.Point(3, 51);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 13;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = false;
            this.buttonStart.Click += new System.EventHandler(this.button_Start_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(3, 99);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(315, 23);
            this.progressBar1.TabIndex = 14;
            // 
            // panelManageMedia
            // 
            this.panelManageMedia.BackColor = System.Drawing.Color.Transparent;
            this.panelManageMedia.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelManageMedia.Controls.Add(this.labelManageBadMedia);
            this.panelManageMedia.Controls.Add(this.radioButtonExportCSV);
            this.panelManageMedia.Controls.Add(this.radioButtonDelete);
            this.panelManageMedia.Controls.Add(this.radioButtonRename);
            this.panelManageMedia.Controls.Add(this.buttonCleanup);
            this.panelManageMedia.Enabled = false;
            this.panelManageMedia.Location = new System.Drawing.Point(2, 140);
            this.panelManageMedia.Name = "panelManageMedia";
            this.panelManageMedia.Size = new System.Drawing.Size(323, 107);
            this.panelManageMedia.TabIndex = 16;
            // 
            // labelManageBadMedia
            // 
            this.labelManageBadMedia.AutoSize = true;
            this.labelManageBadMedia.BackColor = System.Drawing.Color.Transparent;
            this.labelManageBadMedia.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelManageBadMedia.ForeColor = System.Drawing.Color.MidnightBlue;
            this.labelManageBadMedia.Location = new System.Drawing.Point(3, 8);
            this.labelManageBadMedia.Name = "labelManageBadMedia";
            this.labelManageBadMedia.Size = new System.Drawing.Size(109, 15);
            this.labelManageBadMedia.TabIndex = 18;
            this.labelManageBadMedia.Text = "Manage Bad Media";
            // 
            // labelInstruction
            // 
            this.labelInstruction.AutoSize = true;
            this.labelInstruction.BackColor = System.Drawing.Color.Transparent;
            this.labelInstruction.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelInstruction.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelInstruction.Location = new System.Drawing.Point(180, 83);
            this.labelInstruction.Name = "labelInstruction";
            this.labelInstruction.Size = new System.Drawing.Size(138, 13);
            this.labelInstruction.TabIndex = 17;
            this.labelInstruction.Text = "Click Start to check media";
            // 
            // labelProgress
            // 
            this.labelProgress.AutoSize = true;
            this.labelProgress.BackColor = System.Drawing.Color.Transparent;
            this.labelProgress.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.labelProgress.Location = new System.Drawing.Point(9, 83);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(22, 13);
            this.labelProgress.TabIndex = 26;
            this.labelProgress.Text = "0%";
            // 
            // labelMissingImages
            // 
            this.labelMissingImages.AutoSize = true;
            this.labelMissingImages.BackColor = System.Drawing.Color.Transparent;
            this.labelMissingImages.ForeColor = System.Drawing.Color.MidnightBlue;
            this.labelMissingImages.Location = new System.Drawing.Point(194, 25);
            this.labelMissingImages.Name = "labelMissingImages";
            this.labelMissingImages.Size = new System.Drawing.Size(82, 13);
            this.labelMissingImages.TabIndex = 27;
            this.labelMissingImages.Text = "Missing Images:";
            // 
            // labelMissingImageCount
            // 
            this.labelMissingImageCount.AutoSize = true;
            this.labelMissingImageCount.BackColor = System.Drawing.Color.Transparent;
            this.labelMissingImageCount.ForeColor = System.Drawing.Color.Red;
            this.labelMissingImageCount.Location = new System.Drawing.Point(278, 25);
            this.labelMissingImageCount.Name = "labelMissingImageCount";
            this.labelMissingImageCount.Size = new System.Drawing.Size(13, 13);
            this.labelMissingImageCount.TabIndex = 28;
            this.labelMissingImageCount.Text = "0";
            // 
            // labelCorruptImageCount
            // 
            this.labelCorruptImageCount.AutoSize = true;
            this.labelCorruptImageCount.BackColor = System.Drawing.Color.Transparent;
            this.labelCorruptImageCount.ForeColor = System.Drawing.Color.Red;
            this.labelCorruptImageCount.Location = new System.Drawing.Point(278, 41);
            this.labelCorruptImageCount.Name = "labelCorruptImageCount";
            this.labelCorruptImageCount.Size = new System.Drawing.Size(13, 13);
            this.labelCorruptImageCount.TabIndex = 30;
            this.labelCorruptImageCount.Text = "0";
            // 
            // labelCorruptImages
            // 
            this.labelCorruptImages.AutoSize = true;
            this.labelCorruptImages.BackColor = System.Drawing.Color.Transparent;
            this.labelCorruptImages.ForeColor = System.Drawing.Color.MidnightBlue;
            this.labelCorruptImages.Location = new System.Drawing.Point(195, 41);
            this.labelCorruptImages.Name = "labelCorruptImages";
            this.labelCorruptImages.Size = new System.Drawing.Size(81, 13);
            this.labelCorruptImages.TabIndex = 29;
            this.labelCorruptImages.Text = "Corrupt Images:";
            // 
            // labelSingleColorImageCount
            // 
            this.labelSingleColorImageCount.AutoSize = true;
            this.labelSingleColorImageCount.BackColor = System.Drawing.Color.Transparent;
            this.labelSingleColorImageCount.ForeColor = System.Drawing.Color.Red;
            this.labelSingleColorImageCount.Location = new System.Drawing.Point(278, 57);
            this.labelSingleColorImageCount.Name = "labelSingleColorImageCount";
            this.labelSingleColorImageCount.Size = new System.Drawing.Size(13, 13);
            this.labelSingleColorImageCount.TabIndex = 32;
            this.labelSingleColorImageCount.Text = "0";
            // 
            // labelSingleColorImages
            // 
            this.labelSingleColorImages.AutoSize = true;
            this.labelSingleColorImages.BackColor = System.Drawing.Color.Transparent;
            this.labelSingleColorImages.ForeColor = System.Drawing.Color.MidnightBlue;
            this.labelSingleColorImages.Location = new System.Drawing.Point(173, 57);
            this.labelSingleColorImages.Name = "labelSingleColorImages";
            this.labelSingleColorImages.Size = new System.Drawing.Size(103, 13);
            this.labelSingleColorImages.TabIndex = 31;
            this.labelSingleColorImages.Text = "Single Color Images:";
            // 
            // panelCheckMedia
            // 
            this.panelCheckMedia.BackColor = System.Drawing.Color.Transparent;
            this.panelCheckMedia.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelCheckMedia.Controls.Add(this.labelMissingVideos);
            this.panelCheckMedia.Controls.Add(this.labelMissingVideosCount);
            this.panelCheckMedia.Controls.Add(this.labelVideos);
            this.panelCheckMedia.Controls.Add(this.labelTotalVideosCount);
            this.panelCheckMedia.Controls.Add(this.labelImages);
            this.panelCheckMedia.Controls.Add(this.labelTotalImagesCount);
            this.panelCheckMedia.Controls.Add(this.buttonStop);
            this.panelCheckMedia.Controls.Add(this.labelMissingImages);
            this.panelCheckMedia.Controls.Add(this.progressBar1);
            this.panelCheckMedia.Controls.Add(this.labelSingleColorImageCount);
            this.panelCheckMedia.Controls.Add(this.labelProgress);
            this.panelCheckMedia.Controls.Add(this.labelMissingImageCount);
            this.panelCheckMedia.Controls.Add(this.labelInstruction);
            this.panelCheckMedia.Controls.Add(this.labelSingleColorImages);
            this.panelCheckMedia.Controls.Add(this.labelCorruptImages);
            this.panelCheckMedia.Controls.Add(this.labelCorruptImageCount);
            this.panelCheckMedia.Controls.Add(this.buttonStart);
            this.panelCheckMedia.Location = new System.Drawing.Point(2, 5);
            this.panelCheckMedia.Name = "panelCheckMedia";
            this.panelCheckMedia.Size = new System.Drawing.Size(323, 130);
            this.panelCheckMedia.TabIndex = 33;
            // 
            // labelMissingVideos
            // 
            this.labelMissingVideos.AutoSize = true;
            this.labelMissingVideos.BackColor = System.Drawing.Color.Transparent;
            this.labelMissingVideos.ForeColor = System.Drawing.Color.MidnightBlue;
            this.labelMissingVideos.Location = new System.Drawing.Point(196, 9);
            this.labelMissingVideos.Name = "labelMissingVideos";
            this.labelMissingVideos.Size = new System.Drawing.Size(80, 13);
            this.labelMissingVideos.TabIndex = 39;
            this.labelMissingVideos.Text = "Missing Videos:";
            // 
            // labelMissingVideosCount
            // 
            this.labelMissingVideosCount.AutoSize = true;
            this.labelMissingVideosCount.BackColor = System.Drawing.Color.Transparent;
            this.labelMissingVideosCount.ForeColor = System.Drawing.Color.Red;
            this.labelMissingVideosCount.Location = new System.Drawing.Point(278, 9);
            this.labelMissingVideosCount.Name = "labelMissingVideosCount";
            this.labelMissingVideosCount.Size = new System.Drawing.Size(13, 13);
            this.labelMissingVideosCount.TabIndex = 40;
            this.labelMissingVideosCount.Text = "0";
            // 
            // labelVideos
            // 
            this.labelVideos.AutoSize = true;
            this.labelVideos.BackColor = System.Drawing.Color.Transparent;
            this.labelVideos.ForeColor = System.Drawing.Color.MidnightBlue;
            this.labelVideos.Location = new System.Drawing.Point(7, 25);
            this.labelVideos.Name = "labelVideos";
            this.labelVideos.Size = new System.Drawing.Size(69, 13);
            this.labelVideos.TabIndex = 37;
            this.labelVideos.Text = "Total Videos:";
            // 
            // labelTotalVideosCount
            // 
            this.labelTotalVideosCount.AutoSize = true;
            this.labelTotalVideosCount.BackColor = System.Drawing.Color.Transparent;
            this.labelTotalVideosCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.labelTotalVideosCount.Location = new System.Drawing.Point(84, 25);
            this.labelTotalVideosCount.Name = "labelTotalVideosCount";
            this.labelTotalVideosCount.Size = new System.Drawing.Size(13, 13);
            this.labelTotalVideosCount.TabIndex = 38;
            this.labelTotalVideosCount.Text = "0";
            // 
            // labelImages
            // 
            this.labelImages.AutoSize = true;
            this.labelImages.BackColor = System.Drawing.Color.Transparent;
            this.labelImages.ForeColor = System.Drawing.Color.MidnightBlue;
            this.labelImages.Location = new System.Drawing.Point(7, 9);
            this.labelImages.Name = "labelImages";
            this.labelImages.Size = new System.Drawing.Size(71, 13);
            this.labelImages.TabIndex = 35;
            this.labelImages.Text = "Total Images:";
            // 
            // labelTotalImagesCount
            // 
            this.labelTotalImagesCount.AutoSize = true;
            this.labelTotalImagesCount.BackColor = System.Drawing.Color.Transparent;
            this.labelTotalImagesCount.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            this.labelTotalImagesCount.Location = new System.Drawing.Point(84, 9);
            this.labelTotalImagesCount.Name = "labelTotalImagesCount";
            this.labelTotalImagesCount.Size = new System.Drawing.Size(13, 13);
            this.labelTotalImagesCount.TabIndex = 36;
            this.labelTotalImagesCount.Text = "0";
            // 
            // buttonStop
            // 
            this.buttonStop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.buttonStop.Enabled = false;
            this.buttonStop.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.buttonStop.ForeColor = System.Drawing.SystemColors.ControlText;
            this.buttonStop.Location = new System.Drawing.Point(84, 51);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 34;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = false;
            this.buttonStop.Click += new System.EventHandler(this.button_Stop_Click);
            // 
            // listBoxLog
            // 
            this.listBoxLog.FormattingEnabled = true;
            this.listBoxLog.Location = new System.Drawing.Point(2, 254);
            this.listBoxLog.Name = "listBoxLog";
            this.listBoxLog.Size = new System.Drawing.Size(323, 108);
            this.listBoxLog.TabIndex = 34;
            // 
            // MediaCheckForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GamelistManager.Properties.Resources.background2;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(327, 365);
            this.Controls.Add(this.listBoxLog);
            this.Controls.Add(this.panelCheckMedia);
            this.Controls.Add(this.panelManageMedia);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MediaCheckForm";
            this.Text = "Media Check";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MediaCheckForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MediaCheckForm_FormClosed);
            this.Load += new System.EventHandler(this.MediaCheckForm_Load);
            this.panelManageMedia.ResumeLayout(false);
            this.panelManageMedia.PerformLayout();
            this.panelCheckMedia.ResumeLayout(false);
            this.panelCheckMedia.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button buttonCleanup;
        private System.Windows.Forms.RadioButton radioButtonRename;
        private System.Windows.Forms.RadioButton radioButtonDelete;
        private System.Windows.Forms.RadioButton radioButtonExportCSV;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Panel panelManageMedia;
        private System.Windows.Forms.Label labelInstruction;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.Label labelManageBadMedia;
        private System.Windows.Forms.Label labelMissingImages;
        private System.Windows.Forms.Label labelMissingImageCount;
        private System.Windows.Forms.Label labelCorruptImageCount;
        private System.Windows.Forms.Label labelCorruptImages;
        private System.Windows.Forms.Label labelSingleColorImageCount;
        private System.Windows.Forms.Label labelSingleColorImages;
        private System.Windows.Forms.Panel panelCheckMedia;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.ListBox listBoxLog;
        private System.Windows.Forms.Label labelVideos;
        private System.Windows.Forms.Label labelTotalVideosCount;
        private System.Windows.Forms.Label labelImages;
        private System.Windows.Forms.Label labelTotalImagesCount;
        private System.Windows.Forms.Label labelMissingVideos;
        private System.Windows.Forms.Label labelMissingVideosCount;
    }
}