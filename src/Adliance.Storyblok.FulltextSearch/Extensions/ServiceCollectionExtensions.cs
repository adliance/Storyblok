using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Adliance.Storyblok.FulltextSearch.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStoryblokFulltextSearch<T>(this IServiceCollection services) where T : FulltextSearchBase
    {
        services.AddScoped<LuceneService>();
        services.AddScoped<T>();
        services.AddScoped<FulltextSearchBase, T>();
        services.AddHostedService<FulltextSearchBackgroundService>();
        return services;
    }
}
