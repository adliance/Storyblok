using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Adliance.Storyblok.FulltextSearch;

public class FulltextSearchBackgroundService : BackgroundService
{
    private readonly ILogger<FulltextSearchBackgroundService> _logger;
    private readonly IServiceProvider _services;
    private readonly TimeSpan _period = TimeSpan.FromSeconds(3);
    private DateTime _lastRun = DateTime.MinValue;

    public FulltextSearchBackgroundService(ILogger<FulltextSearchBackgroundService> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fulltextindex background job started.");
        using var scope = _services.CreateScope();
        var fulltextService = scope.ServiceProvider.GetRequiredService<FulltextSearchBase>();

        using var timer = new PeriodicTimer(_period);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (_lastRun < DateTime.UtcNow.AddHours(-1)) // run every hour
            {
                try
                {
                    _logger.LogInformation("Updating fulltext index ...");

                    var numberOfDocuments = await fulltextService.UpdateFulltextIndex();
                    _lastRun = DateTime.UtcNow;

                    if (numberOfDocuments.HasValue) _logger.LogInformation($"Updating fulltextindex completed {numberOfDocuments} documents).");
                    else _logger.LogInformation("No update of fulltextindex required.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to execute {nameof(FulltextSearchBackgroundService)}: {ex.Message}");
                }
            }
        }

        _logger.LogInformation("Fulltextindex background job ended.");
    }
}
