using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GamelistManager.classes.helpers
{
    public static class UIHelper
    {
        public static ContextMenu CreateContextMenu(
          string filePath,
          Action<string>? onScrapeRequested = null,
          Action? onClearRequested = null,
          Action? onDeleteRequested = null)
        {
            ContextMenu contextMenu = new();

            contextMenu.Style =
                Application.Current.FindResource("BaseContextMenu") as Style;

            var menuItemStyle =
                Application.Current.FindResource("MinimalContextMenuItem") as Style;

            bool fileExists = !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
            bool hasFilePath = !string.IsNullOrEmpty(filePath);

            // Only add file operation items if we have a file path
            MenuItem openWithItem = null; // Declare here so we can reference it later

            if (hasFilePath)
            {
                // Open
                MenuItem openItem = new()
                {
                    Header = "Open",
                    Style = menuItemStyle,
                    Tag = filePath,
                    IsEnabled = fileExists
                };
                openItem.Click += MenuItem_Click;

                // Open With
                openWithItem = new()
                {
                    Header = "Open With",
                    Style = menuItemStyle,
                    IsEnabled = fileExists
                };

                // Open File Location
                MenuItem openLocationItem = new()
                {
                    Header = "Open File Location",
                    Style = menuItemStyle,
                    Tag = filePath,
                    IsEnabled = fileExists
                };
                openLocationItem.Click += MenuItem_Click;

                // Properties
                MenuItem propertiesItem = new()
                {
                    Header = "Properties",
                    Style = menuItemStyle,
                    Tag = filePath,
                    IsEnabled = fileExists
                };
                propertiesItem.Click += MenuItem_Click;

                contextMenu.Items.Add(openItem);
                contextMenu.Items.Add(openWithItem);
                contextMenu.Items.Add(openLocationItem);
                contextMenu.Items.Add(propertiesItem);
            }

            // ----- Clear / Delete -----
            if (onClearRequested != null || onDeleteRequested != null)
            {
                contextMenu.Items.Add(new Separator());

                if (onClearRequested != null)
                {
                    MenuItem clearItem = new()
                    {
                        Header = "Clear",
                        Style = menuItemStyle
                    };

                    clearItem.Click += (s, e) =>
                    {
                        onClearRequested();
                    };

                    contextMenu.Items.Add(clearItem);
                }

                if (onDeleteRequested != null)
                {
                    MenuItem deleteItem = new()
                    {
                        Header = "Delete",
                        Style = menuItemStyle,
                        IsEnabled = fileExists
                    };

                    deleteItem.Click += (s, e) =>
                    {
                        // The callback handles all the deletion logic
                        // including confirmation, file deletion, and path clearing
                        onDeleteRequested();
                    };

                    contextMenu.Items.Add(deleteItem);
                }
            }

            // ----- Scrape submenu -----
            if (onScrapeRequested != null)
            {
                contextMenu.Items.Add(new Separator());

                MenuItem scrapeItem = new()
                {
                    Header = "Scrape This Item",
                    Style = menuItemStyle
                };

                MenuItem arcadeDBItem = new()
                {
                    Header = "ArcadeDB",
                    Style = menuItemStyle
                };
                arcadeDBItem.Click += (s, e) =>
                    onScrapeRequested(filePath + "|ArcadeDB");

                MenuItem screenScraperItem = new()
                {
                    Header = "ScreenScraper",
                    Style = menuItemStyle
                };
                screenScraperItem.Click += (s, e) =>
                    onScrapeRequested(filePath + "|ScreenScraper");

                MenuItem emuMoviesItem = new()
                {
                    Header = "EmuMovies",
                    Style = menuItemStyle
                };
                emuMoviesItem.Click += (s, e) =>
                    onScrapeRequested(filePath + "|EmuMovies");

                scrapeItem.Items.Add(arcadeDBItem);
                scrapeItem.Items.Add(screenScraperItem);
                scrapeItem.Items.Add(emuMoviesItem);

                contextMenu.Items.Add(scrapeItem);
            }

            // Populate Open With dynamically (only if we have a file path)
            if (openWithItem != null)
            {
                contextMenu.Opened += (sender, args) =>
                {
                    PopulateOpenWithSubmenu(openWithItem, filePath);
                };
            }

            return contextMenu;
        }

        private static void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;

            string? filePath = menuItem.Tag as string;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show(
                    "File not found or invalid.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                openWithMenu.Items.Add(
                    new MenuItem { Header = "No file selected", IsEnabled = false });
                return;
            }

            string extension = Path.GetExtension(filePath);
            var apps = AppAssociationHelper.GetAssociatedApps(extension);

            if (apps.Count == 0)
            {
                openWithMenu.Items.Add(
                    new MenuItem { Header = "No associated apps", IsEnabled = false });
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
                    var (targetFile, command) =
                        (Tuple<string, string>)((MenuItem)s).Tag;

                    try
                    {
                        if (!command.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            UwpAppHelper.LaunchAppWithFile(
                                command, targetFile, null);
                        }
                        else
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = command,
                                Arguments = $"\"{targetFile}\"",
                                UseShellExecute = true,
                                CreateNoWindow = true
                            });
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
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open file.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void OpenFileLocation(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open file location.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public static ContextMenu CreateScraperMenu(Action<string> onScraperSelected)
        {
            ContextMenu contextMenu = new();

            contextMenu.Style =
                Application.Current.FindResource("BaseContextMenu") as Style;

            var menuItemStyle =
                Application.Current.FindResource("MinimalContextMenuItem") as Style;

            // ArcadeDB option
            MenuItem arcadeDBItem = new()
            {
                Header = "ArcadeDB",
                Style = menuItemStyle
            };
            arcadeDBItem.Click += (s, e) => onScraperSelected("ArcadeDB");

            // ScreenScraper option
            MenuItem screenScraperItem = new()
            {
                Header = "ScreenScraper",
                Style = menuItemStyle
            };
            screenScraperItem.Click += (s, e) => onScraperSelected("ScreenScraper");

            // EmuMovies option
            MenuItem emuMoviesItem = new()
            {
                Header = "EmuMovies",
                Style = menuItemStyle
            };
            emuMoviesItem.Click += (s, e) => onScraperSelected("EmuMovies");

            contextMenu.Items.Add(arcadeDBItem);
            contextMenu.Items.Add(screenScraperItem);
            contextMenu.Items.Add(emuMoviesItem);

            return contextMenu;
        }

    }
}