using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Adliance.Storyblok.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Clients;

public class StoryblokStoryClient(
    IOptions<StoryblokOptions> settings,
    IHttpClientFactory clientFactory,
    IHttpContextAccessor httpContext,
    IMemoryCache memoryCache,
    ILogger<StoryblokBaseClient> logger,
    StoryblokStoryIndex storyIndex)
    : StoryblokBaseClient(settings, clientFactory, httpContext, memoryCache, logger)
{
    public StoryblokStoryQuery Story()
    {
        return new(this);
    }

    internal async Task<StoryblokStory<T>?> LoadStory<T>(CultureInfo? culture, string slug, ResolveLinksType resolveLinks, bool resolveAssets, string resolveRelations) where T : StoryblokComponent
    {
        var story = await LoadStory(culture, slug, resolveLinks, resolveAssets, resolveRelations);
        if (story == null)
        {
            return null;
        }

        return new StoryblokStory<T>(story);
    }

    internal async Task<StoryblokStory?> LoadStory(CultureInfo? culture, string slug, ResolveLinksType resolveLinks, bool resolveAssets, string resolveRelations)
    {
        if (IsInEditor)
        {
            return await LoadStoryFromStoryblok(culture, slug, resolveLinks, resolveAssets, resolveRelations);
        }

        var cacheKey = $"{culture}_{slug}_{resolveLinks}_{resolveAssets}_{resolveRelations}";
        if (MemoryCache.TryGetValue(cacheKey, out StoryblokStory? cachedStory) && cachedStory != null)
        {
            Logger.LogTrace($"Using cached story for slug \"{slug}\" (culture \"{culture}\").");
            return cachedStory;
        }

        var cacheKeyUnavailable = "404_" + cacheKey;
        if (MemoryCache.TryGetValue(cacheKeyUnavailable, out _))
        {
            Logger.LogTrace($"Using cached 404 for slug \"{slug}\" (culture \"{culture}\").");
            return null;
        }

        if (IsMissingInStoryIndex(culture, slug))
        {
            Logger.LogTrace($"Slug \"{slug}\" (culture \"{culture}\") not found in story index, skipping Storyblok API.");
            MemoryCache.Set(cacheKeyUnavailable, true, TimeSpan.FromSeconds(Settings.CacheDurationSeconds));
            return null;
        }

        Logger.LogTrace($"Trying to load story for slug \"{slug}\" (culture \"{culture}\").");
        var story = await LoadStoryFromStoryblok(culture, slug, resolveLinks, resolveAssets, resolveRelations);
        if (story != null)
        {
            Logger.LogTrace($"Story loaded for slug \"{slug}\" (culture \"{culture}\").");
            MemoryCache.Set(cacheKey, story, TimeSpan.FromSeconds(Settings.CacheDurationSeconds));
            return story;
        }

        Logger.LogTrace($"Story not found for slug \"{slug}\" (culture \"{culture}\").");
        MemoryCache.Set(cacheKeyUnavailable, true, TimeSpan.FromSeconds(Settings.CacheDurationSeconds));
        return null;
    }

    private bool IsMissingInStoryIndex(CultureInfo? culture, string slug)
    {
        // the index only contains published stories, so it must never be used for the editor (new pages are usually unpublished) or when drafts are included
        if (!Settings.UseStoryIndexForNotFound || IsInEditor || Settings.IncludeDraftStories)
        {
            return false;
        }

        return storyIndex.Exists(ResolveCulture(culture), slug) == false;
    }

    private async Task<StoryblokStory?> LoadStoryFromStoryblok(CultureInfo? culture, string slug, ResolveLinksType resolveLinks, bool resolveAssets, string resolveRelations)
    {
        var defaultCulture = new CultureInfo(Settings.SupportedCultures.First());
        var currentCulture = ResolveCulture(culture);

        var url = $"{Settings.BaseUrl}/stories/{slug}?token={ApiKey}";
        if (!currentCulture.Equals(defaultCulture))
        {
            url = $"{Settings.BaseUrl}/stories/{currentCulture.ToString().ToLower()}/{slug}?token={ApiKey}";
        }

        url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

        if (resolveLinks != ResolveLinksType.None)
        {
            url += $"&resolve_links={resolveLinks.ToString().ToLower()}";
        }

        // should only work in Premium Plans, (as per https://www.storyblok.com/docs/api/content-delivery/v2)
        if (resolveAssets)
        {
            url += "&resolve_assets=1";
        }

        if (!string.IsNullOrWhiteSpace(resolveRelations))
        {
            url += $"&resolve_relations={resolveRelations}";
        }

        if (Settings.IncludeDraftStories || IsInEditor)
        {
            url += "&version=draft";
        }

        Logger.LogTrace($"Loading {url} ...");

        var response = await Client.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();

        var story = JsonSerializer.Deserialize<StoryblokStoryContainer>(responseString, JsonOptions)?.Story;
        if (story == null)
        {
            throw new Exception($"Unable to deserialize {responseString}.");
        }

        story.LoadedAt = DateTime.UtcNow;
        return story;
    }
}
