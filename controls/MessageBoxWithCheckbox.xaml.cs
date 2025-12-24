using System;
using System.Windows;
using System.Windows.Controls;

namespace GamelistManager.controls
{
    public partial class MessageBoxWithCheckbox : Window
    {
        public bool IsCheckboxChecked { get; private set; }
        public MessageBoxResult Result { get; private set; }

        public MessageBoxWithCheckbox()
        {
            InitializeComponent();
            Result = MessageBoxResult.None;
        }

        private void AddButton(string content, MessageBoxResult result, string? styleName = null)
        {
            var button = new Button
            {
                Content = content,
                Width = 50,
                Height = 20,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(5, 0, 0, 0),
                Tag = result
            };

            if (!string.IsNullOrEmpty(styleName) && Application.Current.Resources.Contains(styleName))
            {
                button.Style = (Style)Application.Current.Resources[styleName];
            }
            else if (Application.Current.Resources.Contains("GreyRoundedButton"))
            {
                button.Style = (Style)Application.Current.Resources["GreyRoundedButton"];
            }

            button.Click += Button_Click;
            ButtonPanel.Children.Add(button);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MessageBoxResult result)
            {
                Result = result;
                IsCheckboxChecked =
                OptionCheckBox.Visibility == Visibility.Visible &&
                OptionCheckBox.IsChecked == true;
                DialogResult = true;
                Close();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // If Result is still None, user closed the window without clicking a button
            if (Result == MessageBoxResult.None)
            {
                Result = MessageBoxResult.Cancel;
                IsCheckboxChecked =
                OptionCheckBox.Visibility == Visibility.Visible &&
                OptionCheckBox.IsChecked == true;
            }
        }

        public static MessageBoxResult Show(
            Window owner,
            string message,
            out bool checkboxChecked,
            string title = "Message",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            string? checkboxText = null,
            bool checkboxDefaultChecked = false,
            string? infoText = null,
            string? warningText = null)
        {
            var dialog = new MessageBoxWithCheckbox
            {
                Owner = owner,
                Title = title
            };

            // Set message
            dialog.MessageText.Text = message;

            // Set icon
            string iconText = icon switch
            {
                MessageBoxImage.Warning or MessageBoxImage.Exclamation => "⚠️",
                MessageBoxImage.Error or MessageBoxImage.Stop => "❌",
                MessageBoxImage.Information or MessageBoxImage.Asterisk => "ℹ️",
                MessageBoxImage.Question => "❓",
                _ => ""
            };

            if (!string.IsNullOrEmpty(iconText))
            {
                dialog.IconText.Text = iconText;
                dialog.IconText.Visibility = Visibility.Visible;
            }

            // Set info text (optional)
            if (!string.IsNullOrEmpty(infoText))
            {
                dialog.InfoText.Text = infoText;
                dialog.InfoText.Visibility = Visibility.Visible;
            }

            // Set warning text (optional)
            if (!string.IsNullOrEmpty(warningText))
            {
                dialog.WarningText.Text = warningText;
                dialog.WarningText.Visibility = Visibility.Visible;
            }

            // Set checkbox (optional)
            if (!string.IsNullOrEmpty(checkboxText))
            {
                dialog.OptionCheckBox.Content = checkboxText;
                dialog.OptionCheckBox.IsChecked = checkboxDefaultChecked;
                dialog.OptionCheckBox.Visibility = Visibility.Visible;
            }

            // Add buttons based on MessageBoxButton enum
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    dialog.AddButton("OK", MessageBoxResult.OK, "GreenRoundedButton");
                    break;
                case MessageBoxButton.OKCancel:
                    dialog.AddButton("OK", MessageBoxResult.OK, "GreenRoundedButton");
                    dialog.AddButton("Cancel", MessageBoxResult.Cancel, null);
                    break;
                case MessageBoxButton.YesNo:
                    dialog.AddButton("Yes", MessageBoxResult.Yes, "GreenRoundedButton");
                    dialog.AddButton("No", MessageBoxResult.No, null);
                    break;
                case MessageBoxButton.YesNoCancel:
                    dialog.AddButton("Yes", MessageBoxResult.Yes, "GreenRoundedButton");
                    dialog.AddButton("No", MessageBoxResult.No, null);
                    dialog.AddButton("Cancel", MessageBoxResult.Cancel, null);
                    break;
            }

            dialog.ShowDialog();
            checkboxChecked = dialog.IsCheckboxChecked;
            return dialog.Result;
        }

        // Overload without checkbox for simpler usage
        public static MessageBoxResult Show(
            Window owner,
            string message,
            string title = "Message",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None)
        {
            return Show(owner, message, out _, title, buttons, icon, null, false, null, null);
        }
    }
}