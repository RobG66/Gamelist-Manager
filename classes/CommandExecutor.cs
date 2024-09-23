using System.Diagnostics;

namespace GamelistManager.classes
{
    public class CommandExecutor
    {
        // Asynchronous method to execute a command
        public static async Task<string> ExecuteCommandAsync(string exePath, string switches)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = switches;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();

                process.WaitForExit();

                return output;
            }
        }

        // Synchronous method to execute a command
        public static string ExecuteCommand(string exePath, string command)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = command;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                // Synchronously wait for the result
                string output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                return output;
            }
        }
    }
}
