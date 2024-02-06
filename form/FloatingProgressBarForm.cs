using System.Windows.Forms;

namespace GamelistManager
{
    public partial class FloatingProgressBarForm : Form
    {
        private int _progressValue;

        public FloatingProgressBarForm()
        {
            InitializeComponent();
        }

        public void IncrementProgressBar()
        {
            if (progressBar1.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    _progressValue = progressBar1.Value;
                    _progressValue++;
                    progressBar1.Value = _progressValue;
                });
            }
            else
            {
                _progressValue = progressBar1.Value;
                _progressValue++;
                progressBar1.Value = _progressValue;
            }
        }

        public void ResetProgressBar(int minimum, int maximum)
        {
            if (progressBar1.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    progressBar1.Minimum = minimum;
                    progressBar1.Maximum = maximum;
                    progressBar1.Step = 1;
                    progressBar1.Value = minimum;
                });
            }
            else
            {
                progressBar1.Minimum = minimum;
                progressBar1.Maximum = maximum;
                progressBar1.Value = minimum;
            }

            _progressValue = minimum;
        }
    }
}
