using System;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adliance.Storyblok.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStoryblok(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddScoped<StoryblokStoryClient>();
            services.AddScoped<StoryblokStoriesClient>();
            services.AddScoped<StoryblokDatasourceClient>();
            return services;
        }

        public static IServiceCollection AddStoryblok(this IServiceCollection services, IConfigurationSection configurationSection, Action<StoryblokOptions>? configure = null)
        {
            var options = new StoryblokOptions();
            configurationSection.Bind(options);

            services.Configure<StoryblokOptions>(storyblokOptions =>
            {
                if (storyblokOptions != null)
                {
                    configurationSection.Bind(storyblokOptions);
                }

                if (configure != null && storyblokOptions != null)
                {
                    configure.Invoke(storyblokOptions);
                }
            });
            
            return AddStoryblok(services);
        }
    }
}