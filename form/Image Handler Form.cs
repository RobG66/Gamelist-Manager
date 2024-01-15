using System;
using System.Windows.Forms;

namespace GamelistManager
{
    public partial class Popup1 : Form
    {
        public bool BoolResult { get; private set; }
        public int intResult { get; private set; }
        int corruptedImages = 0;
        int singleColorImages = 0;
        int missingImages = 0;

        // Modified constructor to accept three integers
        public Popup1(int intValue1, int intValue2, int intValue3)
        {
            InitializeComponent();
            corruptedImages = intValue1;
            singleColorImages = intValue2;
            missingImages = intValue3;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            (BoolResult, intResult) = ReturnTrue();
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            (BoolResult, intResult) = ReturnFalse();
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

        private void Popup1_Load(object sender, EventArgs e)
        {
            label2.Text = $"There are {corruptedImages} corrupt, {singleColorImages} single color and {missingImages} missing images";

            if (corruptedImages == 0 && singleColorImages == 0 && missingImages != 0)
            {
                // If it's only missing images, provide a clear choice
                radioButton2.Text = "Clear image paths";
                radioButton3.Enabled = false;
            }

        }
    }
}
