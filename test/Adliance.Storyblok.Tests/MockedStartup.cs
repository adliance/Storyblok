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
            o.SupportedCultures = ["de", "en"];
            o.RedirectsDatasourceName = "redirects";
            o.AssetKey = "wqPkoW0jchgnNKRYog51xQtt"; // only the asset key to our special Storyblok Testing Space, nothing confidential in here
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseStoryblok();
    }
}
