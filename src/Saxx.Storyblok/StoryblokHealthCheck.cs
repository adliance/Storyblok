using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Saxx.Storyblok.Settings;

namespace Saxx.Storyblok
{
    public class StoryblokHealthCheck : IHealthCheck
    {
        private readonly StoryblokSettings _settings;
        private readonly StoryblokClient _storyblok;
        private readonly ILogger<StoryblokHealthCheck> _logger;

        public StoryblokHealthCheck(StoryblokSettings settings, StoryblokClient storyblok, ILogger<StoryblokHealthCheck> logger)
        {
            _settings = settings;
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
