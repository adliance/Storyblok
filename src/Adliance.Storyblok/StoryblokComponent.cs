using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Adliance.Storyblok
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class StoryblokComponent
    {
        [JsonPropertyName("_uid")] public Guid Uuid { get; set; }
        [JsonPropertyName("_editable")] public string? Editable { get; set; }
        [JsonPropertyName("component")] public string Component { get; set; } = "";

        public bool IsInEditor { get; set; }
    }
}