using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Saxx.Storyblok.Clients
{
    public class StoryblokStoryClient : StoryblokBaseClient
    {
        public StoryblokStoryClient(
            IOptions<StoryblokOptions> settings,
            IHttpClientFactory clientFactory,
            IHttpContextAccessor httpContext,
            IMemoryCache memoryCache,
            ILogger<StoryblokBaseClient> logger) : base(settings, clientFactory, httpContext, memoryCache, logger)
        {
        }

        public StoryblokStoryQuery Story()
        {
            return new StoryblokStoryQuery(this);
        }

        internal async Task<StoryblokStory<T>?> LoadStory<T>(CultureInfo? culture, string slug) where T : StoryblokComponent
        {
            var story = await LoadStory(culture, slug);
            if (story == null)
            {
                return null;
            }

            return new StoryblokStory<T>(story);
        }

        internal async Task<StoryblokStory?> LoadStory(CultureInfo? culture, string slug)
        {
            if (IsInEditor)
            {
                return await LoadStoryFromStoryblok(culture, slug);
            }

            var cacheKey = $"{culture}_{slug}";
            if (MemoryCache.TryGetValue(cacheKey, out StoryblokStory cachedStory))
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

            Logger.LogTrace($"Trying to load story for slug \"{slug}\" (culture \"{culture}\").");
            var story = await LoadStoryFromStoryblok(culture, slug);
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

        private async Task<StoryblokStory?> LoadStoryFromStoryblok(CultureInfo? culture, string slug)
        {
            var defaultCulture = new CultureInfo(Settings.SupportedCultures.First());
            var currentCulture = defaultCulture;

            if (culture != null)
            {
                // only use the culture if it's actually supported
                // use only the short culture "en", if we get a full culture "en-US" but only support the short one
                var matchingCultures = Settings.SupportedCultures
                    .Where(x => x.Equals(culture.ToString(), StringComparison.OrdinalIgnoreCase) || x.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Length)
                    .ToArray();

                if (matchingCultures.Any())
                {
                    currentCulture = new CultureInfo(matchingCultures.First());
                }
            }

            var url = $"{Settings.BaseUrl}/stories/{slug}?token={ApiKey}";
            if (!currentCulture.Equals(defaultCulture))
            {
                url = $"{Settings.BaseUrl}/stories/{currentCulture.ToString().ToLower()}/{slug}?token={ApiKey}";
            }

            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            if (Settings.IncludeDraftStories || IsInEditor)
            {
                url += "&version=draft";
            }

            var response = await Client.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var story = JsonSerializer.Deserialize<StoryblokStoryContainer>(responseString,  JsonOptions).Story;
            story.LoadedAt = DateTime.UtcNow;
            return story;
        }
    }
}
