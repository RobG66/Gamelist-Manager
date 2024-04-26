using System;
using System.Drawing;
using System.Windows.Forms;
using Button = System.Windows.Forms.Button;

namespace GamelistManager.control
{
    public partial class MediaButtons : UserControl
    {
        public event EventHandler Button1Clicked;
        public event EventHandler Button2Clicked;
        public event EventHandler Button3Clicked;

        public MediaButtons()
        {
            InitializeComponent();
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            button1.Click += Button_Click;
            button2.Click += Button_Click;
            button3.Click += Button_Click;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            Button button = sender as Button;
            if (button == button1)
                OnButton1Clicked(EventArgs.Empty);
            else if (button == button2)
                OnButton2Clicked(EventArgs.Empty);
            else if (button == button3)
                OnButton3Clicked(EventArgs.Empty);
        }

        protected virtual void OnButton1Clicked(EventArgs e)
        {
            Button1Clicked?.Invoke(this, e);
        }

        protected virtual void OnButton2Clicked(EventArgs e)
        {
            Button2Clicked?.Invoke(this, e);
        }

        protected virtual void OnButton3Clicked(EventArgs e)
        {
            Button3Clicked?.Invoke(this, e);
        }

        public void SetButtonEnabledState(int buttonIndex, bool enabled)
        {
            // Assuming buttonIndex starts from 0
            if (buttonIndex == 0)
            {
                button1.Enabled = enabled;
                if (enabled)
                {
                    button1.BackColor = SystemColors.Control;
                    button1.Visible = true;
                    tableLayoutPanel1.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, 100);
                    toolTip1.SetToolTip(button1, "Confirm Change");
                }
                else
                {
                    button1.BackColor = Color.LightGray;
                    button1.Visible = false;
                    tableLayoutPanel1.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, 0);
                    toolTip1.SetToolTip(button1, null);
                }
            }
            else if (buttonIndex == 1)
            {
                button2.Enabled = enabled;
                if (enabled)
                {
                    button2.BackColor = SystemColors.Control;
                    toolTip2.SetToolTip(button2, "Remove Item");
                    button2.Visible = true;
                    tableLayoutPanel1.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 100);
                }
                else
                {
                    button2.BackColor = Color.LightGray;
                    toolTip2.SetToolTip(button2, null);
                    button2.Visible = false;
                    tableLayoutPanel1.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, 0);
                }
            }
            else if (buttonIndex == 2)
            {
                button3.Enabled = enabled;
                if (enabled)
                {
                    button3.BackColor = SystemColors.Control;
                    toolTip3.SetToolTip(button3, "Reset To Original");
                    button3.Visible = true;
                    tableLayoutPanel1.ColumnStyles[2] = new ColumnStyle(SizeType.Percent, 100);
                }
                else
                {
                    button3.BackColor = Color.LightGray;
                    toolTip3.SetToolTip(button3, null);
                    button3.Visible = false;
                    tableLayoutPanel1.ColumnStyles[2] = new ColumnStyle(SizeType.Absolute, 0);
                }
            }
        }
    }
}
