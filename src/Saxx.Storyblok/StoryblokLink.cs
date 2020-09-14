using System;
using System.Text.Json.Serialization;

namespace Saxx.Storyblok
{
    public class StoryblokLink
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("url")] public string? Value { get; set; }
        [JsonPropertyName("linktype")] public string? LinkType { get; set; }
        [JsonPropertyName("fieldtype")] public string? FieldType { get; set; }
        [JsonPropertyName("cached_url")] public string? CachedValue { get; set; }

        public string? Url
        {
            get
            {
                var url = string.IsNullOrWhiteSpace(CachedValue) ? Value : CachedValue;
                if (url != null && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    url = url.TrimStart('/');
                    url = "/" + url;
                }

                return url;
            }
        }
    }
}