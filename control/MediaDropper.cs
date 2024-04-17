using System.Windows.Forms;

namespace GamelistManager.control
{
    public partial class MediaDropper : UserControl
    {
        public MediaDropper()
        {
            InitializeComponent();
            comboBox1.Items.AddRange(SharedData.MediaTypes);
        }

        private void panelDropper_DragDrop(object sender, DragEventArgs e)
        {

        }
    }
}
