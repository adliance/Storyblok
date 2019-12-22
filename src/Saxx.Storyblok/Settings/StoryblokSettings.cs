using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Saxx.Storyblok.Settings
{
    public class StoryblokSettings
    {
        public StoryblokSettings()
        {
        }

        public StoryblokSettings(IConfiguration configuration)
        {
            var section = configuration.GetSection("storyblok");

            if (!string.IsNullOrWhiteSpace(section["url"]))
            {
                BaseUrl = section["url"];
            }

            if (!string.IsNullOrWhiteSpace(section["api_key_preview"]))
            {
                ApiKeyPreview = section["api_key_preview"];
            }

            if (!string.IsNullOrWhiteSpace(section["api_key_public"]))
            {
                ApiKeyPublic = section["api_key_public"];
            }
            
            if (!string.IsNullOrWhiteSpace(section["include_draft_stories"]) && bool.TryParse(section["include_draft_stories"], out var b))
            {
                IncludeDraftStories = b;
            }

            if (!string.IsNullOrWhiteSpace(section["cache_duration_seconds"]))
            {
                CacheDurationSeconds = int.Parse(section["cache_duration_seconds"]);
            }

            if (!string.IsNullOrWhiteSpace(section["handle_root_with_slug"]))
            {
                HandleRootWithSlug = section["handle_root_with_slug"];
            }
        }

        public string ApiKeyPreview { get; set; }
        public string ApiKeyPublic { get; set; }
        public bool IncludeDraftStories { get; set; }
        public string BaseUrl { get; set; } = "https://api.storyblok.com/v1/cdn";

        /// <summary>
        /// The duration (in seconds) that all loaded stories will be cached locally.
        /// </summary>
        public int CacheDurationSeconds { get; set; } = 60;
        
        /// <summary>
        /// If this value is not empty, than a call to the root ~/ will be handled with the specified slug.
        /// </summary>
        public string HandleRootWithSlug { get; set; }

        /// <summary>
        /// All story slugs defined here will not be automatically mapped in the middleware.
        /// Use this setting if you have a controller action with the same name as a story, but don't want to story rendered automatically but the controller action.
        /// </summary>
        public IList<string> IgnoreSlugs  { get; set; } = new List<string>();
        
        /// <summary>
        /// The cultures (languages) supported by the Storyblok workspace, for example: "en,de"
        /// The first culture in the list is the default culture.
        /// </summary>
        public IDictionary<CultureInfo, CultureInfo> CultureMappings { get; set; } = new Dictionary<CultureInfo, CultureInfo>();
        public CultureInfo DefaultCulture { get; set; }
    }
}