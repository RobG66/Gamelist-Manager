using System.IO;

namespace GamelistManager.classes.helpers
{
    public static class SshKeyHelper
    {
        public static string RemoveBatoceraKey(string hostName = "batocera")
        {
            try
            {
                // Locate the known_hosts file in the user's .ssh directory
                string sshDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
                string knownHostsPath = Path.Combine(sshDir, "known_hosts");

                if (!File.Exists(knownHostsPath))
                {
                    return ("The known_hosts file does not exist.");
                }

                // Read all lines from the known_hosts file
                var lines = File.ReadAllLines(knownHostsPath);

                // Remove lines that contain the specified hostname
                var filteredLines = lines.Where(line => !line.Contains(hostName)).ToArray();

                // Check if any changes were made (i.e., host key was present and removed)
                if (lines.Length == filteredLines.Length)
                {
                    return ($"Host '{hostName}' not found in known_hosts.");
                }

                // Write the updated lines back to the known_hosts file
                File.WriteAllLines(knownHostsPath, filteredLines);

                return ($"Successfully removed the SSH key for host '{hostName}' from known_hosts.");
            }
            catch (Exception ex)
            {
                return ($"An error occurred while removing the key: {ex.Message}");
            }
        }
    }
}