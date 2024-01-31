using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamelistManager.form
{
    public partial class ProgressBarForm : Form
    {
        private Form mainForm;

        public ProgressBarForm()
        {
            InitializeComponent();
        }

        public void UpdateProgressBar()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke((MethodInvoker)delegate
                {
                    int value = progressBar1.Value;
                    value++;
                    progressBar1.Value = value;
                });
            }
            else
            {
                int value = progressBar1.Value;
                value++;
                progressBar1.Value = value;
            }
        }

        public void resetprogressbar(int minimum, int maximum)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke((MethodInvoker)delegate
                {
                    progressBar1.Minimum = minimum;
                    progressBar1.Maximum = maximum;
                    progressBar1.Value = minimum;
                });
            }
            else
            {
                progressBar1.Minimum = minimum;
                progressBar1.Maximum = maximum;
                progressBar1.Value = minimum;
            }
        }

        public void SetMainForm(Form gameForm)
        {
            mainForm = gameForm;
        }

    }
}
