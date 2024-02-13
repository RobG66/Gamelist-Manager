using Renci.SshNet;

namespace GamelistManager
{
    internal static class SSHCommander
    {
        public static string ExecuteSSHCommand(string hostName, string userID, string userPassword, string command)
        {
            string output = string.Empty;
            using (var client = new SshClient(hostName, userID, userPassword))
            {
                try
                {
                    client.Connect();
                }
                catch
                {
                    return "connectfailed";
                }

                using (var commandRunner = client.RunCommand(command))
                {
                    // Show the server output in a message box
                    output = commandRunner.Result;
                }
                client.Disconnect();
                userPassword = null;
                return output;
            }
        }
    }
}
