using System;
using System.Text.Json.Serialization;

namespace Saxx.Storyblok
{
    public class StoryblokComponent
    {
        [JsonPropertyName("_uid")] public Guid Uuid { get; set; }
        [JsonPropertyName("component")] public string Component { get; set; } = "";
    }
}