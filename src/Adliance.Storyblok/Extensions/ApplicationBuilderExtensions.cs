using System;
using System.Globalization;
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
        if (requestLocalizationOptions?.Value == null && options?.Value.SupportedCultures.Length > 0)
        {
            throw new InvalidOperationException("RequestLocalizationOptions is not configured. Please register `IOptions<RequestLocalizationOptions>` to your service collection.");
        }

        if (requestLocalizationOptions?.Value != null)
        {
            var localizationOptions = requestLocalizationOptions.Value;

            // special handling of Storyblok preview URLs that contain the language, like ~/de/home vs. ~/home
            // if we have such a URL, we also change the current culture accordingly
            localizationOptions.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
            {
                var slug = context.Request.Path.ToString().Trim('/');
                var supportedCultures = options?.Value.SupportedCultures ?? [];

                foreach (var supportedCulture in supportedCultures)
                {
                    if (slug.StartsWith($"{supportedCulture}/", StringComparison.OrdinalIgnoreCase) || slug.Equals(supportedCulture, StringComparison.OrdinalIgnoreCase))
                    {
                        return await Task.FromResult(new ProviderCultureResult(supportedCulture));
                    }
                }

                return await Task.FromResult<ProviderCultureResult?>(null);
            }));

            // this query parameter is added by Storyblok in preview mode. We ALWAYS want to use this one first so that the selected language in Storyblok UI matches the language displayed.
            localizationOptions.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(async context =>
            {
                var storyblokEditorLanguage = context.Request.Query["_storyblok_lang"].FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(storyblokEditorLanguage))
                {
                    var supportedCultures = options?.Value.SupportedCultures ?? [];
                    foreach (var supportedCulture in supportedCultures)
                    {
                        if (storyblokEditorLanguage.Equals(supportedCulture, StringComparison.OrdinalIgnoreCase)) return await Task.FromResult(new ProviderCultureResult(supportedCulture));
                    }

                    if (storyblokEditorLanguage.Equals("default", StringComparison.OrdinalIgnoreCase)) return await Task.FromResult(new ProviderCultureResult(supportedCultures.First()));
                }

                return await Task.FromResult<ProviderCultureResult?>(null);
            }));

            if (options?.Value != null)
            {
                foreach (var culture in options.Value.SupportedCultures)
                {
                    if (!localizationOptions.SupportedCultures?.Any(x => x.Name.Equals(culture, StringComparison.OrdinalIgnoreCase)) ?? false)
                    {
                        localizationOptions.SupportedCultures?.Add(new CultureInfo(culture));
                    }

                    if (!localizationOptions.SupportedUICultures?.Any(x => x.Name.Equals(culture, StringComparison.OrdinalIgnoreCase)) ?? false)
                    {
                        localizationOptions.SupportedUICultures?.Add(new CultureInfo(culture));
                    }
                }
            }
        }

        app.MapWhen(context => options?.Value.EnableSitemap == true && context.Request.Path.StartsWithSegments("/sitemap.xml", StringComparison.OrdinalIgnoreCase),
            appBuilder => { appBuilder.UseMiddleware<StoryblokSitemapMiddleware>(); });

        app.MapWhen(
            context => !string.IsNullOrWhiteSpace(options?.Value.SlugForClearingCache) &&
                       context.Request.Path.StartsWithSegments("/" + options.Value.SlugForClearingCache.Trim('/'), StringComparison.OrdinalIgnoreCase),
            appBuilder => { appBuilder.UseMiddleware<StoryblokClearCacheMiddleware>(); });

        app.UseRequestLocalization();
        app.UseMiddleware<StoryblokMiddleware>();

        return app;
    }
}
