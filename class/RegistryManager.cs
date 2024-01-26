using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

// Misc Registry methods in here

namespace GamelistManager
{
    public static class RegistryManager
    {
       private const string programRegistryKey = @"Software\GamelistManager";
       private const string filenamesKey = "LastFilenames";


        public static void ClearRecentFiles()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(programRegistryKey, true))
                {
                    // Clear the LastFilenames value
                    key.DeleteValue(filenamesKey, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing recent files in registry: {ex.Message}");
            }
        }

        public static void SaveLastOpenedGamelistName(string lastFileName)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(programRegistryKey, true))
                {
                    string existingFilenamesString = key?.GetValue(filenamesKey) as string;

                    int maxFiles = 10;

                    List<string> lastFileNamesList = !string.IsNullOrEmpty(existingFilenamesString)
                        ? existingFilenamesString.Split(',').ToList()
                        : new List<string>();

                    // Check if the new filename is already in the list
                    bool filenameExists = lastFileNamesList.Any(filename => string.Equals(filename, lastFileName, StringComparison.OrdinalIgnoreCase));

                    if (!filenameExists)
                    {
                        lastFileNamesList.Insert(0, lastFileName);
                        if (lastFileNamesList.Count > maxFiles)
                        {
                            lastFileNamesList.RemoveAt(lastFileNamesList.Count - 1);
                        }
                    }
                    else
                    {
                        // Move the existing filename to position 0
                        lastFileNamesList.Remove(lastFileName);
                        lastFileNamesList.Insert(0, lastFileName);
                    }

                    // Combine the list into a string
                    string filenamesString = string.Join(",", lastFileNamesList);

                    // Save the updated string to the registry
                    key?.SetValue(filenamesKey, filenamesString);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving last filename to registry: {ex.Message}");
            }
        }

        public static List<string> GetRecentFiles()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(programRegistryKey))
                {
                    if (key != null)
                    {
                        string filenamesString = key.GetValue(filenamesKey) as string;

                        if (!string.IsNullOrEmpty(filenamesString))
                        {
                            return filenamesString.Split(',').ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading last filenames from registry: {ex.Message}");
            }

            // Returns a list, but it is empty
            return new List<string>();
        }

        public static void SaveRegistryValue(string valueName, string value)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(programRegistryKey))
                {
                    if (key != null)
                    {
                        key.SetValue(valueName, value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to the registry: {ex.Message}");
                // Handle the exception as needed
            }
        }

        public static string ReadRegistryValue(string valueName)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(programRegistryKey))
                {
                    if (key != null)
                    {
                        // If the value doesn't exist, this will return null
                        return key.GetValue(valueName) as string;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading from the registry: {ex.Message}");
                // Handle the exception as needed
            }

            return string.Empty;
        }

    }
}
