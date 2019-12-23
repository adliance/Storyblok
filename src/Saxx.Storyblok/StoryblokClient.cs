using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Saxx.Storyblok.Attributes;
using Saxx.Storyblok.Converters;
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
        private readonly bool _includeDraftStories;

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
            _apiKey = settings.IncludeDraftStories || _isInEditor ? settings.ApiKeyPreview : settings.ApiKeyPublic;
            _includeDraftStories = settings.IncludeDraftStories;
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

        public StoryblokStoriesQuery Stories()
        {
            return new StoryblokStoriesQuery(this);
        }

        public StoryblokStoryQuery Story()
        {
            return new StoryblokStoryQuery(this);
        }

        internal async Task<IList<StoryblokStory<T>>> LoadStories<T>(string parameters) where T : StoryblokComponent
        {
            // if we only want stories of a specific type, we should add the corresponding component filter to only load the required components from Storyblok
            var attribute = typeof(T).GetCustomAttribute<StoryblokComponentAttribute>();
            if (attribute != null)
            {
                parameters += $"&filter_query[component][in]={attribute.Name}";
            }

            var stories = await LoadStories(parameters);
            return stories.Select(x => new StoryblokStory<T>(x)).ToList();
        }

        internal async Task<IList<StoryblokStory>> LoadStories(string parameters)
        {
            var cacheKey = $"stories_{parameters}";
            if (_memoryCache.TryGetValue(cacheKey, out IList<StoryblokStory> cachedStories))
            {
                _logger.LogTrace($"Using cached stories for \"{parameters}\".");
                return cachedStories;
            }

            var url = $"{_baseUrl}/stories?token={_apiKey}&{parameters.Trim('&')}";
            if (_includeDraftStories || _isInEditor)
            {
                url += "&version=draft";
            }

            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            _logger.LogTrace($"Trying to load stories for \"{parameters}\".");
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var stories = JsonSerializer.Deserialize<StoryblokStoriesContainer>(responseString, JsonOptions);

            _logger.LogTrace($"Stories loaded for \"{parameters}\".");
            foreach (var s in stories.Stories)
            {
                s.LoadedAt = DateTime.UtcNow;
            }

            var result = stories.Stories.ToList();
            _memoryCache.Set(cacheKey, result, TimeSpan.FromSeconds(_cacheDuration));
            return result;
        }

        internal async Task<StoryblokStory<T>> LoadStory<T>(CultureInfo culture, string slug) where T : StoryblokComponent
        {
            return new StoryblokStory<T>(await LoadStory(culture, slug));
        }

        internal async Task<StoryblokStory> LoadStory(CultureInfo culture, string slug)
        {
            if (culture == null)
            {
                culture = _defaultCulture;
            }

            if (_isInEditor)
            {
                return await LoadStoryFromStoryblok(culture, slug);
            }

            var cacheKey = $"{culture}_{slug}";
            if (_memoryCache.TryGetValue(cacheKey, out StoryblokStory cachedStory))
            {
                _logger.LogTrace($"Using cached story for slug \"{slug}\" (culture \"{culture}\").");
                return cachedStory;
            }

            var cacheKeyUnavailable = "404_" + cacheKey;
            if (_memoryCache.TryGetValue(cacheKeyUnavailable, out _))
            {
                _logger.LogTrace($"Using cached 404 for slug \"{slug}\" (culture \"{culture}\").");
                return null;
            }

            _logger.LogTrace($"Trying to load story for slug \"{slug}\" (culture \"{culture}\").");
            var story = await LoadStoryFromStoryblok(culture, slug);
            if (story != null)
            {
                _logger.LogTrace($"Story loaded for slug \"{slug}\" (culture \"{culture}\").");
                _memoryCache.Set(cacheKey, story, TimeSpan.FromSeconds(_cacheDuration));
                return story;
            }

            _logger.LogTrace($"Story not found for slug \"{slug}\" (culture \"{culture}\").");
            _memoryCache.Set(cacheKeyUnavailable, true, TimeSpan.FromSeconds(_cacheDuration));
            return null;
        }

        public void ClearCache()
        {
            try
            {
                // this is sloow, but I was not able to find any other way to clear the memory cache
                var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                // ReSharper disable PossibleNullReferenceException
                var entriesCollection = field.GetValue(_memoryCache);
                var clearMethod = entriesCollection.GetType().GetMethod("Clear");
                clearMethod.Invoke(entriesCollection, null);
                // ReSharper restore PossibleNullReferenceException
                _logger.LogTrace("Cache cleared.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to clear cache.");
            }
        }

        private async Task<StoryblokStory> LoadStoryFromStoryblok(CultureInfo culture, string slug)
        {
            var language = _defaultCulture;

            if (culture != null && _cultureMappings.ContainsKey(culture))
            {
                language = _cultureMappings[culture];
            }

            var url = $"{_baseUrl}/stories/{slug}?token={_apiKey}";
            if (!language.Equals(_defaultCulture))
            {
                url = $"{_baseUrl}/stories/{language.ToString().ToLower()}/{slug}?token={_apiKey}";
            }

            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            if (_includeDraftStories || _isInEditor)
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

            var story = JsonSerializer.Deserialize<StoryblokStoryContainer>(responseString, JsonOptions).Story;
            story.LoadedAt = DateTime.UtcNow;
            return story;
        }

        private JsonSerializerOptions JsonOptions
        {
            get
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new StoryblokComponentConverter());
                options.Converters.Add(new StoryblokDateConverter());
                options.Converters.Add(new StoryblokNullableDateConverter());
                options.Converters.Add(new StoryblokIntConverter());
                options.Converters.Add(new StoryblokNullableIntConverter());
                options.Converters.Add(new StoryblokMarkdownConverter());
                return options;
            }
        }
    }
}