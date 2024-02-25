using System;
using System.Collections.Generic;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Adliance.Storyblok
{
    public class StoryblokOptions
    {
        public string? ApiKeyPreview { get; set; }
        public string? ApiKeyPublic { get; set; }
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
        public string[] SupportedCultures { get; set; } = new string[0];

        /// <summary>
        /// This is the slug that will be loaded from Storyblok as part of the health check middleware.
        /// </summary>
        public string SlugForHealthCheck { get; set; } = "home";

        /// <summary>
        /// If set, the middleware will clear all caches when this slug is being called.
        /// This is useful for using it as the Storyblok webhook callback on content changes.
        /// </summary>
        public string SlugForClearingCache { get; set; } = "/clear-storyblok-cache";

        public bool EnableSitemap { get; set; } = true;

        public bool ResolveAssets { get; set; } = false;
        public ResolveLinksType ResolveLinks { get; set; } = ResolveLinksType.Url;
        public string ResolveRelations { get; set; } = "";
        
        /// <summary>
        /// The name of the datasource that contains HTTP redirect information. Leave empty to not use the RedirectsMiddleware at all.
        /// </summary>
        public string? RedirectsDatasourceName { get; set; }
        
        public Func<StoryblokStory, bool> SitemapFilter { get; set; } = _ => true;
    }
}