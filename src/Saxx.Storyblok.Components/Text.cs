using System.Text.Json.Serialization;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Components
{
    [StoryblokComponent("text")]
    public class Text : StoryblokComponent
    {
        [JsonPropertyName("content")] public string Content { get; set; }
    }
}