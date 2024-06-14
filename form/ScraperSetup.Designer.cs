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
            this.comboBoxImageSource = new System.Windows.Forms.ComboBox();
            this.labelImageSource = new System.Windows.Forms.Label();
            this.labelBoxSource = new System.Windows.Forms.Label();
            this.comboBoxBoxSource = new System.Windows.Forms.ComboBox();
            this.labelLogoSource = new System.Windows.Forms.Label();
            this.comboBoxLogoSource = new System.Windows.Forms.ComboBox();
            this.labelRegion = new System.Windows.Forms.Label();
            this.comboBoxRegion = new System.Windows.Forms.ComboBox();
            this.labelLanguage = new System.Windows.Forms.Label();
            this.comboBoxLanguage = new System.Windows.Forms.ComboBox();
            this.labelMaxThreads = new System.Windows.Forms.Label();
            this.comboBoxMaxThreads = new System.Windows.Forms.ComboBox();
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
            this.labelNote.Location = new System.Drawing.Point(0, 307);
            this.labelNote.Name = "labelNote";
            this.labelNote.Size = new System.Drawing.Size(239, 23);
            this.labelNote.TabIndex = 20;
            this.labelNote.Text = "Credentials are saved in Credential Manager";
            this.labelNote.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonExit
            // 
            this.buttonExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.buttonExit.Location = new System.Drawing.Point(156, 41);
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
            this.buttonSave.Location = new System.Drawing.Point(156, 12);
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
            // comboBoxImageSource
            // 
            this.comboBoxImageSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxImageSource.FormattingEnabled = true;
            this.comboBoxImageSource.Location = new System.Drawing.Point(124, 6);
            this.comboBoxImageSource.Name = "comboBoxImageSource";
            this.comboBoxImageSource.Size = new System.Drawing.Size(107, 21);
            this.comboBoxImageSource.TabIndex = 22;
            this.comboBoxImageSource.SelectedIndexChanged += new System.EventHandler(this.ComboBox_ImageSource_SelectedIndexChanged);
            // 
            // labelImageSource
            // 
            this.labelImageSource.AutoSize = true;
            this.labelImageSource.BackColor = System.Drawing.Color.Transparent;
            this.labelImageSource.Location = new System.Drawing.Point(7, 9);
            this.labelImageSource.Name = "labelImageSource";
            this.labelImageSource.Size = new System.Drawing.Size(73, 13);
            this.labelImageSource.TabIndex = 23;
            this.labelImageSource.Text = "Image Source";
            // 
            // labelBoxSource
            // 
            this.labelBoxSource.AutoSize = true;
            this.labelBoxSource.BackColor = System.Drawing.Color.Transparent;
            this.labelBoxSource.Location = new System.Drawing.Point(7, 32);
            this.labelBoxSource.Name = "labelBoxSource";
            this.labelBoxSource.Size = new System.Drawing.Size(62, 13);
            this.labelBoxSource.TabIndex = 25;
            this.labelBoxSource.Text = "Box Source";
            // 
            // comboBoxBoxSource
            // 
            this.comboBoxBoxSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBoxSource.FormattingEnabled = true;
            this.comboBoxBoxSource.Location = new System.Drawing.Point(124, 29);
            this.comboBoxBoxSource.Name = "comboBoxBoxSource";
            this.comboBoxBoxSource.Size = new System.Drawing.Size(107, 21);
            this.comboBoxBoxSource.TabIndex = 24;
            this.comboBoxBoxSource.SelectedIndexChanged += new System.EventHandler(this.ComboBox_BoxSource_SelectedIndexChanged);
            // 
            // labelLogoSource
            // 
            this.labelLogoSource.AutoSize = true;
            this.labelLogoSource.BackColor = System.Drawing.Color.Transparent;
            this.labelLogoSource.Location = new System.Drawing.Point(7, 55);
            this.labelLogoSource.Name = "labelLogoSource";
            this.labelLogoSource.Size = new System.Drawing.Size(68, 13);
            this.labelLogoSource.TabIndex = 27;
            this.labelLogoSource.Text = "Logo Source";
            // 
            // comboBoxLogoSource
            // 
            this.comboBoxLogoSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLogoSource.FormattingEnabled = true;
            this.comboBoxLogoSource.Location = new System.Drawing.Point(124, 52);
            this.comboBoxLogoSource.Name = "comboBoxLogoSource";
            this.comboBoxLogoSource.Size = new System.Drawing.Size(107, 21);
            this.comboBoxLogoSource.TabIndex = 26;
            this.comboBoxLogoSource.SelectedIndexChanged += new System.EventHandler(this.ComboBox_LogoSource_SelectedIndexChanged);
            // 
            // labelRegion
            // 
            this.labelRegion.AutoSize = true;
            this.labelRegion.BackColor = System.Drawing.Color.Transparent;
            this.labelRegion.Location = new System.Drawing.Point(7, 78);
            this.labelRegion.Name = "labelRegion";
            this.labelRegion.Size = new System.Drawing.Size(87, 13);
            this.labelRegion.TabIndex = 29;
            this.labelRegion.Text = "Preferred Region";
            // 
            // comboBoxRegion
            // 
            this.comboBoxRegion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRegion.FormattingEnabled = true;
            this.comboBoxRegion.Location = new System.Drawing.Point(124, 75);
            this.comboBoxRegion.Name = "comboBoxRegion";
            this.comboBoxRegion.Size = new System.Drawing.Size(107, 21);
            this.comboBoxRegion.TabIndex = 28;
            // 
            // labelLanguage
            // 
            this.labelLanguage.AutoSize = true;
            this.labelLanguage.BackColor = System.Drawing.Color.Transparent;
            this.labelLanguage.Location = new System.Drawing.Point(7, 101);
            this.labelLanguage.Name = "labelLanguage";
            this.labelLanguage.Size = new System.Drawing.Size(101, 13);
            this.labelLanguage.TabIndex = 31;
            this.labelLanguage.Text = "Preferred Language";
            // 
            // comboBoxLanguage
            // 
            this.comboBoxLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLanguage.FormattingEnabled = true;
            this.comboBoxLanguage.Location = new System.Drawing.Point(124, 98);
            this.comboBoxLanguage.Name = "comboBoxLanguage";
            this.comboBoxLanguage.Size = new System.Drawing.Size(107, 21);
            this.comboBoxLanguage.TabIndex = 30;
            // 
            // labelMaxThreads
            // 
            this.labelMaxThreads.AutoSize = true;
            this.labelMaxThreads.BackColor = System.Drawing.Color.Transparent;
            this.labelMaxThreads.Location = new System.Drawing.Point(8, 124);
            this.labelMaxThreads.Name = "labelMaxThreads";
            this.labelMaxThreads.Size = new System.Drawing.Size(95, 13);
            this.labelMaxThreads.TabIndex = 33;
            this.labelMaxThreads.Text = "Max Thread Count";
            // 
            // comboBoxMaxThreads
            // 
            this.comboBoxMaxThreads.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMaxThreads.FormattingEnabled = true;
            this.comboBoxMaxThreads.Items.AddRange(new object[] {
            "en: English",
            "de: German",
            "zh: Chinese",
            "ko: Korean",
            "da: Danish",
            "es: Spanish",
            "fi: Finnish",
            "fr: French",
            "hu: Hungarian",
            "it: Italian",
            "ja: Japanese",
            "nl: Dutch",
            "no: Norwegian",
            "pl: Polish",
            "pt: Portuguese",
            "ru: Russian",
            "sk: Slovakian",
            "sv: Swedish",
            "cz: Czech",
            "tr: Turkish"});
            this.comboBoxMaxThreads.Location = new System.Drawing.Point(124, 121);
            this.comboBoxMaxThreads.Name = "comboBoxMaxThreads";
            this.comboBoxMaxThreads.Size = new System.Drawing.Size(107, 21);
            this.comboBoxMaxThreads.TabIndex = 32;
            // 
            // checkBoxHideNonGame
            // 
            this.checkBoxHideNonGame.AutoSize = true;
            this.checkBoxHideNonGame.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxHideNonGame.Location = new System.Drawing.Point(10, 145);
            this.checkBoxHideNonGame.Name = "checkBoxHideNonGame";
            this.checkBoxHideNonGame.Size = new System.Drawing.Size(161, 17);
            this.checkBoxHideNonGame.TabIndex = 34;
            this.checkBoxHideNonGame.Text = "Automatically hide non-game";
            this.checkBoxHideNonGame.UseVisualStyleBackColor = false;
            // 
            // checkBoxNoZZZ
            // 
            this.checkBoxNoZZZ.AutoSize = true;
            this.checkBoxNoZZZ.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxNoZZZ.Location = new System.Drawing.Point(10, 168);
            this.checkBoxNoZZZ.Name = "checkBoxNoZZZ";
            this.checkBoxNoZZZ.Size = new System.Drawing.Size(160, 17);
            this.checkBoxNoZZZ.TabIndex = 35;
            this.checkBoxNoZZZ.Text = "Remove ZZZ(not game) text";
            this.checkBoxNoZZZ.UseVisualStyleBackColor = false;
            // 
            // checkBoxScrapeByGameID
            // 
            this.checkBoxScrapeByGameID.AutoSize = true;
            this.checkBoxScrapeByGameID.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxScrapeByGameID.Location = new System.Drawing.Point(10, 191);
            this.checkBoxScrapeByGameID.Name = "checkBoxScrapeByGameID";
            this.checkBoxScrapeByGameID.Size = new System.Drawing.Size(172, 17);
            this.checkBoxScrapeByGameID.TabIndex = 36;
            this.checkBoxScrapeByGameID.Text = "Scrape by Game ID if available";
            this.checkBoxScrapeByGameID.UseVisualStyleBackColor = false;
            // 
            // panelOptions
            // 
            this.panelOptions.BackColor = System.Drawing.Color.Transparent;
            this.panelOptions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelOptions.Controls.Add(this.labelImageSource);
            this.panelOptions.Controls.Add(this.checkBoxScrapeByGameID);
            this.panelOptions.Controls.Add(this.comboBoxImageSource);
            this.panelOptions.Controls.Add(this.checkBoxNoZZZ);
            this.panelOptions.Controls.Add(this.comboBoxBoxSource);
            this.panelOptions.Controls.Add(this.checkBoxHideNonGame);
            this.panelOptions.Controls.Add(this.labelBoxSource);
            this.panelOptions.Controls.Add(this.labelMaxThreads);
            this.panelOptions.Controls.Add(this.comboBoxLogoSource);
            this.panelOptions.Controls.Add(this.comboBoxMaxThreads);
            this.panelOptions.Controls.Add(this.labelLogoSource);
            this.panelOptions.Controls.Add(this.labelLanguage);
            this.panelOptions.Controls.Add(this.comboBoxRegion);
            this.panelOptions.Controls.Add(this.comboBoxLanguage);
            this.panelOptions.Controls.Add(this.labelRegion);
            this.panelOptions.Enabled = false;
            this.panelOptions.Location = new System.Drawing.Point(2, 91);
            this.panelOptions.Name = "panelOptions";
            this.panelOptions.Size = new System.Drawing.Size(235, 215);
            this.panelOptions.TabIndex = 37;
            // 
            // ScraperSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GamelistManager.Properties.Resources.background6;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(239, 330);
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
        private System.Windows.Forms.ComboBox comboBoxImageSource;
        private System.Windows.Forms.Label labelImageSource;
        private System.Windows.Forms.Label labelBoxSource;
        private System.Windows.Forms.ComboBox comboBoxBoxSource;
        private System.Windows.Forms.Label labelLogoSource;
        private System.Windows.Forms.ComboBox comboBoxLogoSource;
        private System.Windows.Forms.Label labelRegion;
        private System.Windows.Forms.ComboBox comboBoxRegion;
        private System.Windows.Forms.Label labelLanguage;
        private System.Windows.Forms.ComboBox comboBoxLanguage;
        private System.Windows.Forms.Label labelMaxThreads;
        private System.Windows.Forms.ComboBox comboBoxMaxThreads;
        private System.Windows.Forms.CheckBox checkBoxHideNonGame;
        private System.Windows.Forms.CheckBox checkBoxNoZZZ;
        private System.Windows.Forms.CheckBox checkBoxScrapeByGameID;
        private System.Windows.Forms.Panel panelOptions;
    }
}
