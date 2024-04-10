using Adliance.Storyblok.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Adliance.Storyblok.FulltextSearch.Tests;

public class MockedStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddStoryblok(o =>
        {
            o.ApiKeyPublic = "bhbAxSYj2PjrAwleHntSfQtt"; // the public API key of our special Unit Test Storyblok Space, nothing confidential in there
            o.SupportedCultures = ["de", "en"];
            o.RedirectsDatasourceName = "redirects";
        });

        // we don't call AddStoryblokFulltext here because I don't want to register the background job here
        services.AddScoped<LuceneService>();
        services.AddScoped<MockedFulltextSearch>();
        services.AddScoped<FulltextSearchBase, MockedFulltextSearch>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseStoryblok();
    }
}
