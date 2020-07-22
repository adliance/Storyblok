using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Saxx.Storyblok.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStoryblok(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddScoped<StoryblokClient>();
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