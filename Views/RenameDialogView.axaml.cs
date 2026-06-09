using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gamelist_Manager.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Gamelist_Manager.Views
{
    public partial class RenameDialogView : Window
    {
        public static async Task<string?> ShowAsync(string currentName, Window? owner = null)
        {
            owner ??= (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (owner == null) return null;

            var dialog = new RenameDialogView(currentName);
            return await dialog.ShowDialog<string?>(owner);
        }

        private readonly string _currentName;

        public RenameDialogView() : this(string.Empty) { }

        public RenameDialogView(string currentName)
        {
            InitializeComponent();
            _currentName = currentName;
            NewNameTextBox.Text = currentName;

            Loaded += (s, e) =>
            {
                NewNameTextBox.Focus();
                if (!string.IsNullOrEmpty(currentName))
                {
                    NewNameTextBox.SelectionStart = 0;
                    NewNameTextBox.SelectionEnd = currentName.Length;
                }
            };
        }

        private void RenameButton_Click(object? sender, RoutedEventArgs e)
        {
            string newName = NewNameTextBox.Text?.Trim() ?? string.Empty;
            if (ValidateName(newName, out string errorMessage))
            {
                Close(newName);
            }
            else
            {
                ErrorTextBlock.Text = errorMessage;
                ErrorTextBlock.IsVisible = true;
            }
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(null);
        }

        private void NewNameTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            ErrorTextBlock.IsVisible = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter)
            {
                string newName = NewNameTextBox.Text?.Trim() ?? string.Empty;
                if (ValidateName(newName, out string errorMessage))
                {
                    Close(newName);
                }
                else
                {
                    ErrorTextBlock.Text = errorMessage;
                    ErrorTextBlock.IsVisible = true;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close(null);
                e.Handled = true;
            }
        }

        private bool ValidateName(string name, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Profile name cannot be empty.";
                return false;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (name.IndexOfAny(invalidChars) >= 0)
            {
                errorMessage = "Profile name contains invalid characters.";
                return false;
            }

            if (string.Equals(name, _currentName, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "The new name must be different from the current name.";
                return false;
            }

            try
            {
                string path = ProfileService.Instance.GetProfilePath(name);
                if (File.Exists(path))
                {
                    errorMessage = $"A profile named '{name}' already exists.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error validating profile name: {ex.Message}";
                return false;
            }

            return true;
        }
    }
}
