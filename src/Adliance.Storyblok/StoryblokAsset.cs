using System;
using System.Text.Json.Serialization;
using Adliance.Storyblok.Extensions;

namespace Adliance.Storyblok;

public class StoryblokAsset : IAsset
{
    [JsonPropertyName("filename")] public string? Url { get; set; }
    [JsonPropertyName("fieldtype")] public string? FieldType { get; set; }
    [JsonPropertyName("alt")] public string? Alt { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("copyright")] public string? Copyright { get; set; }
    [JsonPropertyName("original")] public StoryblokAssetOriginal? Original { get; set; }
}

public class StoryblokAssetOriginal : IAsset
{
    [JsonPropertyName("filename")] public string? Url { get; set; }
    [JsonPropertyName("fieldtype")] public string? FieldType { get; set; }
    [JsonPropertyName("alt")] public string? Alt { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("copyright")] public string? Copyright { get; set; }
    [JsonPropertyName("content_length")] public int? ContentLength { get; set; }
    [JsonPropertyName("content_type")] public string? ContentType { get; set; }
    [JsonPropertyName("asset_folder_id")] public string? AssetFolderId { get; set; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public DateTime? UpdatedAt { get; set; }
    [JsonPropertyName("expire_at")] public DateTime? ExpireAt { get; set; }
    [JsonPropertyName("is_private")] public bool? IsPrivate { get; set; }
    [JsonPropertyName("signed_url")] public string? SignedUrl { get; set; }
}

public class StoryblokAssetContainer
{
    [JsonPropertyName("asset")] public StoryblokAssetOriginal? Asset { get; set; }
}