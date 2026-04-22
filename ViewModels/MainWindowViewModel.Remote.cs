using CommunityToolkit.Mvvm.Input;
using Gamelist_Manager.Views;
using System;
using System.Diagnostics;

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
    #endregion

    #region Commands

    [RelayCommand]
    private void MapNetworkDrive()
    {
        if (!LoadSSHCredentials()) return;
        
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
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "sh",
                    Arguments = $"-c \"ssh {_username}@{_sshTarget}\"",
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            CommandFailed();
        }
    }

    [RelayCommand]
    private void GetVersion()
    {
        if (!LoadSSHCredentials()) return;
    }

    [RelayCommand]
    private void ShowUpdates()
    {
        if (!LoadSSHCredentials()) return;
    }

    [RelayCommand]
    private void StopEmulators()
    {
        if (!LoadSSHCredentials()) return;
    }

    [RelayCommand]
    private void StopEmulationstation()
    {
        if (!LoadSSHCredentials()) return;
    }

    [RelayCommand]
    private void RebootHost()
    {
        if (!LoadSSHCredentials()) return;
    }

    [RelayCommand]
    private void ShutdownHost()
    {
        if (!LoadSSHCredentials()) return;
    }

    [RelayCommand]
    private void RemoveBatoceraSSHKey()
    {
        if (!LoadSSHCredentials()) return;
    }

    #endregion

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
        return;
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
        return;
    }
}