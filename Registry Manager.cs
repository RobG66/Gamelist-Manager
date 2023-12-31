using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GamelistManager
{
    public static class RegistryManager
    {
        private const string RegistryKey = @"Software\GamelistManager";
        private const string LastFilenamesValueName = "LastFilenames";

        public static void ClearRecentFiles()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKey, true))
                {
                    // Clear the LastFilenames value
                    key.DeleteValue(LastFilenamesValueName, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing recent files in registry: {ex.Message}");
            }
        }

        public static void SaveLastFilename(string newFilename)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKey, true))
                {
                    string existingFilenamesString = key?.GetValue(LastFilenamesValueName) as string;

                    List<string> existingFilenames = !string.IsNullOrEmpty(existingFilenamesString)
                        ? existingFilenamesString.Split(',').ToList()
                        : new List<string>();

                    // Check if the new filename is already in the list
                    bool filenameExists = existingFilenames.Any(filename => string.Equals(filename, newFilename, StringComparison.OrdinalIgnoreCase));

                    if (!filenameExists)
                    {
                        existingFilenames.Insert(0, newFilename);

                        if (existingFilenames.Count > 10)
                        {
                            existingFilenames.RemoveAt(existingFilenames.Count - 1);
                        }

                        string filenamesString = string.Join(",", existingFilenames);

                        key?.SetValue(LastFilenamesValueName, filenamesString);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving last filename to registry: {ex.Message}");
            }
        }
        public static List<string> LoadLastFilenames()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKey))
                {
                    if (key != null)
                    {
                        string filenamesString = key.GetValue(LastFilenamesValueName) as string;

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

            return new List<string>();
        }
    }
}
