using System.Drawing;
using System.Windows.Forms;

namespace GamelistManager.control
{
    public partial class ScraperPreview : UserControl
    {
        public ScraperPreview()
        {
            InitializeComponent();
        }

        // Update PictureBox control directly
        public void UpdatePictureBox(string imageFilePath)
        {
            if (!string.IsNullOrEmpty(imageFilePath))
            {
                pictureBox1.ImageLocation = imageFilePath;
            }
        }

        public void UpdatePictureBoxWithImage(Image image)
        {
            pictureBox1.Image = image;
        }

    }
}
