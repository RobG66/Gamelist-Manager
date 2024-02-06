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
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.button_Start = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.radioButton_Videos = new System.Windows.Forms.RadioButton();
            this.radioButton_Images = new System.Windows.Forms.RadioButton();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label_progress = new System.Windows.Forms.Label();
            this.label_Missing = new System.Windows.Forms.Label();
            this.label_MissingCount = new System.Windows.Forms.Label();
            this.label_CorruptCount = new System.Windows.Forms.Label();
            this.label_Corrupt = new System.Windows.Forms.Label();
            this.label_SingleColorCount = new System.Windows.Forms.Label();
            this.label_SingleColor = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.button_Stop = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // button2
            // 
            this.button2.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.button2.Location = new System.Drawing.Point(96, 103);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 10;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.button1.Location = new System.Drawing.Point(3, 103);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 9;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton3.Location = new System.Drawing.Point(3, 78);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(248, 17);
            this.radioButton3.TabIndex = 8;
            this.radioButton3.Text = "Rename with a \'bad-\' prefix and clear paths";
            this.radioButton3.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton2.Location = new System.Drawing.Point(3, 54);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(140, 17);
            this.radioButton2.TabIndex = 7;
            this.radioButton2.Text = "Delete and clear paths";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButton1.Location = new System.Drawing.Point(3, 30);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(113, 17);
            this.radioButton1.TabIndex = 6;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Export list to CSV";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // button_Start
            // 
            this.button_Start.BackColor = System.Drawing.Color.Lime;
            this.button_Start.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.button_Start.Location = new System.Drawing.Point(12, 86);
            this.button_Start.Name = "button_Start";
            this.button_Start.Size = new System.Drawing.Size(75, 23);
            this.button_Start.TabIndex = 13;
            this.button_Start.Text = "Start";
            this.button_Start.UseVisualStyleBackColor = false;
            this.button_Start.Click += new System.EventHandler(this.button_Start_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 128);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(400, 23);
            this.progressBar1.TabIndex = 14;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.Controls.Add(this.radioButton_Videos);
            this.panel1.Controls.Add(this.radioButton_Images);
            this.panel1.Location = new System.Drawing.Point(12, 30);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(132, 25);
            this.panel1.TabIndex = 15;
            // 
            // radioButton_Videos
            // 
            this.radioButton_Videos.AutoSize = true;
            this.radioButton_Videos.Enabled = false;
            this.radioButton_Videos.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.radioButton_Videos.Location = new System.Drawing.Point(69, 6);
            this.radioButton_Videos.Name = "radioButton_Videos";
            this.radioButton_Videos.Size = new System.Drawing.Size(60, 17);
            this.radioButton_Videos.TabIndex = 1;
            this.radioButton_Videos.TabStop = true;
            this.radioButton_Videos.Text = "Videos";
            this.radioButton_Videos.UseVisualStyleBackColor = true;
            // 
            // radioButton_Images
            // 
            this.radioButton_Images.AutoSize = true;
            this.radioButton_Images.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.radioButton_Images.Location = new System.Drawing.Point(4, 6);
            this.radioButton_Images.Name = "radioButton_Images";
            this.radioButton_Images.Size = new System.Drawing.Size(61, 17);
            this.radioButton_Images.TabIndex = 0;
            this.radioButton_Images.TabStop = true;
            this.radioButton_Images.Text = "Images";
            this.radioButton_Images.UseVisualStyleBackColor = true;
            this.radioButton_Images.CheckedChanged += new System.EventHandler(this.radioButton_Images_CheckedChanged);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.Transparent;
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.radioButton1);
            this.panel2.Controls.Add(this.radioButton2);
            this.panel2.Controls.Add(this.radioButton3);
            this.panel2.Controls.Add(this.button1);
            this.panel2.Controls.Add(this.button2);
            this.panel2.Enabled = false;
            this.panel2.Location = new System.Drawing.Point(12, 159);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(400, 134);
            this.panel2.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label1.Location = new System.Drawing.Point(3, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 15);
            this.label1.TabIndex = 18;
            this.label1.Text = "Manage Bad Media";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.Location = new System.Drawing.Point(9, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(177, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "Click Start button to check media";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.label2.Location = new System.Drawing.Point(9, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Count";
            // 
            // label_progress
            // 
            this.label_progress.AutoSize = true;
            this.label_progress.BackColor = System.Drawing.Color.Transparent;
            this.label_progress.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.label_progress.Location = new System.Drawing.Point(13, 112);
            this.label_progress.Name = "label_progress";
            this.label_progress.Size = new System.Drawing.Size(22, 13);
            this.label_progress.TabIndex = 26;
            this.label_progress.Text = "0%";
            // 
            // label_Missing
            // 
            this.label_Missing.AutoSize = true;
            this.label_Missing.BackColor = System.Drawing.Color.Transparent;
            this.label_Missing.ForeColor = System.Drawing.Color.MidnightBlue;
            this.label_Missing.Location = new System.Drawing.Point(29, 18);
            this.label_Missing.Name = "label_Missing";
            this.label_Missing.Size = new System.Drawing.Size(45, 13);
            this.label_Missing.TabIndex = 27;
            this.label_Missing.Text = "Missing:";
            // 
            // label_MissingCount
            // 
            this.label_MissingCount.AutoSize = true;
            this.label_MissingCount.BackColor = System.Drawing.Color.Transparent;
            this.label_MissingCount.ForeColor = System.Drawing.Color.Red;
            this.label_MissingCount.Location = new System.Drawing.Point(77, 18);
            this.label_MissingCount.Name = "label_MissingCount";
            this.label_MissingCount.Size = new System.Drawing.Size(13, 13);
            this.label_MissingCount.TabIndex = 28;
            this.label_MissingCount.Text = "0";
            // 
            // label_CorruptCount
            // 
            this.label_CorruptCount.AutoSize = true;
            this.label_CorruptCount.BackColor = System.Drawing.Color.Transparent;
            this.label_CorruptCount.ForeColor = System.Drawing.Color.Red;
            this.label_CorruptCount.Location = new System.Drawing.Point(77, 44);
            this.label_CorruptCount.Name = "label_CorruptCount";
            this.label_CorruptCount.Size = new System.Drawing.Size(13, 13);
            this.label_CorruptCount.TabIndex = 30;
            this.label_CorruptCount.Text = "0";
            // 
            // label_Corrupt
            // 
            this.label_Corrupt.AutoSize = true;
            this.label_Corrupt.BackColor = System.Drawing.Color.Transparent;
            this.label_Corrupt.ForeColor = System.Drawing.Color.MidnightBlue;
            this.label_Corrupt.Location = new System.Drawing.Point(30, 44);
            this.label_Corrupt.Name = "label_Corrupt";
            this.label_Corrupt.Size = new System.Drawing.Size(44, 13);
            this.label_Corrupt.TabIndex = 29;
            this.label_Corrupt.Text = "Corrupt:";
            // 
            // label_SingleColorCount
            // 
            this.label_SingleColorCount.AutoSize = true;
            this.label_SingleColorCount.BackColor = System.Drawing.Color.Transparent;
            this.label_SingleColorCount.ForeColor = System.Drawing.Color.Red;
            this.label_SingleColorCount.Location = new System.Drawing.Point(77, 70);
            this.label_SingleColorCount.Name = "label_SingleColorCount";
            this.label_SingleColorCount.Size = new System.Drawing.Size(13, 13);
            this.label_SingleColorCount.TabIndex = 32;
            this.label_SingleColorCount.Text = "0";
            // 
            // label_SingleColor
            // 
            this.label_SingleColor.AutoSize = true;
            this.label_SingleColor.BackColor = System.Drawing.Color.Transparent;
            this.label_SingleColor.ForeColor = System.Drawing.Color.MidnightBlue;
            this.label_SingleColor.Location = new System.Drawing.Point(9, 70);
            this.label_SingleColor.Name = "label_SingleColor";
            this.label_SingleColor.Size = new System.Drawing.Size(63, 13);
            this.label_SingleColor.TabIndex = 31;
            this.label_SingleColor.Text = "Single Color";
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.Transparent;
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.label_Missing);
            this.panel3.Controls.Add(this.label_SingleColorCount);
            this.panel3.Controls.Add(this.label_MissingCount);
            this.panel3.Controls.Add(this.label_SingleColor);
            this.panel3.Controls.Add(this.label_Corrupt);
            this.panel3.Controls.Add(this.label_CorruptCount);
            this.panel3.Location = new System.Drawing.Point(288, 12);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(124, 100);
            this.panel3.TabIndex = 33;
            // 
            // button_Stop
            // 
            this.button_Stop.BackColor = System.Drawing.Color.Crimson;
            this.button_Stop.Enabled = false;
            this.button_Stop.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.button_Stop.ForeColor = System.Drawing.Color.White;
            this.button_Stop.Location = new System.Drawing.Point(93, 86);
            this.button_Stop.Name = "button_Stop";
            this.button_Stop.Size = new System.Drawing.Size(75, 23);
            this.button_Stop.TabIndex = 34;
            this.button_Stop.Text = "Stop";
            this.button_Stop.UseVisualStyleBackColor = false;
            this.button_Stop.Click += new System.EventHandler(this.button_Stop_Click);
            // 
            // MediaCheckForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GamelistManager.Properties.Resources.background2;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(428, 298);
            this.Controls.Add(this.button_Stop);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.label_progress);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.button_Start);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MediaCheckForm";
            this.Text = "Image Handler";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MediaCheckForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MediaCheckForm_FormClosed);
            this.Load += new System.EventHandler(this.MediaCheckForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.Button button_Start;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton radioButton_Videos;
        private System.Windows.Forms.RadioButton radioButton_Images;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label_progress;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label_Missing;
        private System.Windows.Forms.Label label_MissingCount;
        private System.Windows.Forms.Label label_CorruptCount;
        private System.Windows.Forms.Label label_Corrupt;
        private System.Windows.Forms.Label label_SingleColorCount;
        private System.Windows.Forms.Label label_SingleColor;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button button_Stop;
    }
}