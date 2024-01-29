namespace GamelistManager.control
{
    partial class ScreenScraperSetup
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.textbox_ScreenScraperPassword = new System.Windows.Forms.TextBox();
            this.textbox_ScreenScraperName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox_ImageSource = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBox_BoxSource = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.comboBox_LogoSource = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(81, 91);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(53, 17);
            this.checkBox1.TabIndex = 21;
            this.checkBox1.Text = "Show";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(3, 8);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(145, 13);
            this.label5.TabIndex = 20;
            this.label5.Text = "Saved In Credential Manager";
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.button2.Location = new System.Drawing.Point(81, 211);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(64, 23);
            this.button2.TabIndex = 19;
            this.button2.Text = "Exit";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.button1.Location = new System.Drawing.Point(6, 211);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(64, 23);
            this.button1.TabIndex = 18;
            this.button1.Text = "Save";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textbox_ScreenScraperPassword
            // 
            this.textbox_ScreenScraperPassword.Location = new System.Drawing.Point(6, 89);
            this.textbox_ScreenScraperPassword.Name = "textbox_ScreenScraperPassword";
            this.textbox_ScreenScraperPassword.Size = new System.Drawing.Size(72, 20);
            this.textbox_ScreenScraperPassword.TabIndex = 17;
            this.textbox_ScreenScraperPassword.UseSystemPasswordChar = true;
            this.textbox_ScreenScraperPassword.TextChanged += new System.EventHandler(this.ScreenScraperPassword_TextChanged);
            // 
            // textbox_ScreenScraperName
            // 
            this.textbox_ScreenScraperName.Location = new System.Drawing.Point(6, 50);
            this.textbox_ScreenScraperName.Name = "textbox_ScreenScraperName";
            this.textbox_ScreenScraperName.Size = new System.Drawing.Size(72, 20);
            this.textbox_ScreenScraperName.TabIndex = 16;
            this.textbox_ScreenScraperName.TextChanged += new System.EventHandler(this.ScreenScraperID_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(130, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "ScreenScraper Password:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 34);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(131, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "ScreenScraper UserName";
            // 
            // comboBox_ImageSource
            // 
            this.comboBox_ImageSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_ImageSource.FormattingEnabled = true;
            this.comboBox_ImageSource.Items.AddRange(new object[] {
            "Screenshot",
            "Title Screenshot",
            "Mix V1",
            "Mix V2",
            "Box 2D",
            "Box 3D",
            "Fan Art"});
            this.comboBox_ImageSource.Location = new System.Drawing.Point(81, 125);
            this.comboBox_ImageSource.Name = "comboBox_ImageSource";
            this.comboBox_ImageSource.Size = new System.Drawing.Size(82, 21);
            this.comboBox_ImageSource.TabIndex = 22;
            this.comboBox_ImageSource.SelectedIndexChanged += new System.EventHandler(this.comboBox_ImageSource_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 128);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 23;
            this.label1.Text = "Image Source";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 155);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 13);
            this.label4.TabIndex = 25;
            this.label4.Text = "Box Source";
            // 
            // comboBox_BoxSource
            // 
            this.comboBox_BoxSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_BoxSource.FormattingEnabled = true;
            this.comboBox_BoxSource.Items.AddRange(new object[] {
            "Box 2D",
            "Box 3D"});
            this.comboBox_BoxSource.Location = new System.Drawing.Point(81, 152);
            this.comboBox_BoxSource.Name = "comboBox_BoxSource";
            this.comboBox_BoxSource.Size = new System.Drawing.Size(82, 21);
            this.comboBox_BoxSource.TabIndex = 24;
            this.comboBox_BoxSource.SelectedIndexChanged += new System.EventHandler(this.comboBox_BoxSource_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 182);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(68, 13);
            this.label6.TabIndex = 27;
            this.label6.Text = "Logo Source";
            // 
            // comboBox_LogoSource
            // 
            this.comboBox_LogoSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_LogoSource.FormattingEnabled = true;
            this.comboBox_LogoSource.Items.AddRange(new object[] {
            "Wheel",
            "Marquee"});
            this.comboBox_LogoSource.Location = new System.Drawing.Point(81, 179);
            this.comboBox_LogoSource.Name = "comboBox_LogoSource";
            this.comboBox_LogoSource.Size = new System.Drawing.Size(82, 21);
            this.comboBox_LogoSource.TabIndex = 26;
            this.comboBox_LogoSource.SelectedIndexChanged += new System.EventHandler(this.comboBox_LogoSource_SelectedIndexChanged);
            // 
            // ScreenScraperSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label6);
            this.Controls.Add(this.comboBox_LogoSource);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.comboBox_BoxSource);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox_ImageSource);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textbox_ScreenScraperPassword);
            this.Controls.Add(this.textbox_ScreenScraperName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Name = "ScreenScraperSetup";
            this.Size = new System.Drawing.Size(235, 264);
            this.Load += new System.EventHandler(this.ScreenScraperSetup_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textbox_ScreenScraperPassword;
        private System.Windows.Forms.TextBox textbox_ScreenScraperName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_ImageSource;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBox_BoxSource;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox comboBox_LogoSource;
    }
}
