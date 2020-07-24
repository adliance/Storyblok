using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Saxx.Storyblok
{
    public class StoryblokStory<T> : StoryblokStory where T : StoryblokComponent
    {
        public StoryblokStory(StoryblokStory story)
        {
            Name = story.Name;
            Slug = story.Slug;
            FullSlug = story.FullSlug;
            CreatedAt = story.CreatedAt;
            PublishedAt = story.PublishedAt;
            FirstPublishedAt = story.FirstPublishedAt;
            Id = story.Id;
            Uuid = story.Uuid;
            IsStartPage = story.IsStartPage;
            Position = story.Position;
            Language = story.Language;

            var castContent = story.Content as T;
            Content = castContent ?? throw new Exception($"A component of type \"{story.Content?.GetType()}\" cannot be cast to \"{typeof(T)}\"");
        }

        // ReSharper disable once MemberCanBePrivate.Global
        [JsonPropertyName("content")] public new T? Content { get; set; }
    }

    public class StoryblokStory
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("full_slug")] public string? FullSlug { get; set; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("published_at")] public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("first_published_at")] public DateTime? FirstPublishedAt { get; set; }

        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("uuid")] public Guid Uuid { get; set; }
        [JsonPropertyName("content")] public StoryblokComponent Content { get; set; } = new StoryblokComponent();

        [JsonPropertyName("position")] public int Position { get; set; }
        [JsonPropertyName("is_startpage")] public bool IsStartPage { get; set; }

        [JsonPropertyName("lang")] public string Language { get; set; } = "";

        public DateTime LoadedAt { get; set; }

        public override string ToString()
        {
            return FullSlug ?? "";
        }
    }

    public class StoryblokStoriesContainer
    {
        [JsonPropertyName("stories")] public IEnumerable<StoryblokStory> Stories { get; set; } = Enumerable.Empty<StoryblokStory>();
    }

    public class StoryblokStoryContainer
    {
        [JsonPropertyName("story")] public StoryblokStory Story { get; set; } = new StoryblokStory();
    }
}