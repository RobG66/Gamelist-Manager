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
    }
}
