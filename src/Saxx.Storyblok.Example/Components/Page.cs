using System.Text.Json.Serialization;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Example.Components
{
    public class Page : Saxx.Storyblok.Components.Page
    {
        [JsonPropertyName("menu_title")] public string MenuTitle { get; set; }
        [JsonPropertyName("menu_order")] public int MenuOrder { get; set; }
    }
}