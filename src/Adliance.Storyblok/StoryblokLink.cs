using System;
using System.Text.Json.Serialization;

namespace Adliance.Storyblok
{
    public class StoryblokLink
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("url")] public string? Value { get; set; }
        [JsonPropertyName("linktype")] public string? LinkType { get; set; }
        [JsonPropertyName("fieldtype")] public string? FieldType { get; set; }
        [JsonPropertyName("cached_url")] public string? CachedValue { get; set; }
        [JsonPropertyName("anchor")] public string? Anchor { get; set; }
        
        /// <summary>
        /// This property is available when the story has been requested via resolve_links parameters set.
        /// See https://www.storyblok.com/cl/url-resolving for more information.
        /// </summary>
        [JsonPropertyName("story")] public StoryblokStory? Story { get; set; }

        public string? Url
        {
            get
            {
                string? url = null;
                if (Story != null)
                {
                    url = Story.FullSlug;
                }

                if (string.IsNullOrWhiteSpace(url))
                {
                    url = CachedValue;
                }

                if (string.IsNullOrWhiteSpace(url))
                {
                    url = Value;
                }

                if (url != null && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    url = url.TrimStart('/');
                    url = "/" + url;
                }

                if (!string.IsNullOrWhiteSpace(Anchor))
                {
                    url += $"#{Anchor}";
                }

                return url;
            }
        }
    }
}