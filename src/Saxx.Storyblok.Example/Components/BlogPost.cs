using System;
using System.Text.Json.Serialization;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Example.Components
{
    [StoryblokComponent("blog_post", "BlogPost")]
    public class BlogPost : Saxx.Storyblok.Components.Page
    {
        [JsonPropertyName("date")] public DateTime Date { get; set; }
    }
}