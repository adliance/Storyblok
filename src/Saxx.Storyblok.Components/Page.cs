using System.Text.Json.Serialization;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Components
{
    [StoryblokComponent("page", "Page")]
    public class Page : StoryblokComponent
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("keywords")] public string Keywords { get; set; }
        [JsonPropertyName("body")] public StoryblokComponent[] Body { get; set; }
    }
}