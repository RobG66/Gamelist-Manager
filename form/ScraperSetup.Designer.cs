namespace GamelistManager.control
{
    partial class ScraperSetup
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
            this.checkboxShowPassword = new System.Windows.Forms.CheckBox();
            this.labelNote = new System.Windows.Forms.Label();
            this.buttonExit = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.textboxScraperPassword = new System.Windows.Forms.TextBox();
            this.textboxScraperName = new System.Windows.Forms.TextBox();
            this.labelPassword = new System.Windows.Forms.Label();
            this.labelName = new System.Windows.Forms.Label();
            this.labelRegion = new System.Windows.Forms.Label();
            this.comboBoxRegion = new System.Windows.Forms.ComboBox();
            this.labelLanguage = new System.Windows.Forms.Label();
            this.comboBoxLanguage = new System.Windows.Forms.ComboBox();
            this.checkBoxHideNonGame = new System.Windows.Forms.CheckBox();
            this.checkBoxNoZZZ = new System.Windows.Forms.CheckBox();
            this.checkBoxScrapeByGameID = new System.Windows.Forms.CheckBox();
            this.panelOptions = new System.Windows.Forms.Panel();
            this.panelOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkboxShowPassword
            // 
            this.checkboxShowPassword.AutoSize = true;
            this.checkboxShowPassword.BackColor = System.Drawing.Color.Transparent;
            this.checkboxShowPassword.Location = new System.Drawing.Point(121, 69);
            this.checkboxShowPassword.Name = "checkboxShowPassword";
            this.checkboxShowPassword.Size = new System.Drawing.Size(53, 17);
            this.checkboxShowPassword.TabIndex = 21;
            this.checkboxShowPassword.Text = "Show";
            this.checkboxShowPassword.UseVisualStyleBackColor = false;
            this.checkboxShowPassword.CheckedChanged += new System.EventHandler(this.CheckBox1_CheckedChanged);
            // 
            // labelNote
            // 
            this.labelNote.BackColor = System.Drawing.Color.Transparent;
            this.labelNote.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelNote.ForeColor = System.Drawing.Color.Red;
            this.labelNote.Location = new System.Drawing.Point(0, 235);
            this.labelNote.Name = "labelNote";
            this.labelNote.Size = new System.Drawing.Size(274, 21);
            this.labelNote.TabIndex = 20;
            this.labelNote.Text = "Credentials are saved in Credential Manager";
            this.labelNote.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonExit
            // 
            this.buttonExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.buttonExit.Location = new System.Drawing.Point(187, 41);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(64, 23);
            this.buttonExit.TabIndex = 19;
            this.buttonExit.Text = "Exit";
            this.buttonExit.UseVisualStyleBackColor = false;
            this.buttonExit.Click += new System.EventHandler(this.Button2_Click);
            // 
            // buttonSave
            // 
            this.buttonSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.buttonSave.Location = new System.Drawing.Point(187, 12);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(64, 23);
            this.buttonSave.TabIndex = 18;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = false;
            this.buttonSave.Click += new System.EventHandler(this.Button1_Click);
            // 
            // textboxScraperPassword
            // 
            this.textboxScraperPassword.Location = new System.Drawing.Point(9, 67);
            this.textboxScraperPassword.Name = "textboxScraperPassword";
            this.textboxScraperPassword.Size = new System.Drawing.Size(106, 20);
            this.textboxScraperPassword.TabIndex = 17;
            this.textboxScraperPassword.UseSystemPasswordChar = true;
            this.textboxScraperPassword.TextChanged += new System.EventHandler(this.ScraperPassword_TextChanged);
            // 
            // textboxScraperName
            // 
            this.textboxScraperName.Location = new System.Drawing.Point(9, 28);
            this.textboxScraperName.Name = "textboxScraperName";
            this.textboxScraperName.Size = new System.Drawing.Size(106, 20);
            this.textboxScraperName.TabIndex = 16;
            this.textboxScraperName.TextChanged += new System.EventHandler(this.ScreenScraperID_TextChanged);
            // 
            // labelPassword
            // 
            this.labelPassword.AutoSize = true;
            this.labelPassword.BackColor = System.Drawing.Color.Transparent;
            this.labelPassword.Location = new System.Drawing.Point(6, 51);
            this.labelPassword.Name = "labelPassword";
            this.labelPassword.Size = new System.Drawing.Size(130, 13);
            this.labelPassword.TabIndex = 14;
            this.labelPassword.Text = "ScreenScraper Password:";
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.BackColor = System.Drawing.Color.Transparent;
            this.labelName.Location = new System.Drawing.Point(6, 12);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(109, 13);
            this.labelName.TabIndex = 13;
            this.labelName.Text = "ScreenScraper Name";
            // 
            // labelRegion
            // 
            this.labelRegion.AutoSize = true;
            this.labelRegion.BackColor = System.Drawing.Color.Transparent;
            this.labelRegion.Location = new System.Drawing.Point(3, 12);
            this.labelRegion.Name = "labelRegion";
            this.labelRegion.Size = new System.Drawing.Size(87, 13);
            this.labelRegion.TabIndex = 29;
            this.labelRegion.Text = "Preferred Region";
            // 
            // comboBoxRegion
            // 
            this.comboBoxRegion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRegion.FormattingEnabled = true;
            this.comboBoxRegion.Location = new System.Drawing.Point(120, 9);
            this.comboBoxRegion.Name = "comboBoxRegion";
            this.comboBoxRegion.Size = new System.Drawing.Size(145, 21);
            this.comboBoxRegion.TabIndex = 28;
            // 
            // labelLanguage
            // 
            this.labelLanguage.AutoSize = true;
            this.labelLanguage.BackColor = System.Drawing.Color.Transparent;
            this.labelLanguage.Location = new System.Drawing.Point(3, 35);
            this.labelLanguage.Name = "labelLanguage";
            this.labelLanguage.Size = new System.Drawing.Size(101, 13);
            this.labelLanguage.TabIndex = 31;
            this.labelLanguage.Text = "Preferred Language";
            // 
            // comboBoxLanguage
            // 
            this.comboBoxLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLanguage.FormattingEnabled = true;
            this.comboBoxLanguage.Location = new System.Drawing.Point(120, 32);
            this.comboBoxLanguage.Name = "comboBoxLanguage";
            this.comboBoxLanguage.Size = new System.Drawing.Size(145, 21);
            this.comboBoxLanguage.TabIndex = 30;
            // 
            // checkBoxHideNonGame
            // 
            this.checkBoxHideNonGame.AutoSize = true;
            this.checkBoxHideNonGame.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxHideNonGame.Location = new System.Drawing.Point(6, 59);
            this.checkBoxHideNonGame.Name = "checkBoxHideNonGame";
            this.checkBoxHideNonGame.Size = new System.Drawing.Size(238, 17);
            this.checkBoxHideNonGame.TabIndex = 34;
            this.checkBoxHideNonGame.Text = "ScreenScraper: Automatically hide non-game";
            this.checkBoxHideNonGame.UseVisualStyleBackColor = false;
            this.checkBoxHideNonGame.CheckedChanged += new System.EventHandler(this.checkBoxHideNonGame_CheckedChanged);
            // 
            // checkBoxNoZZZ
            // 
            this.checkBoxNoZZZ.AutoSize = true;
            this.checkBoxNoZZZ.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxNoZZZ.Location = new System.Drawing.Point(6, 82);
            this.checkBoxNoZZZ.Name = "checkBoxNoZZZ";
            this.checkBoxNoZZZ.Size = new System.Drawing.Size(237, 17);
            this.checkBoxNoZZZ.TabIndex = 35;
            this.checkBoxNoZZZ.Text = "ScreenScraper: Remove ZZZ(not game) text";
            this.checkBoxNoZZZ.UseVisualStyleBackColor = false;
            this.checkBoxNoZZZ.CheckedChanged += new System.EventHandler(this.checkBoxNoZZZ_CheckedChanged);
            // 
            // checkBoxScrapeByGameID
            // 
            this.checkBoxScrapeByGameID.AutoSize = true;
            this.checkBoxScrapeByGameID.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxScrapeByGameID.Location = new System.Drawing.Point(6, 105);
            this.checkBoxScrapeByGameID.Name = "checkBoxScrapeByGameID";
            this.checkBoxScrapeByGameID.Size = new System.Drawing.Size(249, 17);
            this.checkBoxScrapeByGameID.TabIndex = 36;
            this.checkBoxScrapeByGameID.Text = "ScreenScraper: Scrape by Game ID if available";
            this.checkBoxScrapeByGameID.UseVisualStyleBackColor = false;
            this.checkBoxScrapeByGameID.CheckedChanged += new System.EventHandler(this.checkBoxScrapeByGameID_CheckedChanged);
            // 
            // panelOptions
            // 
            this.panelOptions.BackColor = System.Drawing.Color.Transparent;
            this.panelOptions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelOptions.Controls.Add(this.checkBoxScrapeByGameID);
            this.panelOptions.Controls.Add(this.checkBoxNoZZZ);
            this.panelOptions.Controls.Add(this.checkBoxHideNonGame);
            this.panelOptions.Controls.Add(this.labelLanguage);
            this.panelOptions.Controls.Add(this.comboBoxRegion);
            this.panelOptions.Controls.Add(this.comboBoxLanguage);
            this.panelOptions.Controls.Add(this.labelRegion);
            this.panelOptions.Enabled = false;
            this.panelOptions.Location = new System.Drawing.Point(2, 91);
            this.panelOptions.Name = "panelOptions";
            this.panelOptions.Size = new System.Drawing.Size(270, 139);
            this.panelOptions.TabIndex = 37;
            // 
            // ScraperSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GamelistManager.Properties.Resources.background6;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(274, 256);
            this.Controls.Add(this.panelOptions);
            this.Controls.Add(this.checkboxShowPassword);
            this.Controls.Add(this.labelNote);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.textboxScraperPassword);
            this.Controls.Add(this.textboxScraperName);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.labelName);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ScraperSetup";
            this.Text = "ScreenScraper Setup";
            this.Load += new System.EventHandler(this.ScraperSetup_Load);
            this.panelOptions.ResumeLayout(false);
            this.panelOptions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkboxShowPassword;
        private System.Windows.Forms.Label labelNote;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.TextBox textboxScraperPassword;
        private System.Windows.Forms.TextBox textboxScraperName;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.Label labelRegion;
        private System.Windows.Forms.ComboBox comboBoxRegion;
        private System.Windows.Forms.Label labelLanguage;
        private System.Windows.Forms.ComboBox comboBoxLanguage;
        private System.Windows.Forms.CheckBox checkBoxHideNonGame;
        private System.Windows.Forms.CheckBox checkBoxNoZZZ;
        private System.Windows.Forms.CheckBox checkBoxScrapeByGameID;
        private System.Windows.Forms.Panel panelOptions;
    }
}
