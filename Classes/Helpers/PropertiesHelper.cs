using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Gamelist_Manager.Classes.Helpers
{
    public static class PropertiesHelper
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, string pszObjectName, string pszPropertyPage);

        private const uint SHOP_FILEPATH = 0x2;

        public static void Show(string filePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                return;

            try
            {
                SHObjectProperties(IntPtr.Zero, SHOP_FILEPATH, filePath, null!);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to show properties: {ex.Message}");
            }
        }
    }
}
