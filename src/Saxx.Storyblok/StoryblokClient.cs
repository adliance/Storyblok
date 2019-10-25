using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Saxx.Storyblok.Extensions;
using Saxx.Storyblok.Settings;

namespace Saxx.Storyblok
{
    public class StoryblokClient
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<StoryblokClient> _logger;
        private readonly HttpClient _client;
        private readonly bool _isInEditor;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly int _cacheDuration;
        private readonly IDictionary<CultureInfo, CultureInfo> _cultureMappings;
        private readonly CultureInfo _defaultCulture;

        public StoryblokClient(StoryblokSettings settings, IHttpClientFactory clientFactory, IHttpContextAccessor httpContext, IMemoryCache memoryCache, ILogger<StoryblokClient> logger)
        {
            _client = clientFactory.CreateClient();
            _memoryCache = memoryCache;
            _logger = logger;
            _isInEditor = httpContext?.HttpContext?.Request?.Query?.IsInStoryblokEditor(settings) ?? false;

            ValidateSettings(settings);
            _cacheDuration = settings.CacheDurationSeconds;
            _cultureMappings = settings.CultureMappings ?? new ConcurrentDictionary<CultureInfo, CultureInfo>();
            _defaultCulture = settings.DefaultCulture ?? CultureInfo.CurrentUICulture;
            _apiKey = _isInEditor ? settings.ApiKeyPreview : settings.ApiKeyPublic;
            _baseUrl = settings.BaseUrl;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void ValidateSettings(StoryblokSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            {
                throw new Exception("Storyblok API URL is missing in app settings.");
            }
            if (_isInEditor && string.IsNullOrWhiteSpace(settings.ApiKeyPreview))
            {
                throw new Exception("Storyblok preview API key is missing in app settings.");
            }
            if (!_isInEditor && string.IsNullOrWhiteSpace(settings.ApiKeyPublic))
            {
                throw new Exception("Storyblok public API key is missing in app settings.");
            }
            if (settings.CacheDurationSeconds < 0)
            {
                throw new Exception("Cache duration (in seconds) must be equal or greater than zero.");
            }
        }

        public async Task<IEnumerable<StoryblokStory>> LoadStories(string startsWith, string excludingFields = null)
        {
            var cacheKey = $"stories_{startsWith}_{excludingFields}";
            if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<StoryblokStory> cachedStories))
            {
                _logger.LogTrace($"Using cached stories for \"{startsWith}\".");
                return cachedStories;
            }

            var url = $"{_baseUrl}/stories?token={_apiKey}&starts_with={startsWith}&excluding_fields={excludingFields}";
            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            _logger.LogTrace($"Trying to load stories for \"{startsWith}\".");
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var stories = JsonConvert.DeserializeObject<StoryblokStoriesContainer>(responseString);

            _logger.LogTrace($"Stories loaded for \"{startsWith}\".");
            foreach (var s in stories.Stories)
            {
                s.LoadedAt = DateTime.UtcNow;
            }
            _memoryCache.Set(cacheKey, stories.Stories, TimeSpan.FromSeconds(_cacheDuration));
            return stories.Stories;
        }

        public async Task<StoryblokStory> LoadStory(CultureInfo culture, string slug)
        {
            if (_isInEditor)
            {
                return await LoadStoryFromStoryblok(culture, slug);
            }

            var cacheKey = $"{culture}_{slug}";
            if (_memoryCache.TryGetValue(cacheKey, out StoryblokStory cachedStory))
            {
                _logger.LogTrace($"Using cached story for slug \"{slug}\" (culture {culture}).");
                return cachedStory;
            }

            var cacheKeyUnavailable = "404_" + cacheKey;
            if (_memoryCache.TryGetValue(cacheKeyUnavailable, out _))
            {
                _logger.LogTrace($"Using cached 404 for slug \"{slug}\" (culture {culture}).");
                return null;
            }

            _logger.LogTrace($"Trying to load story for slug \"{slug}\" (culture {culture}).");
            var story = await LoadStoryFromStoryblok(culture, slug);
            if (story != null)
            {
                _logger.LogTrace($"Story loaded for slug \"{slug}\" (culture {culture}).");
                _memoryCache.Set(cacheKey, story, TimeSpan.FromSeconds(_cacheDuration));
                return story;
            }

            _logger.LogTrace($"Story not found for slug \"{slug}\" (culture {culture}).");
            _memoryCache.Set(cacheKeyUnavailable, true, TimeSpan.FromSeconds(_cacheDuration));
            return null;
        }

        private async Task<StoryblokStory> LoadStoryFromStoryblok(CultureInfo culture, string slug)
        {
            var language = _defaultCulture;
            if (_cultureMappings.ContainsKey(culture))
            {
                language = _cultureMappings[culture];
            }

            var url = $"{_baseUrl}/stories/{slug}?token={_apiKey}";
            if (!language.Equals(_defaultCulture))
            {
                url = $"{_baseUrl}/stories/{language.ToString().ToLower()}/{slug}?token={_apiKey}";
            }
            
            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            if (_isInEditor)
            {
                url += "&version=draft";
            }

            var response = await _client.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var story = JsonConvert.DeserializeObject<StoryblokStoryContainer>(responseString).Story;
            story.LoadedAt = DateTime.UtcNow;
            return story;
        }
    }
}
