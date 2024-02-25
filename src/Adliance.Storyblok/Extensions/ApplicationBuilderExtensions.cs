using System;
using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseStoryblok(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<IOptions<StoryblokOptions>>();
        if (options != null)
        {
            var optionsValue = options.Value;

            if (!string.IsNullOrWhiteSpace(optionsValue.RedirectsDatasourceName))
            {
                app.UseMiddleware<StoryblokRedirectsMiddleware>();
            }
        }

        //Add the ability to use Storyblok style of localization (start of slug)
        var requestLocalizationOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
        if (requestLocalizationOptions?.Value == null && (options?.Value.SupportedCultures.Any() ?? false))
        {
            throw new InvalidOperationException("RequestLocalizationOptions is not configured. Please register `IOptions<RequestLocalizationOptions>` to your service collection.");
        }
        else if (requestLocalizationOptions?.Value != null)
        {
            requestLocalizationOptions.Value.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
            {
                // special handling of Storyblok preview URLs that contain the language, like ~/de/home vs. ~/home
                // if we have such a URL, we also change the current culture accordingly
                var slug = context.Request.Path.ToString().Trim('/');
                var supportedCultures = context.RequestServices.GetService<IOptions<StoryblokOptions>>()?.Value?.SupportedCultures ?? Array.Empty<string>();

                foreach (var supportedCulture in supportedCultures)
                {
                    if (slug.StartsWith($"{supportedCulture}/", StringComparison.OrdinalIgnoreCase) || slug.Equals(supportedCulture, StringComparison.OrdinalIgnoreCase))
                        return await Task.FromResult(new ProviderCultureResult(supportedCulture));
                }

                return await Task.FromResult<ProviderCultureResult?>(null);
            }));

            if (options?.Value != null && requestLocalizationOptions.Value != null)
            {
                foreach (var culture in options.Value.SupportedCultures)
                {
                    if (!requestLocalizationOptions.Value?.SupportedCultures?.Any(x => x.Name.Equals(culture, StringComparison.OrdinalIgnoreCase)) ?? false)
                        requestLocalizationOptions.Value?.SupportedCultures?.Add(new System.Globalization.CultureInfo(culture));

                    if (!requestLocalizationOptions.Value?.SupportedUICultures?.Any(x => x.Name.Equals(culture, StringComparison.OrdinalIgnoreCase)) ?? false)
                        requestLocalizationOptions.Value?.SupportedUICultures?.Add(new System.Globalization.CultureInfo(culture));
                }
            }
        }

        app.MapWhen(context => options?.Value.EnableSitemap == true && context.Request.Path.StartsWithSegments("/sitemap.xml", StringComparison.OrdinalIgnoreCase), appBuilder => { appBuilder.UseMiddleware<StoryblokSitemapMiddleware>(); });

        app.MapWhen(context => !string.IsNullOrWhiteSpace(options?.Value.SlugForClearingCache) && context.Request.Path.StartsWithSegments("/" + options.Value.SlugForClearingCache.Trim('/'), StringComparison.OrdinalIgnoreCase),
            appBuilder => { appBuilder.UseMiddleware<StoryblokClearCacheMiddleware>(); });

        app.UseRequestLocalization();
        app.UseMiddleware<StoryblokMiddleware>();

        return app;
    }
}
