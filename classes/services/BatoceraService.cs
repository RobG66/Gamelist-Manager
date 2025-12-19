using GamelistManager.classes.helpers;
using Renci.SshNet;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GamelistManager.classes.services
{
    public static class BatoceraService
    {
        public static string ExecuteSshCommand(string command)
        {
            string hostName = Properties.Settings.Default.BatoceraHostName;

            if (string.IsNullOrEmpty(hostName))
            {
                MessageBox.Show("The batocera hostname is not configured.\nPlease use the Settings menu to configure this.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null!;
            }

            (string userName, string userPassword) = CredentialHelper.GetCredentials(hostName);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The batocera credentials are missing.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null!;
            }

            string output = string.Empty;
            using var client = new SshClient(hostName, userName, userPassword);
            try
            {
                client.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an error connecting to the host: {ex.Message}", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return null!;
            }
            using (var commandRunner = client.RunCommand(command))
            {
                output = commandRunner.Result;
            }
            client.Disconnect();
            userPassword = null!;
            return output;
        }

        public static async void MapNetworkDrive()
        {
            string hostName = Properties.Settings.Default.BatoceraHostName;

            if (string.IsNullOrEmpty(hostName))
            {
                MessageBox.Show("The Batocera hostname is not set.\nPlease use the Settings menu to configure this.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            (string userName, string userPassword) = CredentialHelper.GetCredentials(hostName);
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The Batocera credentials are missing.\nPlease use the Settings menu to configure this.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string networkShareToCheck = $"\\\\{hostName}\\share";

            bool isMapped = DriveMappingHelper.IsShareMapped(networkShareToCheck);

            if (isMapped == true)
            {
                MessageBox.Show($"There already is a drive mapping: {networkShareToCheck}", "Map Network Drive", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            char driveLetter = '\0';

            // Get first letter starting at z: working backward
            for (char drive = 'Z'; drive >= 'D'; drive--)
            {
                if (!DriveInfo.GetDrives().Any(d => d.Name[0] == drive))
                {
                    driveLetter = drive;
                    break;
                }
            }

            string networkSharePath = $"\\\\{hostName}\\share";
            string exePath = "net";
            string command = $" use {driveLetter}: {networkSharePath} /user:{userName} {userPassword}";

            // Execute the net use command
            string output = await CommandHelper.ExecuteCommandAsync(exePath, command);

            if (output != null && output != string.Empty)
            {
                MessageBox.Show(output, "Map Network Drive", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public static void OpenTerminal()
        {
            string hostName = Properties.Settings.Default.BatoceraHostName;

            if (hostName == null || hostName == string.Empty)
            {
                MessageBox.Show("The batocera hostname is not set.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            (string userName, string userPassword) = CredentialHelper.GetCredentials(hostName);

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPassword))
            {
                MessageBox.Show("The batocera credentials are missing.\nPlease use the Settings menu to configure this", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string sshPath = "C:\\Windows\\System32\\OpenSSH\\ssh.exe";

            try
            {
                ProcessStartInfo psi = new(sshPath)
                {
                    Arguments = $"-t {userName}@{hostName}",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process process = new() { StartInfo = psi };
                process.Start();
            }
            catch (Exception)
            {
                MessageBox.Show("Could not start OpenSSH.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void GetVersion()
        {
            string command = "batocera-es-swissknife --version";
            string output = ExecuteSshCommand(command);

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            MessageBox.Show($"Your Batocera is version {output}.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ShowUpdates()
        {
            string command = "batocera-es-swissknife --update";
            string output = ExecuteSshCommand(command);

            if (string.IsNullOrEmpty(output))
            {
                return;
            }

            MessageBox.Show($"{output}", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void StopEmulators()
        {
            var result = MessageBox.Show("Are you sure you want to stop any running emulators?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;reboot";
            string output = ExecuteSshCommand(command);

            MessageBox.Show("Running emulators should now be stopped.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void StopEmulationStation()
        {
            var result = MessageBox.Show("Are you sure you want to stop EmulationStation?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop ; batocera-es-swissknife --espid";
            string output = ExecuteSshCommand(command);

            if (!string.IsNullOrEmpty(output) && output.Contains('0'))
            {
                MessageBox.Show("EmulationStation is stopped.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Debug.WriteLine(output);
                MessageBox.Show("An unknown error has occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void RebootHost()
        {
            var result = MessageBox.Show("Are you sure you want to reboot your Batocera host?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;reboot";
            string output = ExecuteSshCommand(command);

            MessageBox.Show("A reboot command has been sent to the host.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ShutdownHost()
        {
            var result = MessageBox.Show("Are you sure you want to shutdown your Batocera host?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string command = "/etc/init.d/S31emulationstation stop;sleep 3;shutdown -h now";
            string output = ExecuteSshCommand(command);

            MessageBox.Show("A shutdown command has been sent to the host.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void RemoveSshKey()
        {
            string hostname = Properties.Settings.Default.BatoceraHostName;

            if (string.IsNullOrEmpty(hostname))
            {
                MessageBox.Show("The Batocera hostname is not set.");
                return;
            }

            var result = MessageBox.Show($"Do you want to reset the local SSH key for '{hostname}'?",
                                         "Reset Confirmation",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                string response = SshKeyHelper.RemoveBatoceraKey(hostname);
                MessageBox.Show(response, "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}