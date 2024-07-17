namespace GamelistManager
{
    partial class BatoceraHostSetup
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.BatoceraHostName = new System.Windows.Forms.TextBox();
            this.BatoceraUserID = new System.Windows.Forms.TextBox();
            this.BatoceraUserPassword = new System.Windows.Forms.TextBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonExit = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.labelFolderSetup = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label1.Location = new System.Drawing.Point(3, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Batocera Host Name:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label2.Location = new System.Drawing.Point(3, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 15);
            this.label2.TabIndex = 1;
            this.label2.Text = "Batocera User ID:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label3.Location = new System.Drawing.Point(3, 119);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(135, 15);
            this.label3.TabIndex = 2;
            this.label3.Text = "Batocera User Password:";
            // 
            // BatoceraHostName
            // 
            this.BatoceraHostName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BatoceraHostName.Location = new System.Drawing.Point(6, 47);
            this.BatoceraHostName.Name = "BatoceraHostName";
            this.BatoceraHostName.Size = new System.Drawing.Size(72, 23);
            this.BatoceraHostName.TabIndex = 3;
            this.BatoceraHostName.TextChanged += new System.EventHandler(this.Textbox_TextChanged);
            // 
            // BatoceraUserID
            // 
            this.BatoceraUserID.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BatoceraUserID.Location = new System.Drawing.Point(6, 92);
            this.BatoceraUserID.Name = "BatoceraUserID";
            this.BatoceraUserID.Size = new System.Drawing.Size(72, 23);
            this.BatoceraUserID.TabIndex = 4;
            this.BatoceraUserID.TextChanged += new System.EventHandler(this.Textbox_TextChanged);
            // 
            // BatoceraUserPassword
            // 
            this.BatoceraUserPassword.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.BatoceraUserPassword.Location = new System.Drawing.Point(6, 135);
            this.BatoceraUserPassword.Name = "BatoceraUserPassword";
            this.BatoceraUserPassword.Size = new System.Drawing.Size(72, 23);
            this.BatoceraUserPassword.TabIndex = 5;
            this.BatoceraUserPassword.UseSystemPasswordChar = true;
            this.BatoceraUserPassword.TextChanged += new System.EventHandler(this.Textbox_TextChanged);
            // 
            // buttonSave
            // 
            this.buttonSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.buttonSave.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.buttonSave.Location = new System.Drawing.Point(6, 174);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(64, 23);
            this.buttonSave.TabIndex = 8;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = false;
            this.buttonSave.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonExit
            // 
            this.buttonExit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.buttonExit.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.buttonExit.Location = new System.Drawing.Point(87, 174);
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size(64, 23);
            this.buttonExit.TabIndex = 9;
            this.buttonExit.Text = "Exit";
            this.buttonExit.UseVisualStyleBackColor = false;
            this.buttonExit.Click += new System.EventHandler(this.button2_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.checkBox1.Location = new System.Drawing.Point(81, 137);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(55, 19);
            this.checkBox1.TabIndex = 11;
            this.checkBox1.Text = "Show";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // labelFolderSetup
            // 
            this.labelFolderSetup.AutoSize = true;
            this.labelFolderSetup.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFolderSetup.ForeColor = System.Drawing.Color.Crimson;
            this.labelFolderSetup.Location = new System.Drawing.Point(10, 7);
            this.labelFolderSetup.Name = "labelFolderSetup";
            this.labelFolderSetup.Size = new System.Drawing.Size(146, 18);
            this.labelFolderSetup.TabIndex = 27;
            this.labelFolderSetup.Text = "Batocera Host Setup";
            // 
            // BatoceraHostSetup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelFolderSetup);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.buttonExit);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.BatoceraUserPassword);
            this.Controls.Add(this.BatoceraUserID);
            this.Controls.Add(this.BatoceraHostName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "BatoceraHostSetup";
            this.Size = new System.Drawing.Size(164, 209);
            this.Load += new System.EventHandler(this.BatoceraHostSetup_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox BatoceraHostName;
        private System.Windows.Forms.TextBox BatoceraUserID;
        private System.Windows.Forms.TextBox BatoceraUserPassword;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label labelFolderSetup;
    }
}
