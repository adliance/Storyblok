using System;
using System.Collections.Generic;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Adliance.Storyblok;

public class StoryblokOptions
{
    public string? ApiKeyPreview { get; set; }
    public string? ApiKeyPublic { get; set; }
    public string? AssetKey { get; set; }
    public bool IncludeDraftStories { get; set; }
    public string BaseUrl { get; set; } = "https://api.storyblok.com/v1/cdn";

    /// <summary>
    /// The duration (in seconds) that all loaded stories will be cached locally.
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 60 * 15;

    /// <summary>
    /// If this value is not empty, than a call to the root ~/ will be handled with the specified slug.
    /// </summary>
    public string HandleRootWithSlug { get; set; } = "/home";

    /// <summary>
    /// All story slugs defined here will not be automatically mapped in the middleware.
    /// Use this setting if you have a controller action with the same name as a story, but don't want to story rendered automatically but the controller action.
    /// </summary>
    public IList<string> IgnoreSlugs { get; set; } = new List<string>();

    /// <summary>
    /// The supported cultures, as specified in Storyblok.
    /// The first culture is also the default culture.
    /// </summary>
    public string[] SupportedCultures { get; set; } = [];

    /// <summary>
    /// This is the slug that will be loaded from Storyblok as part of the health check middleware.
    /// </summary>
    public string SlugForHealthCheck { get; set; } = "home";

    /// <summary>
    /// If set, the middleware will clear all caches when this slug is being called.
    /// This is useful for using it as the Storyblok webhook callback on content changes (see https://www.storyblok.com/docs/concepts/webhooks).
    /// </summary>
    public string SlugForClearingCache { get; set; } = "/clear-storyblok-cache";

    /// <summary>
    /// IP addresses (e.g. "203.0.113.10") or networks in CIDR notation (e.g. "203.0.113.0/24") that are allowed to call <see cref="SlugForClearingCache"/>.
    /// Requests from other IP addresses are answered with 403 Forbidden. If empty (default), requests from all IP addresses are allowed.
    /// If the application runs behind a reverse proxy, configure the forwarded headers middleware so that the actual client IP address is used.
    /// </summary>
    public IList<string> ClearCacheAllowedIpAddresses { get; set; } = new List<string>();

    /// <summary>
    /// If enabled, the list of all published stories (the same list that is used for the sitemap) is used to answer requests for non-existing slugs
    /// without calling the Storyblok API. This protects the Storyblok API (and its rate limits) from bots and crawlers requesting random URLs.
    /// The index is never used in the Storyblok editor or if <see cref="IncludeDraftStories"/> is enabled.
    /// Configure a Storyblok webhook to <see cref="SlugForClearingCache"/> for publishing, unpublishing, deleting and moving stories
    /// (see https://www.storyblok.com/docs/concepts/webhooks), otherwise newly published stories will not be found until the cache expires.
    /// </summary>
    public bool UseStoryIndexForNotFound { get; set; }

    public bool EnableSitemap { get; set; } = true;

    public bool ResolveAssets { get; set; }
    public ResolveLinksType ResolveLinks { get; set; } = ResolveLinksType.Url;
    public string ResolveRelations { get; set; } = "";

    /// <summary>
    /// The name of the datasource that contains HTTP redirect information. Leave empty to not use the RedirectsMiddleware at all.
    /// </summary>
    public string? RedirectsDatasourceName { get; set; }

    public Func<StoryblokStory, bool> SitemapFilter { get; set; } = _ => true;
}
