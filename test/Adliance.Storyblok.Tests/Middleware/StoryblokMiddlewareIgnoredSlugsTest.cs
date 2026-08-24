using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Adliance.Storyblok.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Middleware;

public class StoryblokMiddlewareIgnoredSlugsTest
{
    [Theory]
    [InlineData("/ignored-slug-exact")]
    [InlineData("/ignored/test")]
    [InlineData("/test.ign")]
    public async Task RespectsIgnoredSlugs(string url)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Get,
                Path = url
            }
        };
        
        var options = new StoryblokOptions
        {
            IgnoreSlugs = ["/ignored-slug-exact", "/ignored/*", "*.ign"]
        };
        
        // Use unconfigured StoryblokStoryClient as it should never be called anyway.
        // If it is called due to a bug, a null reference exception will be thrown, which fails the test.
        var storyClient = CreateTestStoryClient();
        
        var nextCalled = false;

        var middleware = new StoryblokMiddleware(Next);
        
        await middleware.Invoke(context, storyClient, new OptionsWrapper<StoryblokOptions>(options),
            new NullLogger<StoryblokMiddleware>());
        
        var response = context.Response;
        Assert.True(nextCalled);
        Assert.Equal((int)HttpStatusCode.NotFound, response.StatusCode);
        return;

        Task Next(HttpContext ctx)
        {
            nextCalled = true;
            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Task.CompletedTask;
        }
    }

    private StoryblokStoryClient CreateTestStoryClient()
    {
        var settings = new StoryblokOptions
        {
            ApiKeyPublic = "unused-but-needed-for-validation",
            SupportedCultures = ["de"]
        };
        return new StoryblokStoryClient(new OptionsWrapper<StoryblokOptions>(settings), new MockClientFactory(), null!, null!, null!);
    }
    
    private class MockClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return null!;
        }
    }
}
