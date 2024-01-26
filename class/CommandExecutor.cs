using System.Diagnostics;
using System.Threading.Tasks;

// Runs a command and returns output

namespace GamelistManager
{
    public class CommandExecutor
    {
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
