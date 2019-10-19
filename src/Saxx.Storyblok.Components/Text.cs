using Newtonsoft.Json;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Components
{
    [StoryblokComponent("text")]
    public class Text : StoryblokComponent
    {
        [JsonProperty("content")] public string Content { get; set; }
    }
}