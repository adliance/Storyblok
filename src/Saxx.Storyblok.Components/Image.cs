using Newtonsoft.Json;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Components
{
    [StoryblokComponent("image")]
    public class Image : StoryblokComponent
    {
        [JsonProperty("image")] public string ImageUrl { get; set; }
        [JsonProperty("class")] public string Class { get; set; }
        [JsonProperty("alt")] public string Alt { get; set; }
        [JsonProperty("title")] public string Title { get; set; }
    }
}