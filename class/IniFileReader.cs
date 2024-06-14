using Renci.SshNet.Security;
using System.Collections.Generic;
using System.IO;

public class IniFileReader
{
    private readonly Dictionary<string, Dictionary<string, string>> sections;

    public IniFileReader(string filePath)
    {
        sections = new Dictionary<string, Dictionary<string, string>>();
        ReadIniFile(filePath);
    }

    private void ReadIniFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"INI file not found: {filePath}");
        }

        string[] lines = File.ReadAllLines(filePath);

        string currentSection = null;
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
            {
                continue; // Skip empty lines and comments
            }

            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                sections[currentSection] = new Dictionary<string, string>();
            }
            else if (currentSection != null)
            {
                int indexOfEquals = trimmedLine.IndexOf('=');
                if (indexOfEquals != -1)
                {
                    // If there is an = then key and value are set
                    string key = trimmedLine.Substring(0, indexOfEquals).Trim();
                    string value = trimmedLine.Substring(indexOfEquals + 1).Trim();
                    sections[currentSection][key] = value;
                }
                else
                {
                    // If there is no = then key and value are set the same
                    // For added flexibility of adding just straight values
                    sections[currentSection][trimmedLine] = trimmedLine;
                }
            }
        }
    }

    public Dictionary<string, string> GetSection(string sectionName)
    {
        if (sections.ContainsKey(sectionName))
        {
            return sections[sectionName];
        }
        else
        {
            throw new KeyNotFoundException($"Section '{sectionName}' not found in the INI file.");
        }
    }

    public Dictionary<string, Dictionary<string, string>> GetAllSections()
    {
        return sections;
    }
}
