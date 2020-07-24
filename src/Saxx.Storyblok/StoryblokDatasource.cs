using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Saxx.Storyblok
{
    public class StoryblokDatasource
    {
        [JsonPropertyName("datasource_entries")] public IEnumerable<StoryblokDatasourceEntry> Entries { get; set; } = Enumerable.Empty<StoryblokDatasourceEntry>();
    }

    public class StoryblokDatasourceEntry
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("dimension_value")] public string? Dimension { get; set; }
    }
}