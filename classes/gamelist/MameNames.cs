using System.Diagnostics;
using System.IO;
using System.Xml;

namespace GamelistManager.classes.gamelist
{
    static class MameNames
    {
        public static Dictionary<string, string> Names { get; private set; } = [];

        /// Generates the MAME name/description dictionary from -listxml output.
        /// Revisit this method to improve error handling and performance as needed.

        public static async Task GenerateAsync(string mameExePath)
        {
            if (string.IsNullOrEmpty(mameExePath) || !File.Exists(mameExePath))
                return;

            var mameNames = new Dictionary<string, string>();

            try
            {
                await Task.Run(() =>
                {
                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = mameExePath,
                            Arguments = "-listxml",
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
                                string? name = reader.GetAttribute("name");
                                if (string.IsNullOrEmpty(name))
                                    continue;

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

                                if (!mameNames.ContainsKey(name))
                                    mameNames[name] = description;
                            }
                        }
                    }

                    process.WaitForExit();
                });

                // Store results in the static dictionary
                Names = mameNames;
            }
            catch
            {
                // Handle exceptions (e.g., log them)
            }
        }
    }
}
