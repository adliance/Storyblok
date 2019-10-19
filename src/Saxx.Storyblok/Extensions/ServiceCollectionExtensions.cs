using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Saxx.Storyblok.Settings;

namespace Saxx.Storyblok.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStoryblok(this IServiceCollection services, Action<StoryblokSettings> configureOptions)
        {
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddScoped<StoryblokClient>();

            services.AddSingleton(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var settings = new StoryblokSettings(configuration);
                configureOptions?.Invoke(settings);
                return settings;
            });
            return services;
        }

        public static IServiceCollection AddStoryblok(this IServiceCollection services)
        {
            return AddStoryblok(services, null);
        }
    }
}