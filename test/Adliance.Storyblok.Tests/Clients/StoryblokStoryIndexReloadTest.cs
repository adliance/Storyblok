using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients;

/// <summary>
/// Tests the background (re)loading of the story index with a mocked Storyblok API and a controllable clock.
/// </summary>
public class StoryblokStoryIndexReloadTest
{
    private static readonly CultureInfo Culture = new("de");

    private readonly MockHttpMessageHandler _handler = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly ManualTimeProvider _time = new();
    private readonly StoryblokStoryIndex _index;
    private string[] _publishedSlugs = ["known-page"];

    public StoryblokStoryIndexReloadTest()
    {
        var options = new StoryblokOptions
        {
            ApiKeyPublic = "public-key",
            SupportedCultures = ["de"],
            CacheDurationSeconds = 60,
            UseStoryIndexForNotFound = true
        };

        _handler.Responder = ServeStoriesList;
        _index = new StoryblokStoryIndex(
            new OptionsWrapper<StoryblokOptions>(options),
            new MockClientFactory(_handler),
            _memoryCache,
            NullLogger<StoryblokBaseClient>.Instance,
            NullLogger<StoryblokStoryIndex>.Instance,
            _time);
    }

    [Fact]
    public async Task Returns_Null_Until_Loaded()
    {
        Assert.Null(_index.Exists(Culture, "known-page"));
        await _index.Preload();
        Assert.True(_index.Exists(Culture, "known-page"));
        Assert.False(_index.Exists(Culture, "new-page"));
    }

    [Fact]
    public async Task Uses_Outdated_Index_While_Reloading_In_Background()
    {
        await _index.Preload();
        Assert.False(_index.Exists(Culture, "new-page"));

        _publishedSlugs = ["known-page", "new-page"];
        _time.Advance(TimeSpan.FromSeconds(61));
        _memoryCache.Compact(1.0); // the cached story list expires together with the index
        _handler.Requests.Clear();

        // the outdated index is still used, the request does not wait for the new one
        Assert.False(_index.Exists(Culture, "new-page"));

        await _index.Preload(); // waits for the reload started above
        Assert.Single(_handler.Requests);
        Assert.True(_index.Exists(Culture, "new-page"));
    }

    [Fact]
    public async Task Does_Not_Reload_Up_To_Date_Index()
    {
        await _index.Preload();
        _handler.Requests.Clear();

        _time.Advance(TimeSpan.FromSeconds(59));
        Assert.True(_index.Exists(Culture, "known-page"));
        await _index.Preload();
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task Keeps_Outdated_Index_If_Reload_Fails()
    {
        await _index.Preload();

        _handler.Responder = null; // the list request now fails with 404
        _time.Advance(TimeSpan.FromSeconds(61));
        _memoryCache.Compact(1.0);
        _handler.Requests.Clear();

        Assert.False(_index.Exists(Culture, "new-page"));
        await _index.Preload();
        Assert.Single(_handler.Requests);

        // still answered from the outdated index, and no new attempt right after the failure
        Assert.True(_index.Exists(Culture, "known-page"));
        await _index.Preload();
        Assert.Single(_handler.Requests);

        // retried after a while
        _handler.Responder = ServeStoriesList;
        _time.Advance(TimeSpan.FromSeconds(31));
        _index.Exists(Culture, "known-page");
        await _index.Preload();
        Assert.Equal(2, _handler.Requests.Count);
    }

    [Fact]
    public async Task Discards_Index_On_Clear()
    {
        await _index.Preload();
        _index.Clear();

        Assert.Null(_index.Exists(Culture, "known-page"));
        await _index.Preload();
        Assert.True(_index.Exists(Culture, "known-page"));
    }

    private HttpResponseMessage ServeStoriesList(HttpRequestMessage request)
    {
        var stories = string.Join(",", _publishedSlugs.Select(x => $$"""{ "full_slug": "{{x}}" }"""));
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{ "stories": [{{stories}}] }""", Encoding.UTF8, "application/json")
        };
        response.Headers.Add("Total", _publishedSlugs.Length.ToString(CultureInfo.InvariantCulture));
        return response;
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now += duration;
    }

    private sealed class MockClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
