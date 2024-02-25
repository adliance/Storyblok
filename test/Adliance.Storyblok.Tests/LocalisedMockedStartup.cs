using Adliance.Storyblok.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Adliance.Storyblok.Tests;

public class LocalisedMockedStartup
{
    public void ConfigureServices(IServiceCollection services)
    {

        services.AddRequestLocalization(o =>
        {
            o.AddSupportedCultures("en-NZ", "mi-NZ", "de");
            o.AddSupportedUICultures("en-NZ", "mi-NZ", "de");
            o.SetDefaultCulture("de");
        });

        services.AddStoryblok(o =>
        {
            o.ApiKeyPublic = "bhbAxSYj2PjrAwleHntSfQtt"; // the public API key of our special Unit Test Storyblok Space, nothing confidential in there
            o.SupportedCultures = new[] { "de", "en", "mi-NZ" };
            o.RedirectsDatasourceName = "redirects";
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRequestLocalization();
        app.UseStoryblok();
    }
}
