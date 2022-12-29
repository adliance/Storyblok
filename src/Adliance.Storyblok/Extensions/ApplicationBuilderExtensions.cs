using System;
using System.Globalization;
using System.Linq;
using Adliance.Storyblok.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Extensions
{
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

                var supportedCultures = optionsValue.SupportedCultures.Select(x => new CultureInfo(x)).ToArray();
                if (!supportedCultures.Any()) supportedCultures = new[] { CultureInfo.CurrentUICulture };
                app.UseRequestLocalization(o =>
                {
                    o.DefaultRequestCulture = new RequestCulture(supportedCultures[0].Name, supportedCultures[0].Name);
                    o.SupportedCultures = supportedCultures;
                    o.SupportedUICultures = supportedCultures;

                    // don't load the culture from the HTTP request as this mixes stuff up with the cultured slugs of Storyblok
                    o.RequestCultureProviders = o.RequestCultureProviders.Where(x => x.GetType() != typeof(AcceptLanguageHeaderRequestCultureProvider)).ToList();

                    if (!string.IsNullOrWhiteSpace(optionsValue.CultureCookieName))
                    {
                        if (o.RequestCultureProviders.FirstOrDefault(x => x.GetType() == typeof(CookieRequestCultureProvider)) is CookieRequestCultureProvider cookieProvider)
                        {
                            cookieProvider.CookieName = optionsValue.CultureCookieName;
                        }
                    }
                });
            }

            app.MapWhen(context => options?.Value.EnableSitemap == true && context.Request.Path.StartsWithSegments("/sitemap.xml", StringComparison.OrdinalIgnoreCase), appBuilder => { appBuilder.UseMiddleware<StoryblokSitemapMiddleware>(); });

            app.MapWhen(context => !string.IsNullOrWhiteSpace(options?.Value.SlugForClearingCache) && context.Request.Path.StartsWithSegments("/" + options.Value.SlugForClearingCache.Trim('/'), StringComparison.OrdinalIgnoreCase),
                appBuilder => { appBuilder.UseMiddleware<StoryblokClearCacheMiddleware>(); });

            app.UseMiddleware<StoryblokMiddleware>();
            return app;
        }
    }
}