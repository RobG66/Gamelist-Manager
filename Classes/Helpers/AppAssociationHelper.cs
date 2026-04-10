using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class AppAssociationHelper
    {
        public class AssociatedApp
        {
            public string Name { get; set; } = string.Empty;
            public string Command { get; set; } = string.Empty;
        }

        [SupportedOSPlatform("windows")]
        public static List<AssociatedApp> GetAssociatedApps(string extension)
        {
            var apps = new List<AssociatedApp>();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || string.IsNullOrEmpty(extension))
                return apps;

            // Ensure extension starts with dot
            if (!extension.StartsWith('.'))
                extension = "." + extension;

            string fileExtPath = $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}";

            try
            {
                using var extKey = Registry.CurrentUser.OpenSubKey(fileExtPath);
                if (extKey == null)
                    return apps;

                // OpenWithList: List of executable apps associated with the extension
                using var openWithList = extKey.OpenSubKey("OpenWithList");
                if (openWithList != null)
                {
                    foreach (var valueName in openWithList.GetValueNames())
                    {
                        // Skip MRUList key
                        if (valueName.Equals("MRUList", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var exe = openWithList.GetValue(valueName)?.ToString();
                        if (!string.IsNullOrEmpty(exe))
                        {
                            // If it's a UWP app, fetch its friendly name
                            if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                var appId = exe.Replace("shell:AppsFolder\\", "");
                                string appFriendlyName = GetUwpAppFriendlyName(appId);
                                apps.Add(new AssociatedApp
                                {
                                    Name = appFriendlyName,
                                    Command = exe
                                });
                            }
                            else
                            {
                                // If it's a Win32 app, resolve its name from the registry
                                string appName = GetWin32AppFriendlyName(exe);
                                apps.Add(new AssociatedApp
                                {
                                    Name = appName,
                                    Command = exe
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }

            return apps;
        }

        [SupportedOSPlatform("windows")]
        private static string GetUwpAppFriendlyName(string appUserModelId)
        {
            try
            {
                // Parse the appUserModelId to extract the app name before the underscore
                var parts = appUserModelId.Split('_');
                if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                {
                    return parts[0];
                }
            }
            catch
            {
                // Ignore
            }

            return "Unknown UWP App";
        }

        [SupportedOSPlatform("windows")]
        private static string GetWin32AppFriendlyName(string exePath)
        {
            string exeName = Path.GetFileNameWithoutExtension(exePath);
            string appRegistryPath = $@"Applications\{exePath}";

            try
            {
                // Check the registry for the friendly name
                using var appKey = Registry.ClassesRoot.OpenSubKey(appRegistryPath);
                if (appKey != null)
                {
                    // Try FriendlyAppName first
                    string? friendlyName = appKey.GetValue("FriendlyAppName")?.ToString();
                    if (!string.IsNullOrEmpty(friendlyName))
                        return friendlyName;

                    // Try ApplicationName
                    friendlyName = appKey.GetValue("ApplicationName")?.ToString();
                    if (!string.IsNullOrEmpty(friendlyName))
                        return friendlyName;
                }
            }
            catch
            {
                // Ignore errors
            }

            // If no registry name is found, fall back to the exe name
            return CapitalizeFirstLetter(exeName);
        }

        private static string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            char firstLetter = char.ToUpper(input[0]);
            return firstLetter + input.Substring(1);
        }
    }
}
