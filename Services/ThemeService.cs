using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using System;
using System.Linq;

namespace Gamelist_Manager.Services
{
    public class ThemeService
    {

        #region Public Methods

        // Avalonia only supports changing Accent at runtime; all other palette properties are read once at startup.
        public static void ApplyFontSizes(double globalFontSize, double dataGridFontSize)
        {
            var app = Application.Current;
            if (app == null) return;

            app.Resources["GlobalFontSize"] = globalFontSize;
            app.Resources["GlobalHeaderFontSize"] = globalFontSize + 2;
            app.Resources["GlobalSmallFontSize"] = Math.Max(8, globalFontSize - 2);
            app.Resources["GlobalMenuFontSize"] = globalFontSize + 1;
            app.Resources["GlobalMenuIconSize"] = globalFontSize + 6;
            app.Resources["GlobalIconSize"] = globalFontSize + 2;
            app.Resources["GlobalIconButtonSize"] = globalFontSize + 10;
            app.Resources["ComboBoxMinHeight"] = globalFontSize + 6;
            app.Resources["DataGridFontSizeResource"] = dataGridFontSize;
            app.Resources["DataGridRowHeight"] = dataGridFontSize + 10;
            app.Resources["DataGridHeaderHeight"] = dataGridFontSize + 8;

        }

        public static void ApplyTheme(int themeIndex, int colorIndex)
        {
            ApplyThemeVariant(themeIndex);
            ApplyAccentColor(colorIndex);
        }

        public static void ApplyThemeVariant(int themeIndex)
        {
            var app = Application.Current;
            if (app == null) return;

            app.RequestedThemeVariant = themeIndex switch
            {
                0 => ThemeVariant.Light,
                1 => ThemeVariant.Dark,
                _ => ThemeVariant.Light
            };

            var iconName = themeIndex == 1 ? "dropicon-white.png" : "dropicon-black.png";
            var uri = new Uri($"avares://Gamelist_Manager/Assets/Icons/{iconName}");
            app.Resources["DropIconImage"] = new Bitmap(AssetLoader.Open(uri));
        }

        public static void ApplyAccentColor(int colorIndex)
        {
            var app = Application.Current;
            if (app == null) return;

            var accentColor = colorIndex switch
            {
                0 => Color.FromRgb(0, 120, 212),  // Blue    (#0078D4)
                1 => Color.FromRgb(231, 72, 86),  // Red     (#E74856)
                2 => Color.FromRgb(255, 140, 0),  // Orange  (#FF8C00)
                3 => Color.FromRgb(16, 124, 16),  // Green   (#107C10)
                4 => Color.FromRgb(255, 185, 0),  // Yellow  (#FFB900)
                5 => Color.FromRgb(227, 0, 140),  // Magenta (#E3008C)
                6 => Color.FromRgb(136, 23, 152),  // Purple  (#881798)
                7 => Color.FromRgb(0, 183, 195),  // Teal    (#00B7C3)
                8 => Color.FromRgb(16, 137, 62),  // Lime    (#10893E)
                9 => Color.FromRgb(0, 188, 242),  // Lt Blue (#00BCF2)
                10 => Color.FromRgb(92, 45, 145),  // Indigo  (#5C2D91)
                _ => Color.FromRgb(0, 120, 212)   // Default Blue
            };

            // Update FluentTheme palette Accent (the only property that supports runtime changes)
            foreach (var style in app.Styles)
            {
                if (style is FluentTheme fluentTheme)
                {
                    if (fluentTheme.Palettes != null)
                    {
                        foreach (var palette in fluentTheme.Palettes)
                            palette.Value.Accent = accentColor;
                    }
                    break;
                }
            }

            // Update SystemAccentColor* resources used by ThemeResources.axaml custom brushes
            // Light1/Light2 are lighter (higher lightness), Dark1 is darker — derived via HSL
            app.Resources["SystemAccentColor"] = accentColor;
            app.Resources["SystemAccentColorLight1"] = ShiftLightness(accentColor, +0.15f);
            app.Resources["SystemAccentColorLight2"] = ShiftLightness(accentColor, +0.30f);
            app.Resources["SystemAccentColorDark1"] = ShiftLightness(accentColor, -0.15f);
        }

