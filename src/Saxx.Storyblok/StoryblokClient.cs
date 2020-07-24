using System;
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
using Microsoft.Extensions.Options;
using Saxx.Storyblok.Attributes;
using Saxx.Storyblok.Converters;
using Saxx.Storyblok.Extensions;

namespace Saxx.Storyblok
{
    public class StoryblokClient
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<StoryblokClient> _logger;
        private readonly HttpClient _client;
        private readonly bool _isInEditor;
        private readonly StoryblokOptions _settings;

        public StoryblokClient(IOptions<StoryblokOptions> settings, IHttpClientFactory clientFactory, IHttpContextAccessor httpContext, IMemoryCache memoryCache, ILogger<StoryblokClient> logger)
        {
            _client = clientFactory.CreateClient();
            _memoryCache = memoryCache;
            _logger = logger;
            _settings = settings.Value;
            _isInEditor = httpContext?.HttpContext?.Request?.Query?.IsInStoryblokEditor(_settings) ?? false;

            ValidateSettings();
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            {
                throw new Exception("Storyblok API URL is missing in app settings.");
            }

            if (_isInEditor && string.IsNullOrWhiteSpace(_settings.ApiKeyPreview))
            {
                throw new Exception("Storyblok preview API key is missing in app settings.");
            }

            if (!_isInEditor && string.IsNullOrWhiteSpace(_settings.ApiKeyPublic))
            {
                throw new Exception("Storyblok public API key is missing in app settings.");
            }

            if (_settings.CacheDurationSeconds < 0)
            {
                throw new Exception("Cache duration (in seconds) must be equal or greater than zero.");
            }

            if (!_settings.SupportedCultures.Any())
            {
                throw new Exception("Define at least one supported culture.");
            }
        }

        // ReSharper disable once UnusedMember.Global
        public StoryblokStoriesQuery Stories()
        {
            return new StoryblokStoriesQuery(this, _settings);
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

        private string ApiKey => _settings.IncludeDraftStories || _isInEditor ? (_settings.ApiKeyPreview ?? "") : (_settings.ApiKeyPublic ?? "");

        internal async Task<IList<StoryblokStory>> LoadStories(string parameters)
        {

            var cacheKey = $"stories_{parameters}";
            if (_memoryCache.TryGetValue(cacheKey, out IList<StoryblokStory> cachedStories))
            {
                _logger.LogTrace($"Using cached stories for \"{parameters}\".");
                return cachedStories;
            }

            var url = $"{_settings.BaseUrl}/stories";
            url += $"?token={ApiKey}&{parameters.Trim('&')}";
            if (_settings.IncludeDraftStories || _isInEditor)
            {
                url += "&version=draft";
            }

            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            _logger.LogTrace($"Trying to load stories for \"{parameters}\".");

            var page = 0;
            var maxPage = 1;
            var result = new List<StoryblokStory>();

            while (++page <= maxPage)
            {
                var urlWithPage = url += $"&page={page}";

                var response = await _client.GetAsync(urlWithPage);
                response.EnsureSuccessStatusCode();

                if (page == 1 && response.Headers.Contains("total"))
                {
                    try
                    {
                        var total = int.Parse(response.Headers.First(x => x.Key.Equals("total", StringComparison.OrdinalIgnoreCase)).Value.First());
                        maxPage = (int) Math.Ceiling(total / (double) StoryblokStoriesQuery.PerPage);
                    }
                    catch
                    {
                        maxPage = 1;
                    }
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var stories = JsonSerializer.Deserialize<StoryblokStoriesContainer>(responseString, JsonOptions);

                var currentPageStories = stories.Stories.ToList();
                currentPageStories.ForEach(x => x.LoadedAt = DateTime.UtcNow);
                if (!currentPageStories.Any())
                {
                    // bail out if we didn't get any stories, maybe we did something wrong in page calculation
                    break;
                }

                result.AddRange(currentPageStories);
            }

            _logger.LogTrace($"Stories loaded for \"{parameters}\".");
            _memoryCache.Set(cacheKey, result, TimeSpan.FromSeconds(_settings.CacheDurationSeconds));
            return result;
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
                _memoryCache.Set(cacheKey, story, TimeSpan.FromSeconds(_settings.CacheDurationSeconds));
                return story;
            }

            _logger.LogTrace($"Story not found for slug \"{slug}\" (culture \"{culture}\").");
            _memoryCache.Set(cacheKeyUnavailable, true, TimeSpan.FromSeconds(_settings.CacheDurationSeconds));
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

        private async Task<StoryblokStory?> LoadStoryFromStoryblok(CultureInfo? culture, string slug)
        {
            var defaultCulture = new CultureInfo(_settings.SupportedCultures.First());
            var currentCulture = defaultCulture;

            if (culture != null)
            {
                // only use the culture if it's actually supported
                // use only the short culture "en", if we get a full culture "en-US" but only support the short one
                var matchingCultures = _settings.SupportedCultures
                    .Where(x => x.Equals(culture.ToString(), StringComparison.OrdinalIgnoreCase) || x.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Length)
                    .ToArray();

                if (matchingCultures.Any())
                {
                    currentCulture = new CultureInfo(matchingCultures.First());
                }
            }

            var url = $"{_settings.BaseUrl}/stories/{slug}?token={ApiKey}";
            if (!currentCulture.Equals(defaultCulture))
            {
                url = $"{_settings.BaseUrl}/stories/{currentCulture.ToString().ToLower()}/{slug}?token={ApiKey}";
            }

            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            if (_settings.IncludeDraftStories || _isInEditor)
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