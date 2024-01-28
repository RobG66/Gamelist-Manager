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
            this.button2.Location = new System.Drawing.Point(70, 115);
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
            this.button1.Location = new System.Drawing.Point(6, 115);
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
            this.label2.Size = new System.Drawing.Size(120, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "ScreenScraper User ID:";
            // 
            // ScreenScraperSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textbox_ScreenScraperPassword);
            this.Controls.Add(this.textbox_ScreenScraperName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Name = "ScreenScraperSetup";
            this.Size = new System.Drawing.Size(270, 213);
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
    }
}
