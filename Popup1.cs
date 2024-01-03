using System;
using System.Windows.Forms;

namespace GamelistManager
{
    public partial class Popup1 : Form
    {
        public bool BoolResult { get; private set; }
        public int IntResult { get; private set; }

        public Popup1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            (BoolResult, IntResult) = ReturnTrue();
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            (BoolResult, IntResult) = ReturnFalse();
            this.Close();
        }

        private (bool, int) ReturnTrue()
        {
            int intResult = 0;
            if (radioButton1.Checked)
            {
                intResult = 1;
            }
            else if (radioButton2.Checked)
            {
                intResult = 2;
            }
            else if (radioButton3.Checked)
            {
                intResult = 3;
            }

            return (true, intResult);
        }

        private (bool, int) ReturnFalse()
        {
            return (false, 0);
        }
    }
}
