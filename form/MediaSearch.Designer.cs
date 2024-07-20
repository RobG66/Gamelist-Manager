namespace GamelistManager.form
{
    partial class MediaSearch
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle17 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle18 = new System.Windows.Forms.DataGridViewCellStyle();
            this.textboxSource = new System.Windows.Forms.TextBox();
            this.buttonChooseSource = new System.Windows.Forms.Button();
            this.checkBoxSame = new System.Windows.Forms.CheckBox();
            this.textboxDestination = new System.Windows.Forms.TextBox();
            this.buttonDestination = new System.Windows.Forms.Button();
            this.buttonSearch = new System.Windows.Forms.Button();
            this.checkBoxExisting = new System.Windows.Forms.CheckBox();
            this.checkBoxSearchDefault = new System.Windows.Forms.CheckBox();
            this.comboBoxMediaTypes = new System.Windows.Forms.ComboBox();
            this.dataGridViewImages = new System.Windows.Forms.DataGridView();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonClear = new System.Windows.Forms.Button();
            this.checkBoxDoNotOverwrite = new System.Windows.Forms.CheckBox();
            this.checkBoxDefaultPath = new System.Windows.Forms.CheckBox();
            this.panelbigger = new System.Windows.Forms.Panel();
            this.labelCount = new System.Windows.Forms.Label();
            this.panelsmaller = new System.Windows.Forms.Panel();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.romname = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.mediatype = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.foundimage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewImages)).BeginInit();
            this.panelbigger.SuspendLayout();
            this.panelsmaller.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // textboxSource
            // 
            this.textboxSource.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textboxSource.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.textboxSource.Location = new System.Drawing.Point(83, 3);
            this.textboxSource.Name = "textboxSource";
            this.textboxSource.Size = new System.Drawing.Size(326, 23);
            this.textboxSource.TabIndex = 1;
            this.textboxSource.Text = "D:\\Launchbox\\Images\\Sega Dreamcast\\Screenshot - Gameplay";
            // 
            // buttonChooseSource
            // 
            this.buttonChooseSource.BackColor = System.Drawing.Color.PaleTurquoise;
            this.buttonChooseSource.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.buttonChooseSource.Location = new System.Drawing.Point(3, 3);
            this.buttonChooseSource.Name = "buttonChooseSource";
            this.buttonChooseSource.Size = new System.Drawing.Size(75, 23);
            this.buttonChooseSource.TabIndex = 2;
            this.buttonChooseSource.Text = "Source";
            this.buttonChooseSource.UseVisualStyleBackColor = false;
            this.buttonChooseSource.Click += new System.EventHandler(this.buttonChooseSource_Click);
            // 
            // checkBoxSame
            // 
            this.checkBoxSame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxSame.AutoSize = true;
            this.checkBoxSame.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxSame.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.checkBoxSame.Location = new System.Drawing.Point(437, 55);
            this.checkBoxSame.Name = "checkBoxSame";
            this.checkBoxSame.Size = new System.Drawing.Size(115, 19);
            this.checkBoxSame.TabIndex = 3;
            this.checkBoxSame.Text = "Do not copy files";
            this.checkBoxSame.UseVisualStyleBackColor = false;
            this.checkBoxSame.CheckedChanged += new System.EventHandler(this.checkBoxSame_CheckedChanged);
            // 
            // textboxDestination
            // 
            this.textboxDestination.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textboxDestination.Enabled = false;
            this.textboxDestination.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.textboxDestination.Location = new System.Drawing.Point(83, 31);
            this.textboxDestination.Name = "textboxDestination";
            this.textboxDestination.Size = new System.Drawing.Size(326, 23);
            this.textboxDestination.TabIndex = 4;
            // 
            // buttonDestination
            // 
            this.buttonDestination.BackColor = System.Drawing.Color.PaleTurquoise;
            this.buttonDestination.Enabled = false;
            this.buttonDestination.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.buttonDestination.Location = new System.Drawing.Point(3, 31);
            this.buttonDestination.Name = "buttonDestination";
            this.buttonDestination.Size = new System.Drawing.Size(75, 23);
            this.buttonDestination.TabIndex = 5;
            this.buttonDestination.Text = "Destination";
            this.buttonDestination.UseVisualStyleBackColor = false;
            // 
            // buttonSearch
            // 
            this.buttonSearch.BackColor = System.Drawing.Color.Orange;
            this.buttonSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.buttonSearch.Location = new System.Drawing.Point(9, 105);
            this.buttonSearch.Name = "buttonSearch";
            this.buttonSearch.Size = new System.Drawing.Size(75, 23);
            this.buttonSearch.TabIndex = 7;
            this.buttonSearch.Text = "Search";
            this.buttonSearch.UseVisualStyleBackColor = false;
            this.buttonSearch.Click += new System.EventHandler(this.buttonSearch_Click);
            // 
            // checkBoxExisting
            // 
            this.checkBoxExisting.AutoSize = true;
            this.checkBoxExisting.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxExisting.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.checkBoxExisting.Location = new System.Drawing.Point(9, 134);
            this.checkBoxExisting.Name = "checkBoxExisting";
            this.checkBoxExisting.Size = new System.Drawing.Size(167, 19);
            this.checkBoxExisting.TabIndex = 8;
            this.checkBoxExisting.Text = "Reassociate Existing Media";
            this.checkBoxExisting.UseVisualStyleBackColor = false;
            this.checkBoxExisting.CheckedChanged += new System.EventHandler(this.checkBoxExisting_CheckedChanged);
            // 
            // checkBoxSearchDefault
            // 
            this.checkBoxSearchDefault.AutoSize = true;
            this.checkBoxSearchDefault.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxSearchDefault.Enabled = false;
            this.checkBoxSearchDefault.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.checkBoxSearchDefault.Location = new System.Drawing.Point(182, 134);
            this.checkBoxSearchDefault.Name = "checkBoxSearchDefault";
            this.checkBoxSearchDefault.Size = new System.Drawing.Size(134, 19);
            this.checkBoxSearchDefault.TabIndex = 9;
            this.checkBoxSearchDefault.Text = "Search Default Paths";
            this.checkBoxSearchDefault.UseVisualStyleBackColor = false;
            // 
            // comboBoxMediaTypes
            // 
            this.comboBoxMediaTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxMediaTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMediaTypes.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.comboBoxMediaTypes.FormattingEnabled = true;
            this.comboBoxMediaTypes.Items.AddRange(new object[] {
            "Image",
            "Video",
            "Thumbnail",
            "Marquee",
            "BoxBack",
            "Cartridge",
            "Map",
            "Manual",
            "Fan Art"});
            this.comboBoxMediaTypes.Location = new System.Drawing.Point(425, 3);
            this.comboBoxMediaTypes.Name = "comboBoxMediaTypes";
            this.comboBoxMediaTypes.Size = new System.Drawing.Size(121, 23);
            this.comboBoxMediaTypes.TabIndex = 10;
            this.comboBoxMediaTypes.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // dataGridViewImages
            // 
            this.dataGridViewImages.AllowUserToAddRows = false;
            this.dataGridViewImages.AllowUserToDeleteRows = false;
            this.dataGridViewImages.AllowUserToResizeRows = false;
            dataGridViewCellStyle17.BackColor = System.Drawing.Color.WhiteSmoke;
            this.dataGridViewImages.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle17;
            this.dataGridViewImages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewImages.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;
            this.dataGridViewImages.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewImages.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.romname,
            this.mediatype,
            this.foundimage});
            this.dataGridViewImages.Location = new System.Drawing.Point(3, 168);
            this.dataGridViewImages.Margin = new System.Windows.Forms.Padding(1);
            this.dataGridViewImages.Name = "dataGridViewImages";
            dataGridViewCellStyle18.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle18.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle18.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle18.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle18.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle18.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewImages.RowHeadersDefaultCellStyle = dataGridViewCellStyle18;
            this.dataGridViewImages.RowHeadersVisible = false;
            this.dataGridViewImages.RowHeadersWidth = 123;
            this.dataGridViewImages.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.dataGridViewImages.RowTemplate.Height = 20;
            this.dataGridViewImages.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewImages.Size = new System.Drawing.Size(562, 167);
            this.dataGridViewImages.TabIndex = 6;
            // 
            // buttonSave
            // 
            this.buttonSave.BackColor = System.Drawing.Color.Lime;
            this.buttonSave.Enabled = false;
            this.buttonSave.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.buttonSave.Location = new System.Drawing.Point(90, 105);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 23);
            this.buttonSave.TabIndex = 11;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = false;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonClear
            // 
            this.buttonClear.BackColor = System.Drawing.Color.Yellow;
            this.buttonClear.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.buttonClear.Location = new System.Drawing.Point(171, 105);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(75, 23);
            this.buttonClear.TabIndex = 12;
            this.buttonClear.Text = "Clear";
            this.buttonClear.UseVisualStyleBackColor = false;
            this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
            // 
            // checkBoxDoNotOverwrite
            // 
            this.checkBoxDoNotOverwrite.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxDoNotOverwrite.AutoSize = true;
            this.checkBoxDoNotOverwrite.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.checkBoxDoNotOverwrite.Location = new System.Drawing.Point(437, 78);
            this.checkBoxDoNotOverwrite.Name = "checkBoxDoNotOverwrite";
            this.checkBoxDoNotOverwrite.Size = new System.Drawing.Size(118, 19);
            this.checkBoxDoNotOverwrite.TabIndex = 13;
            this.checkBoxDoNotOverwrite.Text = "Do Not Overwrite";
            this.checkBoxDoNotOverwrite.UseVisualStyleBackColor = true;
            // 
            // checkBoxDefaultPath
            // 
            this.checkBoxDefaultPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxDefaultPath.AutoSize = true;
            this.checkBoxDefaultPath.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.checkBoxDefaultPath.Location = new System.Drawing.Point(437, 34);
            this.checkBoxDefaultPath.Name = "checkBoxDefaultPath";
            this.checkBoxDefaultPath.Size = new System.Drawing.Size(91, 19);
            this.checkBoxDefaultPath.TabIndex = 14;
            this.checkBoxDefaultPath.Text = "Default Path";
            this.checkBoxDefaultPath.UseVisualStyleBackColor = true;
            this.checkBoxDefaultPath.CheckedChanged += new System.EventHandler(this.checkBoxDefaultPath_CheckedChanged);
            // 
            // panelbigger
            // 
            this.panelbigger.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelbigger.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelbigger.Controls.Add(this.label1);
            this.panelbigger.Controls.Add(this.progressBar1);
            this.panelbigger.Controls.Add(this.panelsmaller);
            this.panelbigger.Controls.Add(this.labelCount);
            this.panelbigger.Controls.Add(this.checkBoxExisting);
            this.panelbigger.Controls.Add(this.buttonClear);
            this.panelbigger.Controls.Add(this.buttonSave);
            this.panelbigger.Controls.Add(this.checkBoxSearchDefault);
            this.panelbigger.Controls.Add(this.buttonSearch);
            this.panelbigger.Location = new System.Drawing.Point(3, 3);
            this.panelbigger.Name = "panelbigger";
            this.panelbigger.Size = new System.Drawing.Size(562, 161);
            this.panelbigger.TabIndex = 15;
            // 
            // labelCount
            // 
            this.labelCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCount.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.labelCount.Location = new System.Drawing.Point(398, 135);
            this.labelCount.Name = "labelCount";
            this.labelCount.Size = new System.Drawing.Size(159, 17);
            this.labelCount.TabIndex = 15;
            this.labelCount.Text = "label1";
            this.labelCount.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.labelCount.Visible = false;
            // 
            // panelsmaller
            // 
            this.panelsmaller.Controls.Add(this.label2);
            this.panelsmaller.Controls.Add(this.pictureBox1);
            this.panelsmaller.Controls.Add(this.buttonChooseSource);
            this.panelsmaller.Controls.Add(this.buttonDestination);
            this.panelsmaller.Controls.Add(this.checkBoxDoNotOverwrite);
            this.panelsmaller.Controls.Add(this.checkBoxDefaultPath);
            this.panelsmaller.Controls.Add(this.comboBoxMediaTypes);
            this.panelsmaller.Controls.Add(this.textboxSource);
            this.panelsmaller.Controls.Add(this.textboxDestination);
            this.panelsmaller.Controls.Add(this.checkBoxSame);
            this.panelsmaller.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelsmaller.Location = new System.Drawing.Point(0, 0);
            this.panelsmaller.Name = "panelsmaller";
            this.panelsmaller.Size = new System.Drawing.Size(560, 102);
            this.panelsmaller.TabIndex = 16;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Enabled = false;
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(158, 26);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Enabled = false;
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(180, 22);
            this.toolStripMenuItem1.Text = "Remove Item(s)";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // romname
            // 
            this.romname.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.romname.FillWeight = 50F;
            this.romname.HeaderText = "Rom Name";
            this.romname.Name = "romname";
            this.romname.ReadOnly = true;
            // 
            // mediatype
            // 
            this.mediatype.FillWeight = 25F;
            this.mediatype.HeaderText = "Media Type";
            this.mediatype.Name = "mediatype";
            this.mediatype.ReadOnly = true;
            // 
            // foundimage
            // 
            this.foundimage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.foundimage.HeaderText = "Matched Image";
            this.foundimage.Name = "foundimage";
            this.foundimage.ReadOnly = true;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(261, 105);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(100, 23);
            this.progressBar1.TabIndex = 17;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(367, 110);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 13);
            this.label1.TabIndex = 18;
            this.label1.Text = "0%";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(9, 58);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(190, 42);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 15;
            this.pictureBox1.TabStop = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label2.Location = new System.Drawing.Point(415, 34);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 15);
            this.label2.TabIndex = 16;
            this.label2.Text = "<-";
            // 
            // MediaSearch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(568, 339);
            this.Controls.Add(this.panelbigger);
            this.Controls.Add(this.dataGridViewImages);
            this.Name = "MediaSearch";
            this.Text = "Add Media";
            this.Load += new System.EventHandler(this.MediaSearch_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewImages)).EndInit();
            this.panelbigger.ResumeLayout(false);
            this.panelbigger.PerformLayout();
            this.panelsmaller.ResumeLayout(false);
            this.panelsmaller.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TextBox textboxSource;
        private System.Windows.Forms.Button buttonChooseSource;
        private System.Windows.Forms.CheckBox checkBoxSame;
        private System.Windows.Forms.TextBox textboxDestination;
        private System.Windows.Forms.Button buttonDestination;
        private System.Windows.Forms.Button buttonSearch;
        private System.Windows.Forms.CheckBox checkBoxExisting;
        private System.Windows.Forms.CheckBox checkBoxSearchDefault;
        private System.Windows.Forms.ComboBox comboBoxMediaTypes;
        private System.Windows.Forms.DataGridView dataGridViewImages;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.CheckBox checkBoxDoNotOverwrite;
        private System.Windows.Forms.CheckBox checkBoxDefaultPath;
        private System.Windows.Forms.Panel panelbigger;
        private System.Windows.Forms.Label labelCount;
        private System.Windows.Forms.Panel panelsmaller;
        private System.Windows.Forms.DataGridViewTextBoxColumn romname;
        private System.Windows.Forms.DataGridViewTextBoxColumn mediatype;
        private System.Windows.Forms.DataGridViewTextBoxColumn foundimage;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label2;
    }
}