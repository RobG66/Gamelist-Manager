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
            if (string.IsNullOrEmpty(imagePath1))
            {
                pictureBox1.Image = null;
            }
            else
            {
                pictureBox1.ImageLocation = imagePath1;
            }

            if (string.IsNullOrEmpty(imagePath2))
            {
                pictureBox2.Image = null;
            }
            else
            {
                pictureBox2.ImageLocation = imagePath2;
            }
        }
    }
}
