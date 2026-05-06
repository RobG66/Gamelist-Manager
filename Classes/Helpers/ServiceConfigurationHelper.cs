using Gamelist_Manager.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Gamelist_Manager.Classes.Helpers
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
            services.AddHttpClient(HttpClientNames.Scraper, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(40);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 100
            });

            services.AddHttpClient(HttpClientNames.ScreenScraper, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(40);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 100
            });

            services.AddHttpClient(HttpClientNames.MediaDrop, client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            });

            services.AddSingleton(SharedDataService.Instance);
            services.AddTransient<ScraperService>();
        }
    }
}
