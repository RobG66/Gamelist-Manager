using System;
using System.Linq;

namespace GamelistManager
{
    using System.IO;

    public class NetworkShareChecker
    {
        public static bool IsNetworkShareMapped(string networkShare)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            // Check if any drive is already mapped to the specified network share
            return drives.Any(drive => IsDriveMappedToNetworkShare(drive.Name, networkShare));
        }

        private static bool IsDriveMappedToNetworkShare(string driveLetter, string expectedNetworkShare)
        {
            DriveInfo drive = new DriveInfo(driveLetter);

            // Check if the drive is a network drive and if it points to the expected network share
            if (drive.DriveType == DriveType.Network && drive.IsReady)
            {
                string actualNetworkShare = GetNetworkShareFromDriveMapping(driveLetter);
                return string.Equals(actualNetworkShare, expectedNetworkShare, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static string GetNetworkShareFromDriveMapping(string driveLetter)
        {
            // Get the network share path from the drive mapping
            DriveInfo drive = new DriveInfo(driveLetter);
            if (drive.DriveType == DriveType.Network && drive.IsReady)
            {
                return drive.VolumeLabel; // You may need to adjust this based on how your network shares are labeled
            }

            return null;
        }
    }

}
