using System.Text.Json.Serialization;

namespace Saxx.Storyblok
{
    public class StoryblokUrl
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("linktype")] public string? LinkType { get; set; }
        [JsonPropertyName("fieldtype")] public string? FieldType { get; set; }
        [JsonPropertyName("cached_url")] public string? CachedUrl { get; set; }
    }
}