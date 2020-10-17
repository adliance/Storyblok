using System;
using System.Threading;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StoryblokHealthCheck : IHealthCheck
    {
        private readonly StoryblokOptions _settings;
        private readonly StoryblokStoryClient _storyblok;
        private readonly ILogger<StoryblokHealthCheck> _logger;

        public StoryblokHealthCheck(IOptions<StoryblokOptions> settings, StoryblokStoryClient storyblok, ILogger<StoryblokHealthCheck> logger)
        {
            _settings = settings.Value;
            _storyblok = storyblok;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var story = await _storyblok.Story().WithSlug(_settings.SlugForHealthCheck).Load();
                if (story?.Content == null)
                {
                    throw new Exception("Story of story content is null.");
                }

                return await Task.FromResult(HealthCheckResult.Healthy("Storyblok is healthy."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Storyblok health check failed.");
                return await Task.FromResult(HealthCheckResult.Unhealthy("Storyblok is not healthy."));
            }
        }
    }
}