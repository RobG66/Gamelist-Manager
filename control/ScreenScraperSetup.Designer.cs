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
            this.label_Region = new System.Windows.Forms.Label();
            this.comboBox_Region = new System.Windows.Forms.ComboBox();
            this.label_Language = new System.Windows.Forms.Label();
            this.comboBox_Language = new System.Windows.Forms.ComboBox();
            this.label_MaxThreads = new System.Windows.Forms.Label();
            this.comboBox_MaxThreads = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(84, 69);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(53, 17);
            this.checkBox1.TabIndex = 21;
            this.checkBox1.Text = "Show";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // label5
            // 
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(142, 6);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(73, 80);
            this.label5.TabIndex = 20;
            this.label5.Text = "Credentials are saved in Windows Credential Manager";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.button2.Location = new System.Drawing.Point(76, 235);
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
            this.button1.Location = new System.Drawing.Point(8, 235);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(64, 23);
            this.button1.TabIndex = 18;
            this.button1.Text = "Save";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textbox_ScreenScraperPassword
            // 
            this.textbox_ScreenScraperPassword.Location = new System.Drawing.Point(9, 67);
            this.textbox_ScreenScraperPassword.Name = "textbox_ScreenScraperPassword";
            this.textbox_ScreenScraperPassword.Size = new System.Drawing.Size(72, 20);
            this.textbox_ScreenScraperPassword.TabIndex = 17;
            this.textbox_ScreenScraperPassword.UseSystemPasswordChar = true;
            this.textbox_ScreenScraperPassword.TextChanged += new System.EventHandler(this.ScreenScraperPassword_TextChanged);
            // 
            // textbox_ScreenScraperName
            // 
            this.textbox_ScreenScraperName.Location = new System.Drawing.Point(9, 28);
            this.textbox_ScreenScraperName.Name = "textbox_ScreenScraperName";
            this.textbox_ScreenScraperName.Size = new System.Drawing.Size(72, 20);
            this.textbox_ScreenScraperName.TabIndex = 16;
            this.textbox_ScreenScraperName.TextChanged += new System.EventHandler(this.ScreenScraperID_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 51);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(130, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "ScreenScraper Password:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "ScreenScraper Name";
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
            this.comboBox_ImageSource.Location = new System.Drawing.Point(108, 95);
            this.comboBox_ImageSource.Name = "comboBox_ImageSource";
            this.comboBox_ImageSource.Size = new System.Drawing.Size(107, 21);
            this.comboBox_ImageSource.TabIndex = 22;
            this.comboBox_ImageSource.SelectedIndexChanged += new System.EventHandler(this.comboBox_ImageSource_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 98);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 23;
            this.label1.Text = "Image Source";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(5, 121);
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
            this.comboBox_BoxSource.Location = new System.Drawing.Point(108, 118);
            this.comboBox_BoxSource.Name = "comboBox_BoxSource";
            this.comboBox_BoxSource.Size = new System.Drawing.Size(107, 21);
            this.comboBox_BoxSource.TabIndex = 24;
            this.comboBox_BoxSource.SelectedIndexChanged += new System.EventHandler(this.comboBox_BoxSource_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(5, 144);
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
            this.comboBox_LogoSource.Location = new System.Drawing.Point(108, 141);
            this.comboBox_LogoSource.Name = "comboBox_LogoSource";
            this.comboBox_LogoSource.Size = new System.Drawing.Size(107, 21);
            this.comboBox_LogoSource.TabIndex = 26;
            this.comboBox_LogoSource.SelectedIndexChanged += new System.EventHandler(this.comboBox_LogoSource_SelectedIndexChanged);
            // 
            // label_Region
            // 
            this.label_Region.AutoSize = true;
            this.label_Region.Location = new System.Drawing.Point(5, 167);
            this.label_Region.Name = "label_Region";
            this.label_Region.Size = new System.Drawing.Size(87, 13);
            this.label_Region.TabIndex = 29;
            this.label_Region.Text = "Preferred Region";
            // 
            // comboBox_Region
            // 
            this.comboBox_Region.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Region.FormattingEnabled = true;
            this.comboBox_Region.Items.AddRange(new object[] {
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
            this.comboBox_Region.Location = new System.Drawing.Point(108, 164);
            this.comboBox_Region.Name = "comboBox_Region";
            this.comboBox_Region.Size = new System.Drawing.Size(107, 21);
            this.comboBox_Region.TabIndex = 28;
            // 
            // label_Language
            // 
            this.label_Language.AutoSize = true;
            this.label_Language.Location = new System.Drawing.Point(5, 190);
            this.label_Language.Name = "label_Language";
            this.label_Language.Size = new System.Drawing.Size(101, 13);
            this.label_Language.TabIndex = 31;
            this.label_Language.Text = "Preferred Language";
            // 
            // comboBox_Language
            // 
            this.comboBox_Language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Language.FormattingEnabled = true;
            this.comboBox_Language.Items.AddRange(new object[] {
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
            this.comboBox_Language.Location = new System.Drawing.Point(108, 187);
            this.comboBox_Language.Name = "comboBox_Language";
            this.comboBox_Language.Size = new System.Drawing.Size(107, 21);
            this.comboBox_Language.TabIndex = 30;
            // 
            // label_MaxThreads
            // 
            this.label_MaxThreads.AutoSize = true;
            this.label_MaxThreads.Location = new System.Drawing.Point(6, 213);
            this.label_MaxThreads.Name = "label_MaxThreads";
            this.label_MaxThreads.Size = new System.Drawing.Size(95, 13);
            this.label_MaxThreads.TabIndex = 33;
            this.label_MaxThreads.Text = "Max Thread Count";
            // 
            // comboBox_MaxThreads
            // 
            this.comboBox_MaxThreads.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_MaxThreads.FormattingEnabled = true;
            this.comboBox_MaxThreads.Items.AddRange(new object[] {
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
            this.comboBox_MaxThreads.Location = new System.Drawing.Point(108, 210);
            this.comboBox_MaxThreads.Name = "comboBox_MaxThreads";
            this.comboBox_MaxThreads.Size = new System.Drawing.Size(107, 21);
            this.comboBox_MaxThreads.TabIndex = 32;
            // 
            // ScreenScraperSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label_MaxThreads);
            this.Controls.Add(this.comboBox_MaxThreads);
            this.Controls.Add(this.label_Language);
            this.Controls.Add(this.comboBox_Language);
            this.Controls.Add(this.label_Region);
            this.Controls.Add(this.comboBox_Region);
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
            this.Size = new System.Drawing.Size(228, 264);
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
        private System.Windows.Forms.Label label_Region;
        private System.Windows.Forms.ComboBox comboBox_Region;
        private System.Windows.Forms.Label label_Language;
        private System.Windows.Forms.ComboBox comboBox_Language;
        private System.Windows.Forms.Label label_MaxThreads;
        private System.Windows.Forms.ComboBox comboBox_MaxThreads;
    }
}
