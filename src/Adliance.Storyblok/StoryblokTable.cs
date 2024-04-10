using System;
using System.Text.Json.Serialization;

namespace Adliance.Storyblok;

public class StoryblokTable
{
    [JsonPropertyName("thead")] public StoryblokTableCell[] Header { get; set; } = [];
    [JsonPropertyName("tbody")] public StoryblokTableRow[] Body { get; set; } = [];
}

public class StoryblokTableRow
{
    [JsonPropertyName("body")] public StoryblokTableCell[] Columns { get; set; } = [];
}

public class StoryblokTableCell
{
    [JsonPropertyName("value")] public string? Value { get; set; }

    [JsonIgnore] public Markdown ValueAsMarkdown => new Markdown(Value);
}
