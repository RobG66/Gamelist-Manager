using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Classes.Helpers;
using Gamelist_Manager.Models;
using Gamelist_Manager.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Gamelist_Manager.ViewModels;

public partial class MainWindowViewModel
{
    #region Private Fields
    private string _sshTarget = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    #endregion

    #region Private Methods
    private bool LoadSSHCredentials()
    {
        _sshTarget = _settingsState.Hostname ?? string.Empty;
        _username = _settingsState.UserId ?? string.Empty;
        _password = _settingsState.Password ?? string.Empty;

        return !string.IsNullOrEmpty(_sshTarget) &&
               !string.IsNullOrEmpty(_username) &&
               !string.IsNullOrEmpty(_password);
    }

    private SshConnectionInfo GetSshConnection() => new(_sshTarget, _username, _password);
    #endregion

    #region Commands

    [RelayCommand]
    private void MapNetworkDrive()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }
    }

    [RelayCommand]
    private void OpenTerminal()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c ssh {_username}@{_sshTarget}",
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                // Try common terminal emulators in order of popularity
                string[] terminals = [
                    "x-terminal-emulator", // Debian/Ubuntu default alias
        "gnome-terminal",
        "konsole",
        "xfce4-terminal",
        "xterm",
        "lxterminal",
        "mate-terminal",
        "tilix"
                ];

                string sshCommand = $"ssh {_username}@{_sshTarget}";
                bool launched = false;

                foreach (var terminal in terminals)
                {
                    try
                    {
                        // Each terminal has a different flag for passing a command
                        string args = terminal switch
                        {
                            "gnome-terminal" => $"-- bash -c \"{sshCommand}; exec bash\"",
                            "konsole" => $"-e bash -c \"{sshCommand}; exec bash\"",
                            _ => $"-e \"{sshCommand}\""
                        };

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = terminal,
                            Arguments = args,
                            UseShellExecute = false
                        });

                        launched = true;
                        break;
                    }
                    catch
                    {
                        // Terminal not found, try next
                    }
                }

                if (!launched) CommandFailed();
            }
        }
        catch
        {
            CommandFailed();
        }
    }

    [RelayCommand]
    private async Task GetVersionAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "batocera-es-swissknife --version");

        if (sshResult.Success)
            await ThreeButtonDialogView.ShowInfoAsync("Batocera Version", $"Your Batocera is version {sshResult.Output}.");
        else
            await ThreeButtonDialogView.ShowErrorAsync("Batocera Version", sshResult.Error);
    }

    [RelayCommand]
    private async Task ShowUpdatesAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "batocera-es-swissknife --update");

        if (!string.IsNullOrEmpty(sshResult.Output))
            await ThreeButtonDialogView.ShowInfoAsync("Batocera Updates", sshResult.Output);
        else
            await ThreeButtonDialogView.ShowErrorAsync("Batocera Updates", sshResult.Error);
    }

    [RelayCommand]
    private async Task StopEmulatorsAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowConfirmAsync(
            "Stop Emulators",
            "This will terminate all running emulators on the host. Continue?",
            confirmText: "OK",
            icon: DialogIconTheme.Warning);
        if (!result) return;

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "/etc/init.d/S31emulationstation stop");

        if (sshResult.Success)
            await ThreeButtonDialogView.ShowInfoAsync("Stop Emulators", "Running emulators should now be stopped.");
        else
            await ThreeButtonDialogView.ShowErrorAsync("Stop Emulators", sshResult.Error);
    }

    [RelayCommand]
    private async Task StopEmulationstationAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowConfirmAsync(
            "Stop EmulationStation",
            "This will stop EmulationStation. Continue?",
            confirmText: "OK",
            icon: DialogIconTheme.Warning);
        if (!result) return;

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "/etc/init.d/S31emulationstation stop ; batocera-es-swissknife --espid");

        bool isStopped = sshResult.Output != null && sshResult.Output.TrimEnd().EndsWith("0");

        if (isStopped)
        {
            await ThreeButtonDialogView.ShowInfoAsync("Stop EmulationStation", "EmulationStation is stopped.");
        }
        else
        {
            string error = !string.IsNullOrEmpty(sshResult.Error) ? sshResult.Error : $"Failed or unexpected output: {sshResult.Output}";
            await ThreeButtonDialogView.ShowErrorAsync("Stop EmulationStation", error);
        }
    }

    [RelayCommand]
    private async Task RebootHostAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowConfirmAsync(
            "Reboot Host",
            "This will reboot the remote host. Continue?",
            confirmText: "OK",
            icon: DialogIconTheme.Warning);
        if (!result) return;

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "/etc/init.d/S31emulationstation stop;reboot");

        if (sshResult.Success)
            await ThreeButtonDialogView.ShowInfoAsync("Reboot Host", "A reboot command has been sent to the host.");
        else
            await ThreeButtonDialogView.ShowErrorAsync("Reboot Host", sshResult.Error);
    }

    [RelayCommand]
    private async Task ShutdownHostAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowConfirmAsync(
            "Shutdown Host",
            "This will shutdown the remote host. Continue?",
            confirmText: "OK",
            icon: DialogIconTheme.Warning);
        if (!result) return;

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "/etc/init.d/S31emulationstation stop;sleep 5;shutdown -h now");

        if (sshResult.Success)
            await ThreeButtonDialogView.ShowInfoAsync("Shutdown Host", "A shutdown command has been sent to the host.");
        else
            await ThreeButtonDialogView.ShowErrorAsync("Shutdown Host", sshResult.Error);
    }

    [RelayCommand]
    private async Task RemoveBatoceraSSHKeyAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowConfirmAsync(
            "Remove SSH Key",
            "Are you sure you want to remove the trusted SSH key for this host?",
            confirmText: "OK",
            icon: DialogIconTheme.Warning);
        if (!result) return;

        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ssh-keygen",
                Arguments = $"-R {_sshTarget}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            process?.WaitForExit();
            bool success = process?.ExitCode == 0;

            if (success)
                await ThreeButtonDialogView.ShowInfoAsync("Remove SSH Key", "SSH key has been removed.");
            else
                await ThreeButtonDialogView.ShowErrorAsync("Remove SSH Key", "Failed to remove SSH key.");
        }
        catch
        {
            await ThreeButtonDialogView.ShowErrorAsync("Remove SSH Key", "Failed to remove SSH key. Is ssh-keygen installed?");
        }
    }

    #endregion

    #region Helpers

    private async void CredentialsMissing()
    {
        try
        {
            await ThreeButtonDialogView.ShowInfoAsync("Remote Credentials Missing", "No remote credentials are defined.");
        }
        catch (Exception) { }
    }

    private async void CommandFailed()
    {
        try
        {
            await ThreeButtonDialogView.ShowInfoAsync("Command Failed", "The command execution failed.");
        }
        catch (Exception) { }
    }

    #endregion
}