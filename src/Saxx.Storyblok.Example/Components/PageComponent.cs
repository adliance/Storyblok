using System.Text.Json.Serialization;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Example.Components
{
    [StoryblokComponent("page", "Page")]
    public class PageComponent : StoryblokComponent {
        
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; }
        [JsonPropertyName("keywords")] public string Keywords { get; set; }
        [JsonPropertyName("body")] public StoryblokComponent[] Body { get; set; }
        
        [JsonPropertyName("menu_title")] public string MenuTitle { get; set; }
        [JsonPropertyName("menu_order")] public int MenuOrder { get; set; }
    }
}