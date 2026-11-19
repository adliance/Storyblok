using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Adliance.Storyblok.Converters;
using Adliance.Storyblok.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Clients;

public abstract class StoryblokBaseClient
{
    protected readonly IMemoryCache MemoryCache;
    protected readonly ILogger<StoryblokBaseClient> Logger;
    protected readonly HttpClient Client;
    protected readonly StoryblokOptions Settings;

    /// <summary>
    /// Whether the current request comes from the Storyblok editor (visual preview).
    /// This is evaluated per client instance (i.e. per request), so concurrent editor and public requests don't influence each other.
    /// </summary>
    internal bool IsInEditor { get; }

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

    /// <summary>
    /// Returns the supported culture that should be used to load content for the requested culture.
    /// Uses the exact culture if supported, otherwise the two-letter culture ("en" for "en-US") if supported, otherwise the default culture.
    /// </summary>
    internal CultureInfo ResolveCulture(CultureInfo? culture)
    {
        return ResolveCulture(Settings, culture);
    }

    internal static CultureInfo ResolveCulture(StoryblokOptions settings, CultureInfo? culture)
    {
        if (culture != null)
        {
            var matchingCulture = settings.SupportedCultures
                .Where(x => x.Equals(culture.ToString(), StringComparison.OrdinalIgnoreCase) || x.Equals(culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Length)
                .FirstOrDefault();

            if (matchingCulture != null)
            {
                return new CultureInfo(matchingCulture);
            }
        }

        return new CultureInfo(settings.SupportedCultures.First());
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
            options.Converters.Add(new StoryblokComponentConverter(IsInEditor));
            options.Converters.Add(new StoryblokDateConverter());
            options.Converters.Add(new StoryblokNullableDateConverter());
            options.Converters.Add(new StoryblokIntConverter());
            options.Converters.Add(new StoryblokNullableIntConverter());
            options.Converters.Add(new StoryblokStringConverter());
            options.Converters.Add(new StoryblokNullableStringConverter());
            options.Converters.Add(new StoryblokMarkdownConverter());
            options.Converters.Add(new StoryblokAssetConverter());
            return options;
        }
    }
}
