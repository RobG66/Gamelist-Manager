using Gamelist_Manager.Models;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Helpers;

public static class SshHelper
{
    public static async Task<SshResult> ExecuteCommandAsync(
        SshConnectionInfo connection,
        string command,
        int timeoutSeconds = 10)
    {
        if (string.IsNullOrWhiteSpace(connection.Host))
            return new SshResult(false, string.Empty, "Host is not specified.");

        if (string.IsNullOrWhiteSpace(connection.Username))
            return new SshResult(false, string.Empty, "Username is not specified.");

        await Task.Yield();

        try
        {
            using var client = new SshClient(connection.Host, connection.Username, connection.Password);
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            client.Connect();

            if (!client.IsConnected)
                return new SshResult(false, string.Empty, "Failed to connect to host.");

            using var cmd = client.RunCommand(command);
            string stdout = cmd.Result?.Trim() ?? string.Empty;
            string stderr = cmd.Error?.Trim() ?? string.Empty;
            bool success = cmd.ExitStatus == 0;

            client.Disconnect();

            return new SshResult(success, stdout, stderr);
        }
        catch (SshConnectionException ex)
        {
            return new SshResult(false, string.Empty, $"Connection failed: {ex.Message}");
        }
        catch (SshAuthenticationException ex)
        {
            return new SshResult(false, string.Empty, $"Authentication failed: {ex.Message}");
        }
        catch (SocketException ex)
        {
            return new SshResult(false, string.Empty, $"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new SshResult(false, string.Empty, $"Unexpected error: {ex.Message}");
        }
    }
}