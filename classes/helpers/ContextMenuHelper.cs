using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GamelistManager.classes.helpers
{
    public static class ContextMenuHelper
    {
        // Creates a reusable context menu
        public static ContextMenu CreateContextMenu()
        {
            ContextMenu contextMenu = new();

            // Apply BaseContextMenu style
            contextMenu.Style = Application.Current.FindResource("BaseContextMenu") as Style;

            // Get the MinimalContextMenuItem style
            var menuItemStyle = Application.Current.FindResource("MinimalContextMenuItem") as Style;

            // Add the Open menu item
            MenuItem openItem = new() { Header = "Open", Style = menuItemStyle };
            openItem.Click += MenuItem_Click;

            // Add the Open With menu item
            MenuItem openWithItem = new() { Header = "Open With", Style = menuItemStyle };
            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(openWithItem);

            // Add the Open File Location menu item
            MenuItem openLocationItem = new() { Header = "Open File Location", Style = menuItemStyle };
            openLocationItem.Click += MenuItem_Click;
            contextMenu.Items.Add(openLocationItem);

            // Add the Properties menu item
            MenuItem propertiesItem = new() { Header = "Properties", Style = menuItemStyle };
            propertiesItem.Click += MenuItem_Click;
            contextMenu.Items.Add(propertiesItem);

            // Attach the event handler for when the context menu is opened
            contextMenu.Opened += (sender, args) =>
            {
                PopulateOpenWithSubmenu(openWithItem, contextMenu);
            };

            return contextMenu;
        }

        // Generic click handler for all menu items
        private static void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? menuItem = sender as MenuItem;
            ContextMenu? contextMenu = menuItem?.Parent as ContextMenu;
            Image? parentImage = contextMenu?.PlacementTarget as Image;
            string? filePath = parentImage?.Tag as string;

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                switch (menuItem?.Header.ToString())
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
            else
            {
                MessageBox.Show("File not found or invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void PopulateOpenWithSubmenu(MenuItem openWithMenu, ContextMenu contextMenu)
        {
            // Clear any existing submenu items
            openWithMenu.Items.Clear();

            // Retrieve the file path from the parent context menu
            Image? parentImage = contextMenu?.PlacementTarget as Image;
            string? filePath = parentImage?.Tag as string;

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
                            // ✅ UWP app: Launch using AppUserModelID
                            UwpAppHelper.LaunchAppWithFile(command, targetFile, null);
                        }
                        else
                        {
                            // ✅ Win32 app: Start the process with the file as argument
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

        // Opens the file with its default application
        private static void OpenFile(string filePath)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }

        // Opens the folder containing the file and highlights it
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