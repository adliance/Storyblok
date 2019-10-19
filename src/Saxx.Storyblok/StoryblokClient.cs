using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Saxx.Storyblok.Extensions;
using Saxx.Storyblok.Settings;

namespace Saxx.Storyblok
{
    public class StoryblokClient
    {
        private readonly IMemoryCache _memoryCache;
        private readonly HttpClient _client;
        private readonly bool _isInEditor;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly int _cacheDuration;
        private readonly CultureInfo[] _cultures;

        public StoryblokClient(StoryblokSettings settings, IHttpClientFactory clientFactory, IHttpContextAccessor httpContext, IMemoryCache memoryCache)
        {
            _client = clientFactory.CreateClient();
            _memoryCache = memoryCache;
            _isInEditor = httpContext?.HttpContext?.Request?.Query?.IsInStoryblokEditor(settings) ?? false;

            ValidateSettings(settings);
            _cacheDuration = settings.CacheDurationSeconds;
            _cultures = settings.Cultures ?? new CultureInfo[0];
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
                return cachedStories;
            }

            var url = $"{_baseUrl}/stories?token={_apiKey}&starts_with={startsWith}&excluding_fields={excludingFields}";
            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var stories = JsonConvert.DeserializeObject<StoryblokStoriesContainer>(responseString);

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
                return cachedStory;
            }

            var cacheKeyUnavailable = "404_" + cacheKey;
            if (_memoryCache.TryGetValue(cacheKeyUnavailable, out _))
            {
                return null;
            }

            var story = await LoadStoryFromStoryblok(culture, slug);
            if (story != null)
            {
                _memoryCache.Set(cacheKey, story, TimeSpan.FromSeconds(_cacheDuration));
                return story;
            }

            _memoryCache.Set(cacheKeyUnavailable, true, TimeSpan.FromSeconds(_cacheDuration));
            return null;
        }

        private async Task<StoryblokStory> LoadStoryFromStoryblok(CultureInfo culture, string slug)
        {
            var url = $"{_baseUrl}/stories/{slug}?token={_apiKey}";

            // add the culture to the URL, as long as it's not the default culture
            if (_cultures.Length > 1 && culture != null && !_cultures[0].ToString().Equals(culture.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                url = $"{_baseUrl}/stories/{culture.ToString().ToLower()}/{slug}?token={_apiKey}";
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
