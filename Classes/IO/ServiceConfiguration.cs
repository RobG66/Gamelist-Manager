using Gamelist_Manager.Classes.Api;
using Gamelist_Manager.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Gamelist_Manager.Classes.IO
{
    public static class Startup
    {
        private static readonly Lazy<IServiceProvider> _provider = new(() =>
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            return services.BuildServiceProvider();
        });

        public static IServiceProvider Services => _provider.Value;

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient("ScraperClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 50
            });

            services.AddHttpClient("MediaDropClient", client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            });

            services.AddTransient<API_ScreenScraper>();
            services.AddTransient<API_ArcadeDB>();
            services.AddTransient<API_EmuMovies>();
            services.AddTransient<FileTransfer>();
            services.AddSingleton(SharedDataService.Instance);
            services.AddTransient<ScraperService>();
        }
    }
}
