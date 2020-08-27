using System.Text.Json.Serialization;

namespace Saxx.Storyblok
{
    public class StoryblokAsset
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("filename")] public string? Url { get; set; }
        [JsonPropertyName("fieldtype")] public string? FieldType { get; set; }
        [JsonPropertyName("alt")] public string? Alt { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("copyright")] public string? Copyright { get; set; }
    }
}