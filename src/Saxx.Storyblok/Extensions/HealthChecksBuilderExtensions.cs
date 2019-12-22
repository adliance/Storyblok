using Microsoft.Extensions.DependencyInjection;

namespace Saxx.Storyblok.Extensions
{
    public static class HealthChecksBuilderExtensions
    {
        public static IHealthChecksBuilder AddStoryblok(this IHealthChecksBuilder builder)
        {
            return builder.AddCheck<StoryblokHealthCheck>("Storyblok");
        }
    }
}