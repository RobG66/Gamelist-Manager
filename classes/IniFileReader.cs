using System.IO;

public static class IniFileReader
{
    public static Dictionary<string, Dictionary<string, string>> ReadIniFile(string filePath)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"INI file not found: {filePath}");
        }

        string[] lines = File.ReadAllLines(filePath);

        string currentSection = string.Empty;
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
            {
                continue;
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
                    string key = trimmedLine.Substring(0, indexOfEquals).Trim();
                    string value = trimmedLine.Substring(indexOfEquals + 1).Trim();
                    sections[currentSection][key] = value;
                }
                else
                {
                    sections[currentSection][trimmedLine] = trimmedLine;
                }
            }
        }

        return sections;
    }

    public static Dictionary<string, string>? GetSection(string filePath, string sectionName)
    {


        var sections = ReadIniFile(filePath);
        return sections.TryGetValue(sectionName, out var section) ? section : null;
    }
}
