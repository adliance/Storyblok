using Adliance.Storyblok.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Adliance.Storyblok.Tests;

public class MockedStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddStoryblok(o =>
        {
            o.ApiKeyPublic = "bhbAxSYj2PjrAwleHntSfQtt"; // the public API key of our special Unit Test Storyblok Space, nothing confidential in there
            o.SupportedCultures = new[] { "de", "en" };
            o.RedirectsDatasourceName = "redirects";
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseStoryblok();
    }
}
