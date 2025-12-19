using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GamelistManager.classes.helpers
{
    public static class UIHelper
    {
        // Creates a reusable context menu for a given file path
        public static ContextMenu CreateContextMenu(string filePath)
        {
            ContextMenu contextMenu = new();

            // Apply BaseContextMenu style
            contextMenu.Style = Application.Current.FindResource("BaseContextMenu") as Style;

            // Get the MinimalContextMenuItem style
            var menuItemStyle = Application.Current.FindResource("MinimalContextMenuItem") as Style;

            // Add the Open menu item
            MenuItem openItem = new() { Header = "Open", Style = menuItemStyle, Tag = filePath };
            openItem.Click += MenuItem_Click;

            // Add the Open With menu item
            MenuItem openWithItem = new() { Header = "Open With", Style = menuItemStyle };

            // Add the Open File Location menu item
            MenuItem openLocationItem = new() { Header = "Open File Location", Style = menuItemStyle, Tag = filePath };
            openLocationItem.Click += MenuItem_Click;

            // Add the Properties menu item
            MenuItem propertiesItem = new() { Header = "Properties", Style = menuItemStyle, Tag = filePath };
            propertiesItem.Click += MenuItem_Click;

            // Add items to context menu
            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(openWithItem);
            contextMenu.Items.Add(openLocationItem);
            contextMenu.Items.Add(propertiesItem);

            // Populate Open With submenu when opened
            contextMenu.Opened += (sender, args) =>
            {
                PopulateOpenWithSubmenu(openWithItem, filePath);
            };

            return contextMenu;
        }
        private static void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;

            string? filePath = menuItem.Tag as string;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("File not found or invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            switch (menuItem.Header?.ToString())
            {
                case "Open":
                    OpenFile(filePath);
                    break;
                case "Open File Location":
                    OpenFileLocation(filePath);
                    break;
                case "Properties":
                    PropertiesHelper.Show(filePath);
                    break;
            }
        }

        private static void PopulateOpenWithSubmenu(MenuItem openWithMenu, string filePath)
        {
            openWithMenu.Items.Clear();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                openWithMenu.Items.Add(new MenuItem { Header = "No file selected", IsEnabled = false });
                return;
            }

            string extension = Path.GetExtension(filePath);
            var apps = AppAssociationHelper.GetAssociatedApps(extension);

            if (apps.Count == 0)
            {
                openWithMenu.Items.Add(new MenuItem { Header = "No associated apps", IsEnabled = false });
                return;
            }

            foreach (var app in apps)
            {
                var item = new MenuItem
                {
                    Header = app.Name,
                    Tag = new Tuple<string, string>(filePath, app.Command)
                };

                item.Click += (s, e) =>
                {
                    var (targetFile, command) = ((Tuple<string, string>)((MenuItem)s).Tag);

                    try
                    {
                        if (!command.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            UwpAppHelper.LaunchAppWithFile(command, targetFile, null);
                        }
                        else
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = command,
                                Arguments = $"\"{targetFile}\"",
                                UseShellExecute = true,
                                CreateNoWindow = true
                            };
                            Process.Start(psi);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to open with {app.Name}.\n\n{ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                };

                openWithMenu.Items.Add(item);
            }
        }

        private static void OpenFile(string filePath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }

        private static void OpenFileLocation(string filePath)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }
    }
}

