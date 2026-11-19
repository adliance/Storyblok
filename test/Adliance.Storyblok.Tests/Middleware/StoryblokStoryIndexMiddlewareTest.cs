using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Middleware;

/// <summary>
/// Tests the story index (answering "not found" from the cached story list) with a mocked Storyblok API.
/// </summary>
public class StoryblokStoryIndexMiddlewareTest
{
    private const string StoriesJson = """
                                       {
                                         "stories": [
                                           { "full_slug": "known-page", "default_full_slug": "known-page", "translated_slugs": [{ "path": "known-page-translated", "lang": "en" }] },
                                           { "full_slug": "folder/", "default_full_slug": "folder/", "is_startpage": true },
                                           { "full_slug": "en/known-page-translated", "default_full_slug": "known-page" }
                                         ]
                                       }
                                       """;

    private readonly MockedWebApplicationFactory<MockedStartup> _factory;
    private readonly HttpClient _client;
    private readonly MockHttpMessageHandler _handler;
    private readonly StoryblokOptions _options;

    public StoryblokStoryIndexMiddlewareTest()
    {
        _factory = new MockedWebApplicationFactory<MockedStartup>(true);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _handler = _factory.Services.GetRequiredService<MockHttpMessageHandler>();
        _options = _factory.Services.GetRequiredService<IOptions<StoryblokOptions>>().Value;
        _options.UseStoryIndexForNotFound = true;
        _options.RedirectsDatasourceName = null; // not relevant here, and would only add requests to the mocked API
        _handler.Responder = ServeStoriesList;
    }

    [Theory]
    [InlineData("/does-not-exist")]
    [InlineData("/wp-admin/setup-config.php")]
    [InlineData("/.env")]
    [InlineData("/en/does-not-exist")]
    [InlineData("/de/does-not-exist")]
    public async Task Does_Not_Request_Unknown_Slug(string url)
    {
        await AssertNotFound(url);
        Assert.Contains(_handler.Requests, IsListRequest);
        Assert.DoesNotContain(_handler.Requests, IsSingleStoryRequest);
    }

    [Fact]
    public async Task Does_Not_Request_Anything_For_Second_Unknown_Slug()
    {
        await AssertNotFound("/does-not-exist");
        _handler.Requests.Clear();

        await AssertNotFound("/does-not-exist-either");
        Assert.Empty(_handler.Requests);
    }

    [Theory]
    [InlineData("/known-page", "/stories/known-page?")]
    [InlineData("/KNOWN-page/", "/stories/KNOWN-page?")]
    [InlineData("/folder", "/stories/folder?")]
    [InlineData("/en/known-page", "/stories/en/known-page?")]
    [InlineData("/en/known-page-translated", "/stories/en/known-page-translated?")]
    public async Task Requests_Known_Slug(string url, string expectedRequest)
    {
        await AssertNotFound(url); // the mocked API returns 404 for single stories
        Assert.Contains(_handler.Requests, x => x.Contains(expectedRequest));
    }

    [Fact]
    public async Task Requests_Unknown_Slug_In_Editor()
    {
        _options.ApiKeyPreview = "preview-key";
        var timestamp = ((long)Math.Floor((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        var token = Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes($"123:preview-key:{timestamp}")));

        await AssertNotFound($"/new-unpublished-page?_storyblok_tk[space_id]=123&_storyblok_tk[timestamp]={timestamp}&_storyblok_tk[token]={token}");
        Assert.Contains(_handler.Requests, x => x.Contains("/stories/new-unpublished-page?"));
        Assert.DoesNotContain(_handler.Requests, IsListRequest);
    }

    [Fact]
    public async Task Requests_Unknown_Slug_With_Draft_Stories()
    {
        _options.ApiKeyPreview = "preview-key";
        _options.IncludeDraftStories = true;

        await AssertNotFound("/does-not-exist");
        Assert.Contains(_handler.Requests, x => x.Contains("/stories/does-not-exist?"));
        Assert.DoesNotContain(_handler.Requests, IsListRequest);
    }

    [Fact]
    public async Task Requests_Unknown_Slug_If_Disabled()
    {
        _options.UseStoryIndexForNotFound = false;

        await AssertNotFound("/does-not-exist");
        Assert.Contains(_handler.Requests, x => x.Contains("/stories/does-not-exist?"));
        Assert.DoesNotContain(_handler.Requests, IsListRequest);
    }

    [Fact]
    public async Task Requests_Unknown_Slug_If_Index_Fails_To_Load()
    {
        _handler.Responder = null; // the list request now fails with 404

        await AssertNotFound("/does-not-exist");
        Assert.Contains(_handler.Requests, IsListRequest);
        Assert.Contains(_handler.Requests, x => x.Contains("/stories/does-not-exist?"));

        // the failure is cached for a short time, so the list is not requested again immediately
        _handler.Requests.Clear();
        await AssertNotFound("/does-not-exist-either");
        Assert.DoesNotContain(_handler.Requests, IsListRequest);
        Assert.Contains(_handler.Requests, x => x.Contains("/stories/does-not-exist-either?"));
    }

    [Fact]
    public async Task Reloads_Index_After_Clearing_Cache()
    {
        await AssertNotFound("/does-not-exist");
        Assert.Contains(_handler.Requests, IsListRequest);
        _handler.Requests.Clear();

        var response = await _client.GetAsync("/clear-storyblok-cache");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertNotFound("/does-not-exist");
        Assert.Contains(_handler.Requests, IsListRequest);
        Assert.DoesNotContain(_handler.Requests, IsSingleStoryRequest);
    }

    private async Task AssertNotFound(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static HttpResponseMessage? ServeStoriesList(HttpRequestMessage request)
    {
        if (request.RequestUri == null || !IsListRequest(request.RequestUri.ToString()))
        {
            return null;
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(StoriesJson, Encoding.UTF8, "application/json")
        };
        response.Headers.Add("Total", "3");
        return response;
    }

    private static bool IsListRequest(string url)
    {
        return new Uri(url).AbsolutePath.EndsWith("/stories", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSingleStoryRequest(string url)
    {
        return new Uri(url).AbsolutePath.Contains("/stories/", StringComparison.OrdinalIgnoreCase);
    }
}
