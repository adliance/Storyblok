using System.Collections.Generic;

namespace Adliance.Storyblok.FulltextSearch;

public class SearchResultItem
{
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Preview { get; set; }
}

public class SearchResult
{
    public int TotalResults { get; set; }
    public IList<SearchResultItem> Results { get; set; } = new List<SearchResultItem>();
}