using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;


namespace GamelistManager
{
    internal static class RegistryManager
    {
        private const string programRegistryKey = @"Software\GamelistManager";
        private const string filenamesKey = "LastFilenames";

        public static void ClearRecentFiles()
        {
            try
            {
                CreateRegistryKeyIfNotExist(programRegistryKey);

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(programRegistryKey, writable: true))
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
                CreateRegistryKeyIfNotExist(programRegistryKey);

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(programRegistryKey, writable: true))
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
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(programRegistryKey, writable: false))
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

        public static void WriteRegistryValue(string subkey, string valueName, string value)
        {
            string regkey = programRegistryKey;

            try
            {
                if (!string.IsNullOrEmpty(subkey))
                {
                    regkey = programRegistryKey + "\\" + subkey;
                }

                CreateRegistryKeyIfNotExist(regkey);

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regkey, writable: true))
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

        public static string ReadRegistryValue(string platform, string valueName)
        {
            string regkey = programRegistryKey;

            try
            {
                if (!string.IsNullOrEmpty(platform))
                {
                    regkey = programRegistryKey + "\\" + platform;
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regkey, writable: false))
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

        static void CreateRegistryKeyIfNotExist(string subKeyPath)
        {
            // Open the base registry key
            RegistryKey baseKey = Registry.CurrentUser;

            // Try to open the specified sub key
            RegistryKey subKey = baseKey.OpenSubKey(subKeyPath, writable: true);

            // Check if the sub key exists
            if (subKey == null)
            {
                // The sub key does not exist, so create it
                Console.WriteLine($"Creating registry key: {subKeyPath}");
                subKey = baseKey.CreateSubKey(subKeyPath);
                if (subKey != null)
                {
                    Console.WriteLine($"Registry key created successfully: {subKeyPath}");
                }
                else
                {
                    Console.WriteLine($"Failed to create registry key: {subKeyPath}");
                }
            }
            else
            {
                Console.WriteLine($"Registry key already exists: {subKeyPath}");
            }

            // Close the sub key
            subKey?.Close();
        }

    }
}
