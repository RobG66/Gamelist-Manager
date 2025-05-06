using System.Diagnostics;
using System.IO;
using System.Xml;

namespace GamelistManager.classes
{
    internal static class MameHelper
    {
        public class ChdInfo
        {
            public string? GameName { get; set; }
            public string? DiskName { get; set; }
            public bool Required { get; set; }
            public bool Present { get; set; }
            public string? Status { get; set; }
        }

        public static async Task<List<ChdInfo>> GetMameRequiresCHD(string mameExePath, string mameRomPath)
        {
            List<ChdInfo> chdInfoList = new List<ChdInfo>();
            string command = "-listxml";

            try
            {
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
                                string gameName = reader.GetAttribute("name")!;

                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "disk")
                                    {
                                        string diskName = reader.GetAttribute("name")!;
                                        string status = reader.GetAttribute("status")!;
                                        string optional = reader.GetAttribute("optional")!;

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

                                    }
                                    else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "machine")
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    process.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                // Log or handle the error as needed
                Console.Error.WriteLine($"Error occurred in GetMameRequiresCHD: {ex.Message}");
            }

            return chdInfoList;
        }

        public static async Task<Dictionary<string, string>> GetMameClones(string mameExePath)
        {
            string command = "-listxml";
            Dictionary<string, string> cloneNames = new Dictionary<string, string>();

            try
            {
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
                                string cloneName = reader.GetAttribute("name")!;
                                string cloneOf = reader.GetAttribute("cloneof")!;
                                cloneNames.Add(cloneName, cloneOf);
                            }
                        }
                    }

                    process.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                // Log or handle the error as needed
                Console.Error.WriteLine($"Error occurred in GetMameClones: {ex.Message}");
            }

            return cloneNames;
        }

        public static async Task<Dictionary<string, string>> GetMameNames(string mameExePath)
        {
            string command = "-listxml";
            Dictionary<string, string> mameNames = new Dictionary<string, string>();

            try
            {
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
                                string name = reader.GetAttribute("name")!;
                                string description = string.Empty;

                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "description")
                                    {
                                        description = reader.ReadElementContentAsString();
                                        break;
                                    }
                                    if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "machine")
                                    {
                                        break;
                                    }
                                }

                                mameNames.Add(name, description);
                            }
                        }
                    }

                    process.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                // Log or handle the error as needed
                Console.Error.WriteLine($"Error occurred in GetMameNames: {ex.Message}");
            }

            return mameNames;
        }

        public static async Task<List<string>> GetMameBootleg(string mameExePath)
        {
            string command = "-listxml";
            List<string> bootlegNames = new List<string>();

            try
            {
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
                                string? machineName = reader.GetAttribute("name");

                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "manufacturer")
                                    {
                                        string manufacturer = reader.ReadElementContentAsString();

                                        if (manufacturer.Contains("bootleg", StringComparison.OrdinalIgnoreCase))
                                        {
                                            bootlegNames.Add(machineName!);
                                        }

                                        break;
                                    }
                                    if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "machine")
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    process.WaitForExit();
                });
            }
            catch (Exception ex)
            {
                // Log or handle the error as needed
                Console.Error.WriteLine($"Error occurred in GetMameBootleg: {ex.Message}");
            }

            return bootlegNames;
        }

        public static async Task<List<string>> GetMameUnplayable(string mameExePath)
        {
            string command = "-listxml";
            var gameNames = new List<string>();

            try
            {
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
                                string gameName = reader.GetAttribute("name")!;
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
            }
            catch (Exception ex)
            {
                // Log or handle the error as needed
                Console.Error.WriteLine($"Error occurred in GetMameUnplayable: {ex.Message}");
            }

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

            try
            {
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
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error checking machine attributes: {ex.Message}");
            }

            return isBios || isDevice || isMechanical || isRunnable || hasPreliminaryDriver || hasNodumpDisk;
        }
    }
}
