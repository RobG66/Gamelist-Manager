using Microsoft.Extensions.Configuration;
using System;
using System.Windows.Forms;

namespace GamelistManager
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
              .AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

            IConfigurationRoot root = builder.Build();

            // Access values from the configuration
            var devID = root["devID"];
            var devPassword = root["devPassword"];

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GamelistManagerForm());


        }
    }
}
