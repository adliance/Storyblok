using Microsoft.AspNetCore.Builder;
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
            });
            services.AddScoped<HeaderViewModel>();
            services.AddControllersWithViews();
        }

        public void Configure(IApplicationBuilder app)
        {
            // on top, so that the error messages are localized, too
            app.UseStoryblokRequestLocalization();

            // these error pages are also stories on Storyblok
            app.UseExceptionHandler("/error/500");
            app.UseStatusCodePagesWithReExecute("/error/{0}");

            // the usual
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // add Storyblok before the MVC middleware, so we render the Storyblok story if it exists, or fall back to controller actions
            app.UseStoryblok();

            // and finally, the MVC middleware
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
