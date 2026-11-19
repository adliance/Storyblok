using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Middleware;

public class StoryblokClearCacheMiddlewareTest
{
    private readonly MockedWebApplicationFactory<MockedStartup> _factory = new();

    [Fact]
    public async Task Responds_With_Cleared_Cache()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/clear-storyblok-cache");
        await AssertCacheCleared(response);
    }

    [Fact]
    public async Task Responds_With_Cleared_Cache_For_Configured_Slug()
    {
        var client = _factory.CreateClient();
        var options = _factory.Services.GetRequiredService<IOptions<StoryblokOptions>>();
        options.Value.SlugForClearingCache = "my-own-SLUG";

        var response = await client.GetAsync("/my-own-slug");
        await AssertCacheCleared(response);
    }

    [Fact]
    public async Task Does_Not_Respond_With_Cleared_Cache_If_Disabled()
    {
        var client = _factory.CreateClient();
        var options = _factory.Services.GetRequiredService<IOptions<StoryblokOptions>>();
        options.Value.SlugForClearingCache = "";

        await AssertNotFound(await client.GetAsync("/clear-storyblok-cache"));
    }

    [Theory]
    [InlineData("/seite")]
    [InlineData("/verzeichnis/sitemap.xml")]
    [InlineData("/sitemap/seite")]
    public async Task Does_Not_Respond_With_Cleared_Cache(string url)
    {
        var client = _factory.CreateClient();
        await AssertNotFound(await client.GetAsync(url));
    }

    [Theory]
    [InlineData("203.0.113.10", HttpStatusCode.OK)]
    [InlineData("::ffff:203.0.113.10", HttpStatusCode.OK)]
    [InlineData("198.51.100.42", HttpStatusCode.OK)]
    [InlineData("203.0.113.11", HttpStatusCode.Forbidden)]
    [InlineData("198.51.101.1", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Forbidden)]
    public async Task Respects_Allowed_Ip_Addresses(string? remoteIpAddress, HttpStatusCode expectedStatusCode)
    {
        _factory.CreateClient();
        var options = _factory.Services.GetRequiredService<IOptions<StoryblokOptions>>();
        options.Value.ClearCacheAllowedIpAddresses = ["203.0.113.10", "198.51.100.0/24", "invalid"];

        var context = await _factory.Server.SendAsync(c =>
        {
            c.Request.Method = "POST";
            c.Request.Path = "/clear-storyblok-cache";
            c.Connection.RemoteIpAddress = remoteIpAddress == null ? null : IPAddress.Parse(remoteIpAddress);
        });

        Assert.Equal((int)expectedStatusCode, context.Response.StatusCode);
    }

    [Fact]
    public async Task Allows_All_Ip_Addresses_By_Default()
    {
        _factory.CreateClient();
        var context = await _factory.Server.SendAsync(c =>
        {
            c.Request.Method = "POST";
            c.Request.Path = "/clear-storyblok-cache";
            c.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");
        });

        Assert.Equal((int)HttpStatusCode.OK, context.Response.StatusCode);
    }

    private static async Task AssertNotFound(HttpResponseMessage response)
    {
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual("Cache cleared.", responseString);
    }

    private static async Task AssertCacheCleared(HttpResponseMessage response)
    {
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Cache cleared.", responseString);
    }
}
