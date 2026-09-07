using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Adliance.Storyblok.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Middleware;

public class StoryblokMiddlewareIgnoredSlugsTest
{
    private readonly MockedWebApplicationFactory<MockedStartup> _mockClientFactory;
    private readonly HttpClient _client;

    public StoryblokMiddlewareIgnoredSlugsTest()
    {
        _mockClientFactory = new MockedWebApplicationFactory<MockedStartup>(true);
        var clientOptions = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        };
        _client = _mockClientFactory.CreateClient(clientOptions);
    }

    [Theory]
    [InlineData("/ignored-slug-exact")]
    [InlineData("/ignored/test")]
    [InlineData("/ignored")]
    [InlineData("/ignored/")]
    [InlineData("/wp-content/uploads")]
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
            IgnoreSlugs = ["/ignored-slug-exact", "/ignored/*", "*.ign", "/wp-content/*"]
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

    [Theory]
    [InlineData("/ignored", false)]
    [InlineData("/test.ign", false)]
    [InlineData("/subfolder/test.ign", false)]
    [InlineData("/ignored/", false)]
    [InlineData("/.git", false)]
    [InlineData("/de/.git", false)]
    [InlineData("/.git/", false)]
    [InlineData("/.git/config", false)]
    [InlineData("/de/ignored", false)]
    [InlineData("/de/ignored/", false)]
    [InlineData("/en/ignored", false)]
    [InlineData("/en/ignored/", false)]
    [InlineData("/es/ignored", true)]
    [InlineData("/de/subfolder/test.ign", false)]
    [InlineData("/en/subfolder/test.ign", false)]
    [InlineData("/ignored-start/test", false)]
    [InlineData("/de/ignored-start/test", false)]
    [InlineData("/en/ignored-start/test", false)]
    [InlineData("/not-ignored", true)]
    public async Task RespectsIgnoredSlugsInFullPipeline(string url, bool expectRequestToStoryblok)
    {
        var handler = _mockClientFactory.Services.GetRequiredService<MockHttpMessageHandler>();
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        if (expectRequestToStoryblok)
        {
            Assert.Contains(handler.Requests, x => x.Contains(url));
        }
        else
        {
            Assert.DoesNotContain(handler.Requests, x => x.Contains(url));
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
