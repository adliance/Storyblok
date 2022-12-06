using System.Text.Json.Serialization;
using Adliance.Storyblok.Extensions;

namespace Adliance.Storyblok
{
    public class StoryblokAsset : IImageService
    {
        [JsonPropertyName("filename")] public string? Url { get; set; }
        [JsonPropertyName("fieldtype")] public string? FieldType { get; set; }
        [JsonPropertyName("alt")] public string? Alt { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("copyright")] public string? Copyright { get; set; }

        [JsonPropertyName("original")] public StoryblokAssetOriginal? Original { get; set; }
    }

    public class StoryblokAssetOriginal : IImageService
    {
        [JsonPropertyName("filename")] public string? Url { get; set; }
        [JsonPropertyName("fieldtype")] public string? FieldType { get; set; }
        [JsonPropertyName("alt")] public string? Alt { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("copyright")] public string? Copyright { get; set; }
        [JsonPropertyName("content_length")] public int? ContentLength { get; set; }
        [JsonPropertyName("content_type")] public string? ContentType { get; set; }
    }
}