using System.Text.Json.Serialization;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Example.Components
{
    [StoryblokComponent("text")]
    public class TextComponent : StoryblokComponent {
        
        [JsonPropertyName("content")] public Markdown Content { get; set; }
    }
}