        #region Private Methods

        private static Color ShiftLightness(Color color, float delta)
        {
            RgbToHsl(color.R, color.G, color.B, out float h, out float s, out float l);
            l = Math.Clamp(l + delta, 0f, 1f);
            HslToRgb(h, s, l, out byte r, out byte g, out byte b);
            return Color.FromRgb(r, g, b);
        }

        private static void RgbToHsl(byte r, byte g, byte b, out float h, out float s, out float l)
        {
            float rf = r / 255f, gf = g / 255f, bf = b / 255f;
            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            float delta = max - min;

            l = (max + min) / 2f;

            if (delta == 0f) { h = 0f; s = 0f; return; }

            s = l < 0.5f ? delta / (max + min) : delta / (2f - max - min);

            float hRaw = max == rf ? (gf - bf) / delta + (gf < bf ? 6f : 0f)
                       : max == gf ? (bf - rf) / delta + 2f
                       : (rf - gf) / delta + 4f;
            h = hRaw / 6f;
        }

        private static void HslToRgb(float h, float s, float l, out byte r, out byte g, out byte b)
        {
            if (s == 0f)
            {
                r = g = b = (byte)(l * 255f);
                return;
            }
            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;
            r = (byte)(HueToRgb(p, q, h + 1f / 3f) * 255f);
            g = (byte)(HueToRgb(p, q, h) * 255f);
            b = (byte)(HueToRgb(p, q, h - 1f / 3f) * 255f);
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            return t < 1f / 6f ? p + (q - p) * 6f * t
                 : t < 1f / 2f ? q
                 : t < 2f / 3f ? p + (q - p) * (2f / 3f - t) * 6f
                 : p;
        }

        #endregion

        public static string GetThemeVariantName(int themeIndex)
        {
            return themeIndex switch
            {
                0 => "Light",
                1 => "Dark",
                _ => "Light"
            };
        }

        public static string GetAccentColorName(int colorIndex)
        {
            return colorIndex switch
            {
                0 => "Blue",
                1 => "Red",
                2 => "Orange",
                3 => "Green",
                4 => "Yellow",
                5 => "Magenta",
                6 => "Purple",
                7 => "Teal",
                8 => "Lime",
                9 => "Light Blue",
                10 => "Indigo",
                _ => "Blue"
            };
        }

        public static int GetThemeIndex(string themeName)
        {
            return themeName?.ToLower() switch
            {
                "light" => 0,
                "dark" => 1,
                _ => 0
            };
        }

        public static int GetColorIndex(string colorName)
        {
            return colorName?.ToLower() switch
            {
                "blue" => 0,
                "red" => 1,
                "orange" => 2,
                "green" => 3,
                "yellow" => 4,
                "magenta" => 5,
                "purple" => 6,
                "teal" => 7,
                "lime" => 8,
                "light blue" => 9,
                "lightblue" => 9,
                "indigo" => 10,
                _ => 0
            };
        }

