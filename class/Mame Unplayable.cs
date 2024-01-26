using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GamelistManager
{
    public static class MameUnplayable
    {
        public static async Task<List<string>> GetFilteredGameNames(string mameExePath)
        {
            string command = "-listxml";
            string output = await CommandExecutor.ExecuteCommandAsync(mameExePath, command);

            var xml = XDocument.Parse(output);
            var machines = xml.Descendants("machine").ToArray();

            var gameNames = new List<string>();

            foreach (var machine in machines)
            {
                if (ShouldIncludeMachine(machine))
                {
                    gameNames.Add(machine.Attribute("name")?.Value);
                }
            }

            return gameNames;

        }

        private static bool ShouldIncludeMachine(XElement machine)
        {
            return machine.Attribute("isbios")?.Value == "yes" ||
                   machine.Attribute("isdevice")?.Value == "yes" ||
                   machine.Attribute("ismechanical")?.Value == "yes" ||
                   machine.Element("driver")?.Attribute("status")?.Value == "preliminary" ||
                   machine.Element("disk")?.Attribute("status")?.Value == "nodump" ||
                   machine.Attribute("runnable")?.Value == "no";
        }
    }
}
