namespace GamelistManager.control
{
    partial class MediaDropper
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
            this.buttonAdd = new System.Windows.Forms.Button();
            this.panelDropper = new System.Windows.Forms.Panel();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // buttonAdd
            // 
            this.buttonAdd.Location = new System.Drawing.Point(36, 7);
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonAdd.TabIndex = 1;
            this.buttonAdd.Text = "Add Media";
            this.buttonAdd.UseVisualStyleBackColor = true;
            // 
            // panelDropper
            // 
            this.panelDropper.AllowDrop = true;
            this.panelDropper.BackColor = System.Drawing.Color.Transparent;
            this.panelDropper.BackgroundImage = global::GamelistManager.Properties.Resources.dropicon;
            this.panelDropper.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.panelDropper.Location = new System.Drawing.Point(16, 60);
            this.panelDropper.Name = "panelDropper";
            this.panelDropper.Size = new System.Drawing.Size(118, 108);
            this.panelDropper.TabIndex = 5;
            this.panelDropper.DragDrop += new System.Windows.Forms.DragEventHandler(this.panelDropper_DragDrop);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(16, 33);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(121, 21);
            this.comboBox1.Sorted = true;
            this.comboBox1.TabIndex = 6;
            // 
            // MediaDropper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GamelistManager.Properties.Resources.background21;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.panelDropper);
            this.Controls.Add(this.buttonAdd);
            this.DoubleBuffered = true;
            this.Name = "MediaDropper";
            this.Size = new System.Drawing.Size(148, 181);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button buttonAdd;
        private System.Windows.Forms.Panel panelDropper;
        private System.Windows.Forms.ComboBox comboBox1;
    }
}
