using Microsoft.Win32;
using System.IO;

public class AssociatedApp
{
    public string Name { get; set; }
    public string Command { get; set; } // full execution string
}

public static class AppAssociationHelper
{
    public static List<AssociatedApp> GetAssociatedApps(string extension)
    {
        var apps = new List<AssociatedApp>();
        string fileExtPath = $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}";

        using (RegistryKey extKey = Registry.CurrentUser.OpenSubKey(fileExtPath))
        {
            if (extKey == null)
                return apps;

            // 1. OpenWithList: List of executable apps associated with the extension
            using (RegistryKey openWithList = extKey.OpenSubKey("OpenWithList"))
            {
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
        }

        return apps;
    }

    // Helper method to get the friendly name of a UWP app from the registry
    private static string GetUwpAppFriendlyName(string appUserModelId)
    {
        try
        {
            // Parse the appUserModelId to extract the app name before the underscore
            string appName = appUserModelId.Split('_')[0];

            return appName ?? "Unknown UWP App";
        }
        catch
        {
            return "Unknown UWP App";
        }
    }

    // Helper method to get the friendly name of a Win32 app from the registry
    private static string GetWin32AppFriendlyName(string exePath)
    {
        string exeName = Path.GetFileNameWithoutExtension(exePath);
        string appRegistryPath = $@"HKEY_CLASSES_ROOT\Applications\{exeName}\shell\open\command";

        // Check the registry for the friendly name or details
        using (RegistryKey appKey = Registry.ClassesRoot.OpenSubKey(appRegistryPath))
        {
            if (appKey != null)
            {
                string friendlyName = appKey.GetValue(null)?.ToString(); // Default value usually contains the friendly name
                if (!string.IsNullOrEmpty(friendlyName))
                {
                    return friendlyName;
                }
            }
        }

        // If no registry name is found, fall back to the exe name
        return CapitalizeFirstLetter(exeName);
    }

    // Capitalizes the first letter of a string
    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        char firstLetter = char.ToUpper(input[0]);
        return firstLetter + input.Substring(1);
    }
}
