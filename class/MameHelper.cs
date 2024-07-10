using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

namespace GamelistManager
{
    internal static class MameHelper
    {
        public static async Task<List<string>> GetMameClones(string mameExePath)
        {
            string command = "-listxml";
            var cloneNames = new List<string>();

            await Task.Run(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = mameExePath,
                        Arguments = command,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse
                };

                using (var reader = XmlReader.Create(process.StandardOutput, settings))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "machine" && reader.GetAttribute("cloneof") != null)
                        {
                            string cloneName = reader.GetAttribute("name");
                            cloneNames.Add(cloneName);
                        }
                    }
                }

                process.WaitForExit();
            });

            return cloneNames;
        }

        public static async Task<List<string>> GetMameUnplayable(string mameExePath)
        {
            string command = "-listxml";
            var gameNames = new List<string>();

            await Task.Run(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = mameExePath,
                        Arguments = command,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse
                };

                using (var reader = XmlReader.Create(process.StandardOutput, settings))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "machine")
                        {
                            if (ShouldIncludeMachine(reader))
                            {
                                gameNames.Add(reader.GetAttribute("name"));
                            }
                        }
                    }
                }

                process.WaitForExit();
            });

            return gameNames;
        }

        private static bool ShouldIncludeMachine(XmlReader reader)
        {
            bool isBios = reader.GetAttribute("isbios") == "yes";
            bool isDevice = reader.GetAttribute("isdevice") == "yes";
            bool isMechanical = reader.GetAttribute("ismechanical") == "yes";
            bool isRunnable = reader.GetAttribute("runnable") == "no";

            if (isBios || isDevice || isMechanical || isRunnable)
            {
                return true;
            }

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "driver" && reader.GetAttribute("status") == "preliminary")
                    {
                        return true;
                    }
                    if (reader.Name == "disk" && reader.GetAttribute("status") == "nodump")
                    {
                        return true;
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "machine")
                {
                    break;
                }
            }

            return false;
        }
    }
}
