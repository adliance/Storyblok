using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Saxx.Storyblok.Example.ViewModels.Shared;
using Saxx.Storyblok.Extensions;

namespace Saxx.Storyblok.Example
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddStoryblok(options =>
            {
                options.HandleRootWithSlug = "home";
                options.DefaultCulture = new CultureInfo("en");
                options.CultureMappings[new CultureInfo("en-US")] = options.DefaultCulture;
                options.CultureMappings[new CultureInfo("de-AT")] = new CultureInfo("de");
                options.CultureMappings[new CultureInfo("de-DE")] = new CultureInfo("de");
                options.CultureMappings[new CultureInfo("de-CH")] = new CultureInfo("en");
                options.CultureMappings[new CultureInfo("de")] = new CultureInfo("de");
                options.IgnoreSlugs.Add("blog/*");
            });
            services.AddScoped<HeaderViewModel>();
            services.AddControllersWithViews();
            services.AddHealthChecks().AddStoryblok();
        }

        public void Configure(IApplicationBuilder app)
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en"), 
                new CultureInfo("en-US"), 
                new CultureInfo("de-AT"), 
                new CultureInfo("de-DE"),
                new CultureInfo("de-CH"),
                new CultureInfo("de"),
            };
            app.UseRequestLocalization(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(supportedCultures[0].Name, supportedCultures[0].Name);
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            // these error pages are also stories on Storyblok
            app.UseExceptionHandler("/error/500");
            app.UseStatusCodePagesWithReExecute("/error/{0}");

            // the usual
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // add Storyblok before the MVC middleware, so we render the Storyblok story if it exists, or fall back to controller actions
            app.UseStoryblok();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapHealthChecks("health");
            });
        }
    }
}
