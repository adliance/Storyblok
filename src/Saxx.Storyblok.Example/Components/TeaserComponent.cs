using System.Text.Json.Serialization;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Example.Components
{
    [StoryblokComponent("teaser")]
    public class TeaserComponent : StoryblokComponent
    {
        [JsonPropertyName("headline")] public string Headline { get; set; }
    }
}