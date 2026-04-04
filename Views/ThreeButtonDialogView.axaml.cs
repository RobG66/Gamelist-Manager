using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Gamelist_Manager.Views
{
    public partial class ThreeButtonDialogView : Window
    {
        public static async Task<ThreeButtonResult> ShowAsync(ThreeButtonDialogConfig config, Window? owner = null)
        {
            owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner == null) return ThreeButtonResult.Button1;
            return await new ThreeButtonDialogView(config).ShowDialog<ThreeButtonResult>(owner);
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

        private void ApplyIconTheme(DialogIconTheme theme)
        {
            string assetPath = theme switch
            {
                DialogIconTheme.Warning => "avares://Gamelist_Manager/Assets/Icons/warning-triangle.png",
                DialogIconTheme.Info    => "avares://Gamelist_Manager/Assets/Icons/info-circle.png",
                DialogIconTheme.Error   => "avares://Gamelist_Manager/Assets/Icons/error-circle.png",
                _                      => "avares://Gamelist_Manager/Assets/Icons/question-circle.png"
            };

            var uri = new Uri(assetPath);
            var bitmap = new Bitmap(AssetLoader.Open(uri));
            IconDisplay.Source = bitmap;

            string borderClass = theme switch
            {
                DialogIconTheme.Warning => "warning-icon",
                DialogIconTheme.Info    => "info-icon",
                DialogIconTheme.Error   => "error-icon",
                _                      => "question-icon"
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
