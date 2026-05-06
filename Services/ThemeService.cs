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
        #region Private Fields

        private static readonly (Color Color, string Name)[] AccentColors =
        [
            (Color.FromRgb(0, 120, 212),   "Blue"),
            (Color.FromRgb(231, 72, 86),   "Red"),
            (Color.FromRgb(255, 140, 0),   "Orange"),
            (Color.FromRgb(16, 124, 16),   "Green"),
            (Color.FromRgb(255, 185, 0),   "Yellow"),
            (Color.FromRgb(227, 0, 140),   "Magenta"),
            (Color.FromRgb(136, 23, 152),  "Purple"),
            (Color.FromRgb(0, 183, 195),   "Teal"),
            (Color.FromRgb(16, 137, 62),   "Lime"),
            (Color.FromRgb(0, 188, 242),   "Light Blue"),
            (Color.FromRgb(92, 45, 145),   "Indigo"),
        ];

        private static readonly Color DefaultAccentColor = AccentColors[0].Color;

        // Index 0 = None (null), indices 1–21 map to alternating row brushes.
        private static readonly Color?[] AlternatingRowColors =
        [
            null,                              // 0: None
            Color.FromRgb(245, 245, 245),      // 1: Light Gray
            Color.FromRgb(240, 248, 255),      // 2: Light Blue (Alice Blue)
            Color.FromRgb(255, 250, 205),      // 3: Light Yellow (Lemon Chiffon)
            Color.FromRgb(240, 255, 240),      // 4: Light Green (Honeydew)
            Color.FromRgb(255, 228, 225),      // 5: Light Pink (Misty Rose)
            Color.FromRgb(255, 250, 240),      // 6: Light Ivory (Floral White)
            Color.FromRgb(240, 255, 255),      // 7: Light Cyan (Azure)
            Color.FromRgb(255, 248, 220),      // 8: Light Cream (Cornsilk)
            Color.FromRgb(255, 245, 238),      // 9: Light Peach (Seashell)
            Color.FromRgb(211, 211, 211),      // 10: Medium Gray
            Color.FromRgb(173, 216, 230),      // 11: Medium Blue
            Color.FromRgb(144, 238, 144),      // 12: Medium Green
            Color.FromRgb(255, 255, 153),      // 13: Medium Yellow
            Color.FromRgb(255, 182, 193),      // 14: Medium Pink
            Color.FromRgb(216, 191, 216),      // 15: Medium Lavender (Thistle)
            Color.FromRgb(45, 45, 45),         // 16: Dark Gray
            Color.FromRgb(25, 35, 50),         // 17: Dark Blue
            Color.FromRgb(25, 45, 35),         // 18: Dark Green
            Color.FromRgb(25, 45, 45),         // 19: Dark Teal
            Color.FromRgb(40, 30, 50),         // 20: Dark Purple
            Color.FromRgb(45, 35, 30),         // 21: Dark Brown
        ];

        #endregion

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
            app.Resources["NumericUpDownMinHeight"] = globalFontSize + 6;
            app.Resources["ButtonSpinnerButtonHeight"] = Math.Round((globalFontSize + 6) / 2);
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

            var accentColor = colorIndex >= 0 && colorIndex < AccentColors.Length
                ? AccentColors[colorIndex].Color
                : DefaultAccentColor;

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
            return colorIndex >= 0 && colorIndex < AccentColors.Length
                ? AccentColors[colorIndex].Name
                : AccentColors[0].Name;
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
            for (int i = 0; i < AccentColors.Length; i++)
            {
                if (string.Equals(AccentColors[i].Name, colorName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return 0;
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
            SolidColorBrush? alternatingBrush = null;
            if (alternatingRowColorIndex > 0 && alternatingRowColorIndex < AlternatingRowColors.Length)
            {
                var color = AlternatingRowColors[alternatingRowColorIndex];
                if (color.HasValue)
                    alternatingBrush = new SolidColorBrush(color.Value);
            }

            // Remove any existing alternating row styles first
            var stylesToRemove = dataGrid.Styles
                .Where(s => s is Style style && style.Selector?.ToString()?.Contains("DataGridRow:nth-child") == true)
                .ToList();

            dataGrid.Styles.Clear();

            IBrush targetBrush = alternatingBrush != null ? alternatingBrush : Brushes.Transparent;

            var style = new Style(x => x.OfType<DataGridRow>().NthChild(2, 0));
            style.Setters.Add(new Setter(DataGridRow.BackgroundProperty, targetBrush));
            dataGrid.Styles.Add(style);




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