        public static void ApplyDataGridAppearance(DataGrid? dataGrid, int alternatingRowColorIndex, int gridLinesVisibilityIndex)
        {
            if (dataGrid == null) return;

            // Apply Grid Lines Visibility
            dataGrid.GridLinesVisibility = gridLinesVisibilityIndex switch
            {
                0 => DataGridGridLinesVisibility.Horizontal,
                1 => DataGridGridLinesVisibility.Vertical,
                2 => DataGridGridLinesVisibility.All,
                3 => DataGridGridLinesVisibility.None,
                _ => DataGridGridLinesVisibility.Horizontal
            };

            // Apply Alternating Row Color using proper Avalonia DataGrid property
            var alternatingBrush = alternatingRowColorIndex switch
            {
                // None
                0 => null,

                // Light Colors (1-9) - Good for Light Theme
                1 => new SolidColorBrush(Color.FromRgb(245, 245, 245)), // Light Gray
                2 => new SolidColorBrush(Color.FromRgb(240, 248, 255)), // Light Blue (Alice Blue)
                3 => new SolidColorBrush(Color.FromRgb(255, 250, 205)), // Light Yellow (Lemon Chiffon)
                4 => new SolidColorBrush(Color.FromRgb(240, 255, 240)), // Light Green (Honeydew)
                5 => new SolidColorBrush(Color.FromRgb(255, 228, 225)), // Light Pink (Misty Rose)
                6 => new SolidColorBrush(Color.FromRgb(255, 250, 240)), // Light Ivory (Floral White)
                7 => new SolidColorBrush(Color.FromRgb(240, 255, 255)), // Light Cyan (Azure)
                8 => new SolidColorBrush(Color.FromRgb(255, 248, 220)), // Light Cream (Cornsilk)
                9 => new SolidColorBrush(Color.FromRgb(255, 245, 238)), // Light Peach (Seashell)

                // Medium Colors (10-15) - Good for Light Theme
                10 => new SolidColorBrush(Color.FromRgb(211, 211, 211)), // Medium Gray (Light Gray)
                11 => new SolidColorBrush(Color.FromRgb(173, 216, 230)), // Medium Blue (Light Blue)
                12 => new SolidColorBrush(Color.FromRgb(144, 238, 144)), // Medium Green (Light Green)
                13 => new SolidColorBrush(Color.FromRgb(255, 255, 153)), // Medium Yellow (Light Yellow)
                14 => new SolidColorBrush(Color.FromRgb(255, 182, 193)), // Medium Pink (Light Pink)
                15 => new SolidColorBrush(Color.FromRgb(216, 191, 216)), // Medium Lavender (Thistle)

                // Dark Colors (16-21) - Good for Dark Theme
                16 => new SolidColorBrush(Color.FromRgb(45, 45, 45)),   // Dark Gray
                17 => new SolidColorBrush(Color.FromRgb(25, 35, 50)),   // Dark Blue
                18 => new SolidColorBrush(Color.FromRgb(25, 45, 35)),   // Dark Green
                19 => new SolidColorBrush(Color.FromRgb(25, 45, 45)),   // Dark Teal
                20 => new SolidColorBrush(Color.FromRgb(40, 30, 50)),   // Dark Purple
                21 => new SolidColorBrush(Color.FromRgb(45, 35, 30)),   // Dark Brown

                _ => null
            };

            // Remove any existing alternating row styles first
            var stylesToRemove = dataGrid.Styles
                .Where(s => s is Style style && style.Selector?.ToString()?.Contains("DataGridRow:nth-child") == true)
                .ToList();

            foreach (var style in stylesToRemove)
            {
                dataGrid.Styles.Remove(style);
            }

            // Apply new style only if not "None"
            if (alternatingBrush != null)
            {
                // Create a new style for alternating rows (even rows)
                var alternatingRowStyle = new Style(x => x.OfType<DataGridRow>().NthChild(2, 0));
                alternatingRowStyle.Setters.Add(new Setter(DataGridRow.BackgroundProperty, alternatingBrush));
                dataGrid.Styles.Add(alternatingRowStyle);
            }
        }

        public static void ApplyDataGridColumnWidths(DataGrid? dataGrid, double dataGridFontSize)
        {
            if (dataGrid == null) return;

            const double baseFontSize = 12;
            var scale = dataGridFontSize / baseFontSize;

            foreach (var column in dataGrid.Columns)
            {
                var header = column.Header?.ToString();
                switch (header)
                {
                    case "Hidden":
                        column.Width = new DataGridLength(Math.Round(62 * scale));
                        break;
                    case "Favorite":
                        column.Width = new DataGridLength(Math.Round(65 * scale));
                        break;
                }
            }
        }

        public static DataGridGridLinesVisibility GetGridLinesVisibility(int index)
        {
            return index switch
            {
                0 => DataGridGridLinesVisibility.Horizontal,
                1 => DataGridGridLinesVisibility.Vertical,
                2 => DataGridGridLinesVisibility.All,
                3 => DataGridGridLinesVisibility.None,
                _ => DataGridGridLinesVisibility.Horizontal
            };
        }

        #endregion
    }
}
