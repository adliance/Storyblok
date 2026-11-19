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
/// <remarks>
/// Requests never wait for the story list to be loaded:
/// <list type="bullet">
/// <item>If there is no index yet (cold start, after <see cref="Clear"/> or after a failed load), it is loaded in the background and <c>null</c> is returned meanwhile.</item>
/// <item>If the index is older than <see cref="StoryblokOptions.CacheDurationSeconds"/>, the old index is still used while a new one is loaded in the background.</item>
/// </list>
/// </remarks>
public class StoryblokStoryIndex(
    IOptions<StoryblokOptions> settings,
    IHttpClientFactory clientFactory,
    IMemoryCache memoryCache,
    ILogger<StoryblokBaseClient> clientLogger,
    ILogger<StoryblokStoryIndex> logger,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, CultureIndex> _indexes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether a story with the specified slug exists in the published stories of the specified culture.
    /// Never waits for the index to be loaded.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c> if the index could answer reliably, <c>null</c> if no reliable answer is possible (index not loaded yet, load failed, unexpected slug, ...).</returns>
    public bool? Exists(CultureInfo? culture, string slug)
    {
        if (!IsEnabled)
        {
            return null;
        }

        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return null;
        }

        var resolvedCulture = StoryblokBaseClient.ResolveCulture(settings.Value, culture);
        var slugs = GetSlugs(resolvedCulture).Slugs;
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
                var cultureSlugs = GetSlugs(new CultureInfo(supportedCulture)).Slugs;
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
    /// Starts loading the index for all supported cultures in the background, unless it is already loaded and up to date (or currently loading).
    /// </summary>
    /// <returns>A task that completes when all running loads have finished. Loading errors are logged, but never thrown.</returns>
    public Task Preload()
    {
        if (!IsEnabled)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(settings.Value.SupportedCultures.Select(x => GetSlugs(new CultureInfo(x)).Loading));
    }

    /// <summary>
    /// Discards the index of all cultures, e.g. because stories have been published.
    /// Until the index is loaded again, <see cref="Exists"/> returns <c>null</c> (i.e. the Storyblok API is used).
    /// Loads that are still running are discarded as well, because they might contain outdated data.
    /// </summary>
    public void Clear()
    {
        foreach (var index in _indexes.Values)
        {
            lock (index.Lock)
            {
                index.Generation++;
                index.Slugs = null;
                index.LoadedAt = default;
                index.RetryAfter = default;
                index.Loading = null;
            }
        }
    }

    private bool IsEnabled => settings.Value.CacheDurationSeconds > 0;

    private (HashSet<string>? Slugs, Task Loading) GetSlugs(CultureInfo culture)
    {
        var cultureName = culture.ToString().ToLowerInvariant();
        var index = _indexes.GetOrAdd(cultureName, _ => new CultureIndex());

        lock (index.Lock)
        {
            var now = timeProvider.GetUtcNow();
            var isOutdated = index.Slugs == null || now - index.LoadedAt >= TimeSpan.FromSeconds(settings.Value.CacheDurationSeconds);

            // only one load per culture at a time, so that a burst of requests doesn't load the story list multiple times
            if (isOutdated && index.Loading == null && now >= index.RetryAfter)
            {
                logger.LogTrace(index.Slugs == null
                    ? $"Story index for culture \"{cultureName}\" not available, loading it in the background."
                    : $"Story index for culture \"{cultureName}\" is outdated, reloading it in the background.");

                // the lock is still held here, so the load can't complete (and reset Loading) before Loading is assigned
                index.Loading = LoadInBackground(index, culture, cultureName, index.Generation);
            }

            return (index.Slugs, index.Loading ?? Task.CompletedTask);
        }
    }

    private Task LoadInBackground(CultureIndex index, CultureInfo culture, string cultureName, int generation)
    {
        return Task.Run(async () =>
        {
            HashSet<string>? slugs = null;
            Exception? error = null;
            try
            {
                slugs = await LoadSlugs(culture);
            }
            catch (Exception ex)
            {
                error = ex;
            }

            lock (index.Lock)
            {
                if (index.Generation != generation)
                {
                    // the index has been cleared while loading, so the loaded slugs might already be outdated
                    logger.LogTrace($"Discarding story index for culture \"{cultureName}\", because it has been cleared while loading.");
                    return;
                }

                index.Loading = null;
                var now = timeProvider.GetUtcNow();

                if (slugs != null)
                {
                    index.Slugs = slugs;
                    index.LoadedAt = now;
                    index.RetryAfter = default;
                    logger.LogTrace($"Story index for culture \"{cultureName}\" loaded with {slugs.Count} slugs.");
                }
                else
                {
                    // fail open: without index, every request is answered by the Storyblok API like before
                    // an outdated index is kept, because it's still better than calling the Storyblok API for every unknown slug
                    // either way, we don't retry immediately to not hammer the Storyblok API
                    index.RetryAfter = now + RetryAfterFailure;
                    logger.LogWarning(error, index.Slugs == null
                        ? $"Unable to load story index for culture \"{cultureName}\", falling back to the Storyblok API."
                        : $"Unable to reload story index for culture \"{cultureName}\", keeping the outdated index.");
                }
            }
        });
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

    private sealed class CultureIndex
    {
        public readonly Lock Lock = new();

        /// <summary>The slugs of the last successful load, <c>null</c> if there is none.</summary>
        public HashSet<string>? Slugs;

        public DateTimeOffset LoadedAt;

        /// <summary>After a failed load, no new load is started before this point in time.</summary>
        public DateTimeOffset RetryAfter;

        /// <summary>The currently running load, <c>null</c> if none is running.</summary>
        public Task? Loading;

        /// <summary>Incremented by <see cref="StoryblokStoryIndex.Clear"/>, so that loads started before clearing are discarded.</summary>
        public int Generation;
    }
}
