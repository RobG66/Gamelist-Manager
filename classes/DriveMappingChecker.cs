using System.Runtime.InteropServices;
using System.Windows;

namespace GamelistManager.classes
{
    public static class DriveMappingChecker
    {
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        public static extern int WNetGetConnection(
            [MarshalAs(UnmanagedType.LPTStr)] string localName,
            [MarshalAs(UnmanagedType.LPTStr)] System.Text.StringBuilder remoteName,
            ref int length);

        public static bool IsShareMapped(string networkPath)
        {
            try
            {
                // Iterate through drive letters (A-Z) and check if any is mapped to the specified network path
                for (char driveLetter = 'A'; driveLetter <= 'Z'; driveLetter++)
                {
                    string drive = driveLetter + ":";

                    System.Text.StringBuilder remoteName = new System.Text.StringBuilder(256);
                    int length = remoteName.Capacity;

                    // Call WNetGetConnection to get the remote name for the specified drive letter
                    int result = WNetGetConnection(drive, remoteName, ref length);

                    if (result == 0)
                    {
                        // Check if the mapped path matches the desired network path
                        if (string.Equals(remoteName.ToString(), networkPath, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking drive mapping: {ex.Message}");
                return false;
            }
        }
    }

}

