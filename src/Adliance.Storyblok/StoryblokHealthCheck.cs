using System;
using System.Threading;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok;

// ReSharper disable once ClassNeverInstantiated.Global
public class StoryblokHealthCheck(IOptions<StoryblokOptions> settings, StoryblokStoryClient storyblok, ILogger<StoryblokHealthCheck> logger)
    : IHealthCheck
{
    private readonly StoryblokOptions _settings = settings.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var story = await storyblok.Story().WithSlug(_settings.SlugForHealthCheck).Load();
            if (story?.Content == null)
            {
                throw new Exception("Story of story content is null.");
            }

            return await Task.FromResult(HealthCheckResult.Healthy("Storyblok is healthy."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Storyblok health check failed.");
            return await Task.FromResult(HealthCheckResult.Unhealthy("Storyblok is not healthy."));
        }
    }
}
