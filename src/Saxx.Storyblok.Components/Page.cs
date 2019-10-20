using Newtonsoft.Json;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Components
{
    [StoryblokComponent("page", "Story")]
    public class Page : StoryblokComponent
    {
        [JsonProperty("title")] public string Title { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("keywords")] public string Keywords { get; set; }
        [JsonProperty("body")] public StoryblokComponent[] Body { get; set; }

        [JsonProperty("menu_title")] public string MenuTitle { get; set; }
        [JsonProperty("menu_order")] public int? MenuOrder { get; set; }
    }
}