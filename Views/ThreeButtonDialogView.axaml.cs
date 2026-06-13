using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Threading.Tasks;

namespace Gamelist_Manager.Views
{
    public partial class ThreeButtonDialogView : Window
    {
        public static async Task<ThreeButtonResult> ShowAsync(ThreeButtonDialogConfig config, object? owner = null)
        {
            var windowOwner = owner as Window ?? (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (windowOwner == null) return ThreeButtonResult.Button1;
            return await new ThreeButtonDialogView(config).ShowDialog<ThreeButtonResult>(windowOwner);
        }

        public static Task ShowInfoAsync(string title, string message, string? detail = null, object? owner = null)
            => ShowAsync(new ThreeButtonDialogConfig
            {
                Title = title,
                Message = message,
                DetailMessage = detail,
                IconTheme = DialogIconTheme.Info,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK",
            }, owner);

        public static Task ShowWarningAsync(string title, string message, string? detail = null, object? owner = null)
            => ShowAsync(new ThreeButtonDialogConfig
            {
                Title = title,
                Message = message,
                DetailMessage = detail,
                IconTheme = DialogIconTheme.Warning,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK",
            }, owner);

        public static Task ShowErrorAsync(string title, string message, string? detail = null, object? owner = null)
            => ShowAsync(new ThreeButtonDialogConfig
            {
                Title = title,
                Message = message,
                DetailMessage = detail,
                IconTheme = DialogIconTheme.Error,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK",
            }, owner);

        /// <summary>
        /// Shows a two-button confirmation dialog. Returns <c>true</c> if the user confirmed, <c>false</c> if cancelled.
        /// </summary>
        public static async Task<bool> ShowConfirmAsync(
            string title,
            string message,
            string confirmText = "Yes",
            string cancelText = "Cancel",
            DialogIconTheme icon = DialogIconTheme.Question,
            string? detail = null,
            object? owner = null)
        {
            var result = await ShowAsync(new ThreeButtonDialogConfig
            {
                Title = title,
                Message = message,
                DetailMessage = detail,
                IconTheme = icon,
                Button1Text = cancelText,
                Button2Text = "",
                Button3Text = confirmText,
            }, owner);
            return result == ThreeButtonResult.Button3;
        }

        private readonly ThreeButtonResult _button1Result;
        private readonly ThreeButtonResult _button2Result;
        private readonly ThreeButtonResult _button3Result;

        public ThreeButtonDialogView() : this(new ThreeButtonDialogConfig()) { }

        public ThreeButtonDialogView(ThreeButtonDialogConfig config)
        {
            InitializeComponent();

            _button1Result = config.Button1Result;
            _button2Result = config.Button2Result;
            _button3Result = config.Button3Result;

            // Set window title
            Title = config.Title;

            // Set message texts
            MessageText.Text = config.Message;

            if (!string.IsNullOrEmpty(config.DetailMessage))
            {
                DetailText.Text = config.DetailMessage;
                DetailText.IsVisible = true;
            }

            // Apply icon theme
            ApplyIconTheme(config.IconTheme);

            // Set button texts and visibility
            if (!string.IsNullOrEmpty(config.Button1Text))
            {
                Button1.Content = config.Button1Text;
                Button1.IsVisible = true;
            }
            else
                Button1.IsVisible = false;

            if (!string.IsNullOrEmpty(config.Button2Text))
            {
                Button2.Content = config.Button2Text;
                Button2.IsVisible = true;
            }
            else
                Button2.IsVisible = false;

            if (!string.IsNullOrEmpty(config.Button3Text))
            {
                Button3.Content = config.Button3Text;
                Button3.IsVisible = true;
            }
            else
                Button3.IsVisible = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key is not (Key.Enter or Key.Escape))
                return;

            // Only act when exactly one button is visible — keyboard dismissal is
            // ambiguous when multiple choices are present.
            int visibleCount = (Button1.IsVisible ? 1 : 0)
                             + (Button2.IsVisible ? 1 : 0)
                             + (Button3.IsVisible ? 1 : 0);

            if (visibleCount != 1)
                return;

            if (Button1.IsVisible) Close(_button1Result);
            else if (Button2.IsVisible) Close(_button2Result);
            else Close(_button3Result);

            e.Handled = true;
        }

        private void ApplyIconTheme(DialogIconTheme theme)
        {
            string assetPath = theme switch
            {
                DialogIconTheme.Warning => "avares://Gamelist_Manager/Assets/Icons/warning-triangle.png",
                DialogIconTheme.Info => "avares://Gamelist_Manager/Assets/Icons/info-circle.png",
                DialogIconTheme.Error => "avares://Gamelist_Manager/Assets/Icons/error-circle.png",
                _ => "avares://Gamelist_Manager/Assets/Icons/question-circle.png"
            };

            var uri = new Uri(assetPath);
            var bitmap = new Bitmap(AssetLoader.Open(uri));
            IconDisplay.Source = bitmap;

            string borderClass = theme switch
            {
                DialogIconTheme.Warning => "warning-icon",
                DialogIconTheme.Info => "info-icon",
                DialogIconTheme.Error => "error-icon",
                _ => "question-icon"
            };
            IconBorder.Classes.Add(borderClass);
            DetailText.Classes.Add(borderClass);
        }

        private void Button1_Click(object? sender, RoutedEventArgs e)
        {
            Close(_button1Result);
        }

        private void Button2_Click(object? sender, RoutedEventArgs e)
        {
            Close(_button2Result);
        }

        private void Button3_Click(object? sender, RoutedEventArgs e)
        {
            Close(_button3Result);
        }
    }
}
