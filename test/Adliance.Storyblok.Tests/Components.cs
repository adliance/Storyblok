using System.Text.Json.Serialization;
using Adliance.Storyblok.Attributes;

namespace Adliance.Storyblok.Tests
{
    // contains several very simplified component definitions that correspond with our test space in Storyblok, so that we can test against actual components

    [StoryblokComponent("page")]
    public class PageComponent : StoryblokComponent
    {
       [JsonPropertyName("content")] public StoryblokComponent[]? Content { get; set; }
    }
    
    [StoryblokComponent("section")]
    public class SectionComponent : StoryblokComponent
    {
        [JsonPropertyName("content")] public StoryblokComponent[]? Content { get; set; }
    }
    
    [StoryblokComponent("button")]
    public class ButtonComponent : StoryblokComponent
    {
        [JsonPropertyName("link")] public StoryblokLink? Link { get; set; }
    }
    
    [StoryblokComponent("grid_1x1")]
    public class Grid1x1Component : StoryblokComponent
    {
        [JsonPropertyName("left_column")] public StoryblokComponent[]? Left { get; set; }
        [JsonPropertyName("right_column")] public StoryblokComponent[]? Right { get; set; }
    }
}