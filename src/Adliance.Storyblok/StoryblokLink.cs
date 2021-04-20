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
        [JsonPropertyName("email")] public string? Email { get; set; }
        
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

                url = LinkType switch
                {
                    "story" => Story?.FullSlug,
                    "url" => CachedValue ?? Value,
                    "email" => "mailto:" + Email,
                    _ => "/"
                };

                if (url != null && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
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