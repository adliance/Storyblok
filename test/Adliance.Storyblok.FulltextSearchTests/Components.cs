using System.Text.Json.Serialization;
using Adliance.Storyblok.Attributes;
using Adliance.Storyblok.Converters;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Adliance.Storyblok.FulltextSearch.Tests;

// contains several very simplified component definitions that correspond with our test space in Storyblok, so that we can test against actual components

[StoryblokComponent("page")]
public class PageComponent : StoryblokComponent
{
    [JsonPropertyName("content")] public StoryblokComponent[]? Content { get; set; }
}

[StoryblokComponent("section")]
public class SectionComponent : StoryblokComponent
{
    [JsonPropertyName("content")] public StoryblokComponent[]? Content { get; set; }
}

[StoryblokComponent("button")]
public class ButtonComponent : StoryblokComponent
{
    [JsonPropertyName("link")] public StoryblokLink? Link { get; set; }
}

[StoryblokComponent("table")]
public class TableComponent : StoryblokComponent
{
    [JsonPropertyName("table")] public StoryblokTable? Table { get; set; }
}

[StoryblokComponent("image")]
public class ImageComponent : StoryblokComponent
{
    [JsonPropertyName("asset")] public StoryblokAsset? Asset { get; set; }
}

[StoryblokComponent("component_reference")]
public class ComponentReference : StoryblokComponent
{
    [JsonPropertyName("referenced_component"), JsonConverter(typeof(StoryblokReferencedComponentConverter<ReferencedComponentContainer>))] public StoryblokComponent[]? ReferencedComponent { get; set; }
}

[StoryblokComponent("global_component")]
public class ReferencedComponentContainer : StoryblokReferencedComponentContainer
{
    [JsonPropertyName("contained_component")] public override StoryblokComponent[]? ContainedComponents { get; set; }
}

[StoryblokComponent("grid_1x1")]
public class Grid1x1Component : StoryblokComponent
{
    [JsonPropertyName("left_column")] public StoryblokComponent[]? Left { get; set; }
    [JsonPropertyName("right_column")] public StoryblokComponent[]? Right { get; set; }
}

[StoryblokComponent("dropdown")]
public class DropdownComponent : StoryblokComponent
{
    [JsonPropertyName("singleoption_self")] public string? SingleOptionSelf { get; set; }
    [JsonPropertyName("singleoption_datasource")] public string? SingleOptionDatasource { get; set; }
}
