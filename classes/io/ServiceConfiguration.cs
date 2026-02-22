using GamelistManager.classes.api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace GamelistManager.classes.io
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient("ScraperClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 50
            });

            // Your scraper classes can be transient
            services.AddTransient<API_ScreenScraper>();
            services.AddTransient<API_ArcadeDB>();
            services.AddTransient<API_EmuMovies>();

            // File transfer/downloader too
            services.AddTransient<FileTransfer>();
        }

    }
}

