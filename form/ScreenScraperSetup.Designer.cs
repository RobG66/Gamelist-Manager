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
            this.checkboxShowPassword = new System.Windows.Forms.CheckBox();
            this.labelNote = new System.Windows.Forms.Label();
            this.buttonExit = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.textboxScreenScraperPassword = new System.Windows.Forms.TextBox();
            this.textboxScreenScraperName = new System.Windows.Forms.TextBox();
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
            this.SuspendLayout();
            // 
            // checkboxShowPassword
            // 
            this.checkboxShowPassword.AutoSize = true;
            this.checkboxShowPassword.BackColor = System.Drawing.Color.Transparent;
            this.checkboxShowPassword.Location = new System.Drawing.Point(84, 69);
            this.checkboxShowPassword.Name = "Checkbox_ShowPassword";
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
            this.labelNote.Location = new System.Drawing.Point(0, 302);
            this.labelNote.Name = "Label_Note";
            this.labelNote.Size = new System.Drawing.Size(239, 23);
            this.labelNote.TabIndex = 20;
            this.labelNote.Text = "Credentials are saved in Credential Manager";
            this.labelNote.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonExit
            // 
            this.buttonExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.buttonExit.Location = new System.Drawing.Point(156, 41);
            this.buttonExit.Name = "Button_Exit";
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
            this.buttonSave.Name = "Button_Save";
            this.buttonSave.Size = new System.Drawing.Size(64, 23);
            this.buttonSave.TabIndex = 18;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = false;
            this.buttonSave.Click += new System.EventHandler(this.Button1_Click);
            // 
            // textboxScreenScraperPassword
            // 
            this.textboxScreenScraperPassword.Location = new System.Drawing.Point(9, 67);
            this.textboxScreenScraperPassword.Name = "Textbox_ScreenScraperPassword";
            this.textboxScreenScraperPassword.Size = new System.Drawing.Size(72, 20);
            this.textboxScreenScraperPassword.TabIndex = 17;
            this.textboxScreenScraperPassword.UseSystemPasswordChar = true;
            this.textboxScreenScraperPassword.TextChanged += new System.EventHandler(this.ScreenScraperPassword_TextChanged);
            // 
            // textboxScreenScraperName
            // 
            this.textboxScreenScraperName.Location = new System.Drawing.Point(9, 28);
            this.textboxScreenScraperName.Name = "Textbox_ScreenScraperName";
            this.textboxScreenScraperName.Size = new System.Drawing.Size(72, 20);
            this.textboxScreenScraperName.TabIndex = 16;
            this.textboxScreenScraperName.TextChanged += new System.EventHandler(this.ScreenScraperID_TextChanged);
            // 
            // labelPassword
            // 
            this.labelPassword.AutoSize = true;
            this.labelPassword.BackColor = System.Drawing.Color.Transparent;
            this.labelPassword.Location = new System.Drawing.Point(6, 51);
            this.labelPassword.Name = "Label_Password";
            this.labelPassword.Size = new System.Drawing.Size(130, 13);
            this.labelPassword.TabIndex = 14;
            this.labelPassword.Text = "ScreenScraper Password:";
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.BackColor = System.Drawing.Color.Transparent;
            this.labelName.Location = new System.Drawing.Point(6, 12);
            this.labelName.Name = "Label_Name";
            this.labelName.Size = new System.Drawing.Size(109, 13);
            this.labelName.TabIndex = 13;
            this.labelName.Text = "ScreenScraper Name";
            // 
            // comboBoxImageSource
            // 
            this.comboBoxImageSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxImageSource.FormattingEnabled = true;
            this.comboBoxImageSource.Items.AddRange(new object[] {
            "Screenshot",
            "Title Screenshot",
            "Mix V1",
            "Mix V2",
            "Box 2D",
            "Box 3D",
            "Fan Art"});
            this.comboBoxImageSource.Location = new System.Drawing.Point(122, 95);
            this.comboBoxImageSource.Name = "ComboBox_ImageSource";
            this.comboBoxImageSource.Size = new System.Drawing.Size(107, 21);
            this.comboBoxImageSource.TabIndex = 22;
            this.comboBoxImageSource.SelectedIndexChanged += new System.EventHandler(this.ComboBox_ImageSource_SelectedIndexChanged);
            // 
            // labelImageSource
            // 
            this.labelImageSource.AutoSize = true;
            this.labelImageSource.BackColor = System.Drawing.Color.Transparent;
            this.labelImageSource.Location = new System.Drawing.Point(5, 98);
            this.labelImageSource.Name = "Label_ImageSource";
            this.labelImageSource.Size = new System.Drawing.Size(73, 13);
            this.labelImageSource.TabIndex = 23;
            this.labelImageSource.Text = "Image Source";
            // 
            // labelBoxSource
            // 
            this.labelBoxSource.AutoSize = true;
            this.labelBoxSource.BackColor = System.Drawing.Color.Transparent;
            this.labelBoxSource.Location = new System.Drawing.Point(5, 121);
            this.labelBoxSource.Name = "Label_BoxSource";
            this.labelBoxSource.Size = new System.Drawing.Size(62, 13);
            this.labelBoxSource.TabIndex = 25;
            this.labelBoxSource.Text = "Box Source";
            // 
            // comboBoxBoxSource
            // 
            this.comboBoxBoxSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBoxSource.FormattingEnabled = true;
            this.comboBoxBoxSource.Items.AddRange(new object[] {
            "Box 2D",
            "Box 3D"});
            this.comboBoxBoxSource.Location = new System.Drawing.Point(122, 118);
            this.comboBoxBoxSource.Name = "ComboBox_BoxSource";
            this.comboBoxBoxSource.Size = new System.Drawing.Size(107, 21);
            this.comboBoxBoxSource.TabIndex = 24;
            this.comboBoxBoxSource.SelectedIndexChanged += new System.EventHandler(this.ComboBox_BoxSource_SelectedIndexChanged);
            // 
            // labelLogoSource
            // 
            this.labelLogoSource.AutoSize = true;
            this.labelLogoSource.BackColor = System.Drawing.Color.Transparent;
            this.labelLogoSource.Location = new System.Drawing.Point(5, 144);
            this.labelLogoSource.Name = "Label_LogoSource";
            this.labelLogoSource.Size = new System.Drawing.Size(68, 13);
            this.labelLogoSource.TabIndex = 27;
            this.labelLogoSource.Text = "Logo Source";
            // 
            // comboBoxLogoSource
            // 
            this.comboBoxLogoSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLogoSource.FormattingEnabled = true;
            this.comboBoxLogoSource.Items.AddRange(new object[] {
            "Wheel",
            "Marquee"});
            this.comboBoxLogoSource.Location = new System.Drawing.Point(122, 141);
            this.comboBoxLogoSource.Name = "ComboBox_LogoSource";
            this.comboBoxLogoSource.Size = new System.Drawing.Size(107, 21);
            this.comboBoxLogoSource.TabIndex = 26;
            this.comboBoxLogoSource.SelectedIndexChanged += new System.EventHandler(this.ComboBox_LogoSource_SelectedIndexChanged);
            // 
            // labelRegion
            // 
            this.labelRegion.AutoSize = true;
            this.labelRegion.BackColor = System.Drawing.Color.Transparent;
            this.labelRegion.Location = new System.Drawing.Point(5, 167);
            this.labelRegion.Name = "Label_Region";
            this.labelRegion.Size = new System.Drawing.Size(87, 13);
            this.labelRegion.TabIndex = 29;
            this.labelRegion.Text = "Preferred Region";
            // 
            // comboBoxRegion
            // 
            this.comboBoxRegion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRegion.FormattingEnabled = true;
            this.comboBoxRegion.Items.AddRange(new object[] {
            "de: Germany",
            "asi: Asia",
            "au: Australia",
            "br: Brazil",
            "bg: Bulgaria",
            "ca: Canada",
            "cl: Chile",
            "cn: China",
            "kr: Korea",
            "dk: Denmark",
            "sp: Spain",
            "eu: Europe",
            "fi: Finland",
            "fr: France",
            "gr: Greece",
            "hu: Hungary",
            "il: Israel",
            "it: Italy",
            "jp: Japan",
            "kw: Kuwait",
            "wor: World",
            "mor: Middle East",
            "no: Norway",
            "nz: New Zealand",
            "oce: Oceania",
            "nl: Netherlands",
            "pe: Peru",
            "pl: Poland",
            "pt: Portugal",
            "cz: Czech republic",
            "uk: United Kingdom",
            "ru: Russia",
            "sk: Slovakia",
            "se: Sweden",
            "tw: Taiwan",
            "tr: Turkey",
            "us: USA",
            "ss: ScreenScraper"});
            this.comboBoxRegion.Location = new System.Drawing.Point(122, 164);
            this.comboBoxRegion.Name = "ComboBox_Region";
            this.comboBoxRegion.Size = new System.Drawing.Size(107, 21);
            this.comboBoxRegion.TabIndex = 28;
            // 
            // labelLanguage
            // 
            this.labelLanguage.AutoSize = true;
            this.labelLanguage.BackColor = System.Drawing.Color.Transparent;
            this.labelLanguage.Location = new System.Drawing.Point(5, 190);
            this.labelLanguage.Name = "Label_Language";
            this.labelLanguage.Size = new System.Drawing.Size(101, 13);
            this.labelLanguage.TabIndex = 31;
            this.labelLanguage.Text = "Preferred Language";
            // 
            // comboBoxLanguage
            // 
            this.comboBoxLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLanguage.FormattingEnabled = true;
            this.comboBoxLanguage.Items.AddRange(new object[] {
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
            this.comboBoxLanguage.Location = new System.Drawing.Point(122, 187);
            this.comboBoxLanguage.Name = "ComboBox_Language";
            this.comboBoxLanguage.Size = new System.Drawing.Size(107, 21);
            this.comboBoxLanguage.TabIndex = 30;
            // 
            // labelMaxThreads
            // 
            this.labelMaxThreads.AutoSize = true;
            this.labelMaxThreads.BackColor = System.Drawing.Color.Transparent;
            this.labelMaxThreads.Location = new System.Drawing.Point(6, 213);
            this.labelMaxThreads.Name = "Label_MaxThreads";
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
            this.comboBoxMaxThreads.Location = new System.Drawing.Point(122, 210);
            this.comboBoxMaxThreads.Name = "comboBox_MaxThreads";
            this.comboBoxMaxThreads.Size = new System.Drawing.Size(107, 21);
            this.comboBoxMaxThreads.TabIndex = 32;
            // 
            // checkBoxHideNonGame
            // 
            this.checkBoxHideNonGame.AutoSize = true;
            this.checkBoxHideNonGame.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxHideNonGame.Location = new System.Drawing.Point(8, 234);
            this.checkBoxHideNonGame.Name = "CheckBox_HideNonGame";
            this.checkBoxHideNonGame.Size = new System.Drawing.Size(161, 17);
            this.checkBoxHideNonGame.TabIndex = 34;
            this.checkBoxHideNonGame.Text = "Automatically hide non-game";
            this.checkBoxHideNonGame.UseVisualStyleBackColor = false;
            // 
            // checkBoxNoZZZ
            // 
            this.checkBoxNoZZZ.AutoSize = true;
            this.checkBoxNoZZZ.BackColor = System.Drawing.Color.Transparent;
            this.checkBoxNoZZZ.Location = new System.Drawing.Point(8, 257);
            this.checkBoxNoZZZ.Name = "CheckBox_NoZZZ";
            this.checkBoxNoZZZ.Size = new System.Drawing.Size(160, 17);
            this.checkBoxNoZZZ.TabIndex = 35;
            this.checkBoxNoZZZ.Text = "Remove ZZZ(not game) text";
            this.checkBoxNoZZZ.UseVisualStyleBackColor = false;
            // 
            // ScreenScraperSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GamelistManager.Properties.Resources.background6;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(239, 325);
            this.Controls.Add(this.checkBoxNoZZZ);
            this.Controls.Add(this.checkBoxHideNonGame);
            this.Controls.Add(this.labelMaxThreads);
            this.Controls.Add(this.comboBoxMaxThreads);
            this.Controls.Add(this.labelLanguage);
            this.Controls.Add(this.comboBoxLanguage);
            this.Controls.Add(this.labelRegion);
            this.Controls.Add(this.comboBoxRegion);
            this.Controls.Add(this.labelLogoSource);
            this.Controls.Add(this.comboBoxLogoSource);
            this.Controls.Add(this.labelBoxSource);
            this.Controls.Add(this.comboBoxBoxSource);
            this.Controls.Add(this.labelImageSource);
            this.Controls.Add(this.comboBoxImageSource);
            this.Controls.Add(this.checkboxShowPassword);
            this.Controls.Add(this.labelNote);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.textboxScreenScraperPassword);
            this.Controls.Add(this.textboxScreenScraperName);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.labelName);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ScreenScraperSetup";
            this.Text = "ScreenScraper Setup";
            this.Load += new System.EventHandler(this.ScreenScraperSetup_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkboxShowPassword;
        private System.Windows.Forms.Label labelNote;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.TextBox textboxScreenScraperPassword;
        private System.Windows.Forms.TextBox textboxScreenScraperName;
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
    }
}
