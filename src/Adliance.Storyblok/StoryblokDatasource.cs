using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Adliance.Storyblok
{
    public class StoryblokDatasource
    {
        [JsonPropertyName("datasource_entries")] public IEnumerable<StoryblokDatasourceEntry> Entries { get; set; } = Enumerable.Empty<StoryblokDatasourceEntry>();
    }

    public class StoryblokDatasourceEntry
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("value")] public string? DefaultDimensionValue { get; set; }
        [JsonPropertyName("dimension_value")] public string? DimensionValue { get; set; }
        public string? Value => DimensionValue ?? DefaultDimensionValue;
    }
}