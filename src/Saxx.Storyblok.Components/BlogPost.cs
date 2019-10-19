using System;
using Newtonsoft.Json;
using Saxx.Storyblok.Attributes;

namespace Saxx.Storyblok.Components
{
    [StoryblokComponent("blog_post")]
    public class BlogPost : Page
    {
        [JsonProperty("date")] public DateTime Date { get; set; }
        [JsonProperty("summary")] public string Summary { get; set; }
        [JsonProperty("author_name")] public string AuthorName { get; set; }
        [JsonProperty("author_image")] public string AuthorImageUrl { get; set; }
    }
}