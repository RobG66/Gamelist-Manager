using System.Windows.Media;

namespace GamelistManager.classes.helpers
{
    public static class ColorHelper
    {
        /// <summary>
        /// Returns a readable foreground color (Black or White)
        /// based on the luminance of the background color.
        /// </summary>
        public static SolidColorBrush GetReadableForeground(Color background)
        {
            double luminance =
                (0.299 * background.R) +
                (0.587 * background.G) +
                (0.114 * background.B);

            return luminance > 128 ? Brushes.Black : Brushes.White;
        }

        /// <summary>
        /// Determines if a color is "light".
        /// </summary>
        public static bool IsLightColor(Color color)
        {
            double luminance =
                (0.299 * color.R) +
                (0.587 * color.G) +
                (0.114 * color.B);

            return luminance > 128;
        }
    }
}