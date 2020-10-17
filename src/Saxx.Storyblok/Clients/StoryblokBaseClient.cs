using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Saxx.Storyblok.Converters;
using Saxx.Storyblok.Extensions;

namespace Saxx.Storyblok.Clients
{
    public abstract class StoryblokBaseClient
    {
        protected readonly IMemoryCache MemoryCache;
        protected readonly ILogger<StoryblokBaseClient> Logger;
        protected readonly HttpClient Client;
        internal static bool IsInEditor;
        protected readonly StoryblokOptions Settings;

        protected StoryblokBaseClient(IOptions<StoryblokOptions> settings, IHttpClientFactory clientFactory, IHttpContextAccessor? httpContext, IMemoryCache memoryCache, ILogger<StoryblokBaseClient> logger)
        {
            Client = clientFactory.CreateClient();
            MemoryCache = memoryCache;
            Logger = logger;
            Settings = settings.Value;
            IsInEditor = httpContext?.HttpContext?.Request?.Query?.IsInStoryblokEditor(Settings) ?? false;

            ValidateSettings();
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(Settings.BaseUrl))
            {
                throw new Exception("Storyblok API URL is missing in app settings.");
            }

            if (IsInEditor && string.IsNullOrWhiteSpace(Settings.ApiKeyPreview))
            {
                throw new Exception("Storyblok preview API key is missing in app settings.");
            }

            if (!IsInEditor && string.IsNullOrWhiteSpace(Settings.ApiKeyPublic))
            {
                throw new Exception("Storyblok public API key is missing in app settings.");
            }

            if (Settings.CacheDurationSeconds < 0)
            {
                throw new Exception("Cache duration (in seconds) must be equal or greater than zero.");
            }

            if (!Settings.SupportedCultures.Any())
            {
                throw new Exception("Define at least one supported culture.");
            }
        }

        protected string ApiKey => Settings.IncludeDraftStories || IsInEditor ? (Settings.ApiKeyPreview ?? "") : (Settings.ApiKeyPublic ?? "");
        
        public void ClearCache()
        {
            try
            {
                // this is sloow, but I was not able to find any other way to clear the memory cache
                var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                // ReSharper disable PossibleNullReferenceException
                var entriesCollection = field.GetValue(MemoryCache);
                var clearMethod = entriesCollection.GetType().GetMethod("Clear");
                clearMethod.Invoke(entriesCollection, null);
                // ReSharper restore PossibleNullReferenceException
                Logger.LogTrace("Cache cleared.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unable to clear cache.");
            }
        }

        protected bool IsDefaultCulture(CultureInfo culture)
        {
            return IsDefaultCulture(culture.ToString());
        }

        private bool IsDefaultCulture(string culture)
        {
            return Settings.SupportedCultures[0].Equals(culture, StringComparison.OrdinalIgnoreCase);
        }

        protected JsonSerializerOptions JsonOptions
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