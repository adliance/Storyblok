using System;
using System.Text.Json.Serialization;

namespace Adliance.Storyblok
{
    public class StoryblokTable
    {
        [JsonPropertyName("thead")] public StoryblokTableCell[]? Header { get; set; } = Array.Empty<StoryblokTableCell>();
        [JsonPropertyName("tbody")] public StoryblokTableRow[]? Body { get; set; } = Array.Empty<StoryblokTableRow>();
    }

    public class StoryblokTableRow
    {
        [JsonPropertyName("body")] public StoryblokTableCell[] Columns { get; set; } = Array.Empty<StoryblokTableCell>();
    }

    public class StoryblokTableCell
    {
        [JsonPropertyName("value")] public string? Value { get; set; }

        [JsonIgnore] public Markdown ValueAsMarkdown => new Markdown(Value);
    }
}