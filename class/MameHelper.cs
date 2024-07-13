using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace GamelistManager
{
    internal static class MameHelper
    {
        public class ChdInfo
        {
            public string GameName { get; set; }
            public string DiskName { get; set; }
            public bool Required { get; set; }
            public bool Present { get; set; }
            public string Status { get; set; }
        }

        public static async Task<List<ChdInfo>> GetMameRequiresCHD(string mameExePath, string mameRomPath)
        {
            List<ChdInfo> chdInfoList = new List<ChdInfo>();
            string command = "-listxml";

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
                            string gameName = reader.GetAttribute("name");
                            bool hasDisk = false;

                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.Element && reader.Name == "disk")
                                {
                                    string diskName = reader.GetAttribute("name");
                                    string status = reader.GetAttribute("status");
                                    string optional = reader.GetAttribute("optional");

                                    bool required = string.IsNullOrEmpty(optional) || optional.ToLower() != "yes";
                                    bool present = false;

                                    string fileName = diskName + ".chd";
                                    string chdPath = Path.Combine(mameRomPath, gameName, fileName);

                                    if (File.Exists(chdPath))
                                    {
                                        present = true;
                                    }

                                    chdInfoList.Add(new ChdInfo
                                    {
                                        GameName = gameName,
                                        DiskName = diskName,
                                        Required = required,
                                        Present = present,
                                        Status = status
                                    });

                                    hasDisk = true;
                                }
                                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "machine")
                                {
                                    break; // Exit the loop when machine element ends
                                }
                            }

                        }
                    }
                }

                process.WaitForExit();
            });

            return chdInfoList;
        }

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
                            string gameName = reader.GetAttribute("name");
                            if (!string.IsNullOrEmpty(gameName))
                            {
                                if (ShouldIncludeMachine(reader))
                                {
                                    gameNames.Add(gameName);
                                }
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
            bool hasPreliminaryDriver = false;
            bool hasNodumpDisk = false;

            // Check if any disk has a status of "nodump"
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "disk" && reader.GetAttribute("status") == "nodump")
                {
                    hasNodumpDisk = true;
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "machine")
                {
                    break;
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "driver" && reader.GetAttribute("status") == "preliminary")
                {
                    hasPreliminaryDriver = true;
                }
            }

            // Determine if the machine should be included based on criteria
            if (isBios || isDevice || isMechanical || isRunnable || hasPreliminaryDriver || hasNodumpDisk)
            {
                return true;
            }

            return false;
        }
    }



}
