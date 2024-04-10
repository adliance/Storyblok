using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.FulltextSearch;

public class FulltextSearchBackgroundService(ILogger<FulltextSearchBackgroundService> logger, IServiceProvider services) : BackgroundService
{
    private readonly TimeSpan _period = TimeSpan.FromSeconds(3);
    private DateTime _lastRun = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Fulltext index background job started.");
        using var scope = services.CreateScope();
        var fulltextService = scope.ServiceProvider.GetRequiredService<FulltextSearchBase>();
        var storyblokOptions = scope.ServiceProvider.GetRequiredService<IOptions<StoryblokOptions>>();

        using var timer = new PeriodicTimer(_period);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (_lastRun < DateTime.UtcNow.AddHours(-1)) // run every hour
            {
                var cultures = storyblokOptions.Value.SupportedCultures;
                if (!cultures.Any()) cultures = ["en"];

                foreach (var culture in cultures)
                {
                    try
                    {
                        logger.LogInformation($"Updating fulltext index for culture {culture} ...");

                        var numberOfDocuments = await fulltextService.UpdateFulltextIndex(culture);
                        _lastRun = DateTime.UtcNow;

                        if (numberOfDocuments.HasValue) logger.LogInformation($"Updating fulltext index completed with {numberOfDocuments} documents).");
                        else logger.LogInformation("No update of fulltext index required.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to execute {nameof(FulltextSearchBackgroundService)}: {ex.Message}");
                    }
                }
            }
        }

        logger.LogInformation("Fulltext index background job ended.");
    }
}
