using System.Text.Json.Serialization;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Components
{
    [StoryblokComponent("image")]
    public class Image : StoryblokComponent
    {
        [JsonPropertyName("image")] public string ImageUrl { get; set; }
        [JsonPropertyName("class")] public string Class { get; set; }
        [JsonPropertyName("alt")] public string Alt { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
    }
}