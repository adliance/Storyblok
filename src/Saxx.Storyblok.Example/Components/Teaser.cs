using Newtonsoft.Json;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Example.Components
{
    [StoryblokComponent("teaser")]
    public class Teaser : StoryblokComponent
    {
        [JsonProperty("headline")] public string Headline { get; set; }
    }
}