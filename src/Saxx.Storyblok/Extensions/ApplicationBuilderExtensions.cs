using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Saxx.Storyblok.Middleware;
using Saxx.Storyblok.Settings;

namespace Saxx.Storyblok.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseStoryblokRequestLocalization(this IApplicationBuilder app)
        {
            var settings = app.ApplicationServices.GetRequiredService<StoryblokSettings>();

            var supportedCultures = settings.Cultures ?? new CultureInfo[0];
            if (!supportedCultures.Any())
            {
                supportedCultures = new[] { CultureInfo.CurrentUICulture };
            }
            app.UseRequestLocalization(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(supportedCultures[0].Name, supportedCultures[0].Name);
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            return app;
        }

        public static IApplicationBuilder UseStoryblok(this IApplicationBuilder app)
        {
            app.UseMiddleware<StoryblokMiddleware>();
            return app;
        }
    }
}
