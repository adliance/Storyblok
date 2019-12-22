using System;
using System.Text.Json.Serialization;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Example.Components
{
    [StoryblokComponent("blog_post", "BlogPost")]
    public class BlogPostComponent :StoryblokComponent
    {
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("body")] public StoryblokComponent[] Body { get; set; }
        [JsonPropertyName("date")] public DateTime Date { get; set; }
    }
}