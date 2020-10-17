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

                if (optionsValue.SupportedCultures.Length > 1)
                {
                    var supportedCultures = optionsValue.SupportedCultures.Select(x => new CultureInfo(x)).ToArray();
                    app.UseRequestLocalization(o =>
                    {
                        o.DefaultRequestCulture = new RequestCulture(supportedCultures[0].Name, supportedCultures[0].Name);
                        o.SupportedCultures = supportedCultures;
                        o.SupportedUICultures = supportedCultures;

                        if (!string.IsNullOrWhiteSpace(optionsValue.CultureCookieName))
                        {
                            if (o.RequestCultureProviders.FirstOrDefault(x => x.GetType() == typeof(CookieRequestCultureProvider)) is CookieRequestCultureProvider cookieProvider)
                            {
                                cookieProvider.CookieName = optionsValue.CultureCookieName;
                            }
                        }
                    });
                }
            }

            app.UseMiddleware<StoryblokMiddleware>();
            return app;
        }
    }
}