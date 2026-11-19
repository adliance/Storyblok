using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Clients;

/// <summary>
/// A per-culture set of the slugs of all published stories, built from the same story list that is used for the sitemap.
/// It is used to answer requests for non-existing slugs without calling the Storyblok API for every single slug.
/// The index only ever answers "definitely missing"; whenever it is unsure, it returns <c>null</c> and the Storyblok API is asked.
/// </summary>
public class StoryblokStoryIndex(
    IOptions<StoryblokOptions> settings,
    IHttpClientFactory clientFactory,
    IMemoryCache memoryCache,
    ILogger<StoryblokBaseClient> clientLogger,
    ILogger<StoryblokStoryIndex> logger)
{
    private const string CacheKeyPrefix = "storyindex_";
    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether a story with the specified slug exists in the published stories of the specified culture.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c> if the index could answer reliably, <c>null</c> if no reliable answer is possible (index not available, load failed, unexpected slug, ...).</returns>
    public async Task<bool?> Exists(CultureInfo? culture, string slug)
    {
        if (settings.Value.CacheDurationSeconds <= 0)
        {
            return null;
        }

        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return null;
        }

        var resolvedCulture = StoryblokBaseClient.ResolveCulture(settings.Value, culture);
        var slugs = await GetSlugs(resolvedCulture);
        if (slugs == null)
        {
            return null;
        }

        if (slugs.Contains(normalizedSlug))
        {
            return true;
        }

        // slugs can also contain the culture ("en/my-page"), which makes Storyblok return the story in that culture
        foreach (var supportedCulture in settings.Value.SupportedCultures)
        {
            var culturePrefix = supportedCulture + "/";
            if (normalizedSlug.StartsWith(culturePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var cultureSlugs = await GetSlugs(new CultureInfo(supportedCulture));
                if (cultureSlugs == null)
                {
                    return null;
                }

                if (cultureSlugs.Contains(normalizedSlug.Substring(culturePrefix.Length)))
                {
                    return true;
                }
            }
        }

        // we can't be sure that we normalize non-ASCII characters the same way Storyblok does, so let the Storyblok API decide
        if (normalizedSlug.Any(c => c > 127))
        {
            return null;
        }

        return false;
    }

    /// <summary>
    /// Loads the index for all supported cultures (if not already cached), e.g. right after the cache has been cleared.
    /// </summary>
    public async Task Preload()
    {
        if (settings.Value.CacheDurationSeconds <= 0)
        {
            return;
        }

        foreach (var culture in settings.Value.SupportedCultures)
        {
            await GetSlugs(new CultureInfo(culture));
        }
    }

    private async Task<HashSet<string>?> GetSlugs(CultureInfo culture)
    {
        var cultureName = culture.ToString().ToLowerInvariant();
        var cacheKey = CacheKeyPrefix + cultureName;
        if (memoryCache.TryGetValue(cacheKey, out IndexEntry? entry) && entry != null)
        {
            return entry.Slugs;
        }

        // make sure that a burst of requests (e.g. right after clearing the cache) only loads the story list once
        var semaphore = _locks.GetOrAdd(cultureName, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            if (memoryCache.TryGetValue(cacheKey, out entry) && entry != null)
            {
                return entry.Slugs;
            }

            try
            {
                var slugs = await LoadSlugs(culture);
                memoryCache.Set(cacheKey, new IndexEntry(slugs), TimeSpan.FromSeconds(settings.Value.CacheDurationSeconds));
                logger.LogTrace($"Story index for culture \"{cultureName}\" loaded with {slugs.Count} slugs.");
                return slugs;
            }
            catch (Exception ex)
            {
                // fail open: without index, every request is answered by the Storyblok API like before
                logger.LogWarning(ex, $"Unable to load story index for culture \"{cultureName}\", falling back to the Storyblok API.");
                memoryCache.Set(cacheKey, new IndexEntry(null), FailureCacheDuration);
                return null;
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<HashSet<string>> LoadSlugs(CultureInfo culture)
    {
        // the index must always reflect the published state, therefore we use a client without HTTP context (= never in editor mode)
        // we use exactly the same query as the sitemap, so both share the same cached story list
        var client = new StoryblokStoriesClient(settings, clientFactory, null, memoryCache, clientLogger);
        var stories = await client.Stories()
            .ForCulture(culture)
            .ExcludingFields("content")
            .Load();

        var culturePrefix = culture.ToString() + "/";
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var story in stories)
        {
            var fullSlug = NormalizeSlug(story.FullSlug);
            Add(result, fullSlug);

            // for non-default cultures, the full slug starts with the culture ("en/my-page"), but the story is loaded as "my-page" with culture "en"
            if (fullSlug.StartsWith(culturePrefix, StringComparison.OrdinalIgnoreCase))
            {
                Add(result, fullSlug.Substring(culturePrefix.Length));
            }

            Add(result, NormalizeSlug(story.DefaultFullSlug));

            // translatable slugs app
            foreach (var translatedSlug in story.TranslatedSlugs)
            {
                Add(result, NormalizeSlug(translatedSlug.Path));
            }
        }

        return result;
    }

    private static void Add(HashSet<string> slugs, string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        slugs.Add(slug);

        // just to be on the safe side, if Storyblok returns URL-encoded slugs
        var unescaped = Uri.UnescapeDataString(slug);
        if (unescaped != slug)
        {
            slugs.Add(unescaped);
        }
    }

    private static string NormalizeSlug(string? slug)
    {
        // folder start pages are returned as "folder/", but requested as "folder"
        return (slug ?? "").Trim().Trim('/');
    }

    private sealed record IndexEntry(HashSet<string>? Slugs);
}
