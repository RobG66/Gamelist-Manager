using System.Diagnostics;
using System.Text;

namespace GamelistManager.classes.helpers
{
    public class CommandHelper
    {
        // Async method to execute a command with optional timeout (milliseconds)
        public static async Task<string> ExecuteCommandAsync(string exePath, string arguments, int timeoutMs = 30000)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentException("Executable path cannot be null or empty.", nameof(exePath));

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) outputBuilder.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) errorBuilder.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait asynchronously for exit with timeout
                bool exited = await Task.Run(() => process.WaitForExit(timeoutMs));

                if (!exited)
                {
                    try { process.Kill(); } catch { /* ignore */ }
                    throw new TimeoutException($"Command '{exePath} {arguments}' timed out after {timeoutMs} ms.");
                }

                string output = outputBuilder.ToString();
                string error = errorBuilder.ToString();

                if (!string.IsNullOrEmpty(error))
                    output += Environment.NewLine + "ERROR: " + error;
                return output;
            }
        }
    }
}
