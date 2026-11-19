using System.Globalization;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients;

/// <summary>
/// Tests the story index against the real Storyblok test space.
/// </summary>
public class StoryblokStoryIndexTest
{
    private readonly MockedWebApplicationFactory<MockedStartup> _factory = new();
    private readonly StoryblokStoryIndex _index;
    private readonly StoryblokStoryClient _client;

    public StoryblokStoryIndexTest()
    {
        _factory.CreateClient();
        _factory.Services.GetRequiredService<IOptions<StoryblokOptions>>().Value.UseStoryIndexForNotFound = true;
        _index = _factory.Services.GetRequiredService<StoryblokStoryIndex>();
        _client = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<StoryblokStoryClient>();
    }

    [Theory]
    [InlineData("de", "page-sections-buttons", true)]
    [InlineData("de", "/page-sections-buttons/", true)]
    [InlineData("de", "page-translated-slug", true)]
    [InlineData("de", "en/page-sections-buttons", true)]
    [InlineData("de", "en/page-translated-english-slug", true)]
    [InlineData("en", "page-sections-buttons", true)]
    [InlineData("en", "page-translated-slug", true)]
    [InlineData("en", "page-translated-english-slug", true)]
    [InlineData("en-US", "page-translated-english-slug", true)]
    [InlineData("fr", "page-sections-buttons", true)]
    [InlineData("de", "does-not-exist", false)]
    [InlineData("en", "does-not-exist", false)]
    [InlineData("de", "en/does-not-exist", false)]
    [InlineData("de", "", null)]
    public async Task Knows_Existing_Slugs(string culture, string slug, bool? expected)
    {
        Assert.Equal(expected, await _index.Exists(new CultureInfo(culture), slug));
    }

    [Fact]
    public async Task Loads_Existing_Stories_And_Skips_Unknown_Ones()
    {
        Assert.NotNull(await _client.Story().WithSlug("page-sections-buttons").Load());
        Assert.NotNull(await _client.Story().WithSlug("/en/page-sections-buttons").Load());
        Assert.NotNull(await _client.Story().WithCulture(new CultureInfo("en")).WithSlug("page-translated-english-slug").Load());
        Assert.Null(await _client.Story().WithSlug("does-not-exist").Load());

        var cache = _factory.Services.GetRequiredService<IMemoryCache>();
        Assert.True(cache.TryGetValue("storyindex_de", out _));
    }
}
