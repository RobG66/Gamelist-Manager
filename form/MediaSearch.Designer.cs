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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.textboxSource = new System.Windows.Forms.TextBox();
            this.buttonChooseSource = new System.Windows.Forms.Button();
            this.checkBoxSame = new System.Windows.Forms.CheckBox();
            this.textboxDestination = new System.Windows.Forms.TextBox();
            this.buttonDestination = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.buttonSearch = new System.Windows.Forms.Button();
            this.checkBoxExisting = new System.Windows.Forms.CheckBox();
            this.checkBoxSearchDefault = new System.Windows.Forms.CheckBox();
            this.romname = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.foundimage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // textboxSource
            // 
            this.textboxSource.Location = new System.Drawing.Point(93, 34);
            this.textboxSource.Name = "textboxSource";
            this.textboxSource.Size = new System.Drawing.Size(232, 20);
            this.textboxSource.TabIndex = 1;
            // 
            // buttonChooseSource
            // 
            this.buttonChooseSource.Location = new System.Drawing.Point(12, 32);
            this.buttonChooseSource.Name = "buttonChooseSource";
            this.buttonChooseSource.Size = new System.Drawing.Size(75, 23);
            this.buttonChooseSource.TabIndex = 2;
            this.buttonChooseSource.Text = "Source";
            this.buttonChooseSource.UseVisualStyleBackColor = true;
            this.buttonChooseSource.Click += new System.EventHandler(this.buttonChooseSource_Click);
            // 
            // checkBoxSame
            // 
            this.checkBoxSame.AutoSize = true;
            this.checkBoxSame.Checked = true;
            this.checkBoxSame.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSame.Location = new System.Drawing.Point(12, 90);
            this.checkBoxSame.Name = "checkBoxSame";
            this.checkBoxSame.Size = new System.Drawing.Size(105, 17);
            this.checkBoxSame.TabIndex = 3;
            this.checkBoxSame.Text = "Do not copy files";
            this.checkBoxSame.UseVisualStyleBackColor = true;
            this.checkBoxSame.CheckedChanged += new System.EventHandler(this.checkBoxSame_CheckedChanged);
            // 
            // textboxDestination
            // 
            this.textboxDestination.Enabled = false;
            this.textboxDestination.Location = new System.Drawing.Point(93, 63);
            this.textboxDestination.Name = "textboxDestination";
            this.textboxDestination.Size = new System.Drawing.Size(232, 20);
            this.textboxDestination.TabIndex = 4;
            // 
            // buttonDestination
            // 
            this.buttonDestination.Enabled = false;
            this.buttonDestination.Location = new System.Drawing.Point(12, 61);
            this.buttonDestination.Name = "buttonDestination";
            this.buttonDestination.Size = new System.Drawing.Size(75, 23);
            this.buttonDestination.TabIndex = 5;
            this.buttonDestination.Text = "Destination";
            this.buttonDestination.UseVisualStyleBackColor = true;
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
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.romname,
            this.foundimage});
            this.dataGridView1.Location = new System.Drawing.Point(10, 123);
            this.dataGridView1.Margin = new System.Windows.Forms.Padding(1);
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
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(548, 168);
            this.dataGridView1.TabIndex = 6;
            // 
            // buttonSearch
            // 
            this.buttonSearch.Location = new System.Drawing.Point(132, 86);
            this.buttonSearch.Name = "buttonSearch";
            this.buttonSearch.Size = new System.Drawing.Size(75, 23);
            this.buttonSearch.TabIndex = 7;
            this.buttonSearch.Text = "Search";
            this.buttonSearch.UseVisualStyleBackColor = true;
            this.buttonSearch.Click += new System.EventHandler(this.buttonSearch_Click);
            // 
            // checkBoxExisting
            // 
            this.checkBoxExisting.AutoSize = true;
            this.checkBoxExisting.Location = new System.Drawing.Point(13, 9);
            this.checkBoxExisting.Name = "checkBoxExisting";
            this.checkBoxExisting.Size = new System.Drawing.Size(156, 17);
            this.checkBoxExisting.TabIndex = 8;
            this.checkBoxExisting.Text = "Reassociate Existing Media";
            this.checkBoxExisting.UseVisualStyleBackColor = true;
            this.checkBoxExisting.CheckedChanged += new System.EventHandler(this.checkBoxExisting_CheckedChanged);
            // 
            // checkBoxSearchDefault
            // 
            this.checkBoxSearchDefault.AutoSize = true;
            this.checkBoxSearchDefault.Enabled = false;
            this.checkBoxSearchDefault.Location = new System.Drawing.Point(175, 9);
            this.checkBoxSearchDefault.Name = "checkBoxSearchDefault";
            this.checkBoxSearchDefault.Size = new System.Drawing.Size(127, 17);
            this.checkBoxSearchDefault.TabIndex = 9;
            this.checkBoxSearchDefault.Text = "Search Default Paths";
            this.checkBoxSearchDefault.UseVisualStyleBackColor = true;
            // 
            // romname
            // 
            this.romname.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.romname.FillWeight = 50F;
            this.romname.HeaderText = "Rom Name";
            this.romname.Name = "romname";
            this.romname.ReadOnly = true;
            // 
            // foundimage
            // 
            this.foundimage.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.foundimage.HeaderText = "Matched Image";
            this.foundimage.Name = "foundimage";
            this.foundimage.ReadOnly = true;
            // 
            // MediaSearch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(568, 339);
            this.Controls.Add(this.checkBoxSearchDefault);
            this.Controls.Add(this.checkBoxExisting);
            this.Controls.Add(this.buttonSearch);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.buttonDestination);
            this.Controls.Add(this.textboxDestination);
            this.Controls.Add(this.checkBoxSame);
            this.Controls.Add(this.buttonChooseSource);
            this.Controls.Add(this.textboxSource);
            this.Name = "MediaSearch";
            this.Text = "Add Media";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox textboxSource;
        private System.Windows.Forms.Button buttonChooseSource;
        private System.Windows.Forms.CheckBox checkBoxSame;
        private System.Windows.Forms.TextBox textboxDestination;
        private System.Windows.Forms.Button buttonDestination;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button buttonSearch;
        private System.Windows.Forms.CheckBox checkBoxExisting;
        private System.Windows.Forms.CheckBox checkBoxSearchDefault;
        private System.Windows.Forms.DataGridViewTextBoxColumn romname;
        private System.Windows.Forms.DataGridViewTextBoxColumn foundimage;
    }
}