using System;
using System.Text.Json.Serialization;

namespace Adliance.Storyblok
{
    public class StoryblokTable
    {
        [JsonPropertyName("_uid")] public Guid Uuid { get; set; }
        [JsonPropertyName("fieldtype")] public string? FieldType { get; set; }
        [JsonPropertyName("thead")] public TableHead[]? TableHeaderColumns { get; set; }
        [JsonPropertyName("tbody")] public TableRow[]? TableBody { get; set; }
        [JsonPropertyName("_editable")] public string? Editable { get; set; }
    }

    public class TableHead
    {
        [JsonPropertyName("_uid")] public Guid Uuid { get; set; }
        [JsonPropertyName("value")] public Markdown? Value { get; set; }
        [JsonPropertyName("_editable")] public string? Editable { get; set; }
    }

    public class TableRow
    {
        [JsonPropertyName("_uid")] public Guid Uuid { get; set; }
        [JsonPropertyName("body")] public TableCol[]? TableColumns { get; set; }
        [JsonPropertyName("_editable")] public string? Editable { get; set; }
    }
    
    public class TableCol
    {
        [JsonPropertyName("_uid")] public Guid Uuid { get; set; }
        [JsonPropertyName("value")] public Markdown? Value { get; set; }
        [JsonPropertyName("_editable")] public string? Editable { get; set; }
    }
}