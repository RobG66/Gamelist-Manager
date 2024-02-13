using System.Windows.Forms;

namespace GamelistManager.control
{
    public partial class ScraperPreview : UserControl
    {
        public ScraperPreview()
        {
            InitializeComponent();
        }

        // Update PictureBox controls directly
        public void UpdatePictureBox(string imagePath1, string imagePath2)
        {
            pictureBox1.ImageLocation = imagePath1;
            pictureBox2.ImageLocation = imagePath2;
        }
    }
}
