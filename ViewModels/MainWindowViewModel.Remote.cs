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
        _sshTarget = _sharedData.Hostname ?? string.Empty;
        _username = _sharedData.UserId ?? string.Empty;
        _password = _sharedData.Password ?? string.Empty;

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

        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Batocera Version",
            Message = sshResult.Success ? $"Your Batocera is version {sshResult.Output}." : sshResult.Error,
            IconTheme = sshResult.Success ? DialogIconTheme.Info : DialogIconTheme.Error,
            Button1Text = "",
            Button2Text = "",
            Button3Text = "OK"
        });
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

        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Batocera Updates",
            Message = !string.IsNullOrEmpty(sshResult.Output) ? sshResult.Output : sshResult.Error,
            IconTheme = !string.IsNullOrEmpty(sshResult.Output) ? DialogIconTheme.Info : DialogIconTheme.Error,
            Button1Text = "",
            Button2Text = "",
            Button3Text = "OK"
        });
    }

    [RelayCommand]
    private async Task StopEmulatorsAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Stop Emulators",
            Message = "Are you sure you want to stop any running emulators?",
            IconTheme = DialogIconTheme.Question,
            Button1Text = "Cancel",
            Button2Text = "",
            Button3Text = "Stop"
        });

        if (result != ThreeButtonResult.Button3) return;

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "/etc/init.d/S31emulationstation stop");

        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Stop Emulators",
            Message = sshResult.Success ? "Running emulators should now be stopped." : sshResult.Error,
            IconTheme = sshResult.Success ? DialogIconTheme.Info : DialogIconTheme.Error,
            Button1Text = "",
            Button2Text = "",
            Button3Text = "OK"
        });
    }

    [RelayCommand]
    private async Task StopEmulationstationAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Stop EmulationStation",
            Message = "Are you sure you want to stop EmulationStation?",
            IconTheme = DialogIconTheme.Question,
            Button1Text = "Cancel",
            Button2Text = "",
            Button3Text = "Stop"
        });

        if (result != ThreeButtonResult.Button3) return;

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "/etc/init.d/S31emulationstation stop ; batocera-es-swissknife --espid");

        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Stop EmulationStation",
            Message = sshResult.Success && sshResult.Output.Contains('0')
                ? "EmulationStation is stopped."
                : sshResult.Error,
            IconTheme = sshResult.Success && sshResult.Output.Contains('0')
                ? DialogIconTheme.Info
                : DialogIconTheme.Error,
            Button1Text = "",
            Button2Text = "",
            Button3Text = "OK"
        });
    }

    [RelayCommand]
    private async Task RebootHostAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Reboot Host",
            Message = "Are you sure you want to reboot your Batocera host?",
            IconTheme = DialogIconTheme.Question,
            Button1Text = "Cancel",
            Button2Text = "",
            Button3Text = "Reboot"
        });

        if (result != ThreeButtonResult.Button3) return;

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "/etc/init.d/S31emulationstation stop;reboot");

        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Reboot Host",
            Message = sshResult.Success ? "A reboot command has been sent to the host." : sshResult.Error,
            IconTheme = sshResult.Success ? DialogIconTheme.Info : DialogIconTheme.Error,
            Button1Text = "",
            Button2Text = "",
            Button3Text = "OK"
        });
    }

    [RelayCommand]
    private async Task ShutdownHostAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Shutdown Host",
            Message = "Are you sure you want to shutdown your Batocera host?",
            IconTheme = DialogIconTheme.Question,
            Button1Text = "Cancel",
            Button2Text = "",
            Button3Text = "Shutdown"
        });

        if (result != ThreeButtonResult.Button3) return;

        var sshResult = await SshHelper.ExecuteCommandAsync(GetSshConnection(), "/etc/init.d/S31emulationstation stop;sleep 5;shutdown -h now");

        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Shutdown Host",
            Message = sshResult.Success ? "A shutdown command has been sent to the host." : sshResult.Error,
            IconTheme = sshResult.Success ? DialogIconTheme.Info : DialogIconTheme.Error,
            Button1Text = "",
            Button2Text = "",
            Button3Text = "OK"
        });
    }

    [RelayCommand]
    private async Task RemoveBatoceraSSHKeyAsync()
    {
        if (!LoadSSHCredentials())
        {
            CredentialsMissing();
            return;
        }

        var result = await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Remove SSH Key",
            Message = $"Do you want to reset the local SSH key for '{_sshTarget}'?",
            IconTheme = DialogIconTheme.Question,
            Button1Text = "Cancel",
            Button2Text = "",
            Button3Text = "Remove"
        });

        if (result != ThreeButtonResult.Button3) return;

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

            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Remove SSH Key",
                Message = success ? "SSH key has been removed." : "Failed to remove SSH key.",
                IconTheme = success ? DialogIconTheme.Info : DialogIconTheme.Error,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
        }
        catch
        {
            await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
            {
                Title = "Remove SSH Key",
                Message = "Failed to remove SSH key. Is ssh-keygen installed?",
                IconTheme = DialogIconTheme.Error,
                Button1Text = "",
                Button2Text = "",
                Button3Text = "OK"
            });
        }
    }

    #endregion

    #region Helpers

    private async void CredentialsMissing()
    {
        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Remote Credentials Missing",
            Message = "No remote credentials are defined.",
            IconTheme = DialogIconTheme.Info,
            Button1Text = "",
            Button2Text = "",
            Button3Text = "OK"
        });
    }

    private async void CommandFailed()
    {
        await ThreeButtonDialogView.ShowAsync(new ThreeButtonDialogConfig
        {
            Title = "Command Failed",
            Message = "The command execution failed.",
            IconTheme = DialogIconTheme.Info,
            Button1Text = "",
            Button2Text = "",
            Button3Text = "OK"
        });
    }

    #endregion
}