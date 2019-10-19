using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Saxx.Storyblok
{
    public class StoryblokStory
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("slug")] public string Slug { get; set; }
        [JsonProperty("full_slug")] public string FullSlug { get; set; }
        [JsonProperty("created_at")] public DateTime CreatedAt { get; set; }
        [JsonProperty("published_at")] public DateTime? PublishedAt { get; set; }
        [JsonProperty("first_published_at")] public DateTime? FirstPublishedAt { get; set; }
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("uuid")] public Guid Uuid { get; set; }
        [JsonProperty("content")] public StoryblokComponent Content { get; set; }

        public DateTime LoadedAt { get; set; }

        public override string ToString()
        {
            return FullSlug;
        }
    }

    public class StoryblokStoriesContainer
    {
        [JsonProperty("stories")] public IEnumerable<StoryblokStory> Stories { get; set; }
    }
    public class StoryblokStoryContainer
    {
        [JsonProperty("story")] public StoryblokStory Story { get; set; }
    }
}
