using System;
using System.Text;
using Adliance.Storyblok.Clients;

namespace Adliance.Storyblok.FulltextSearch.Tests;

public class FulltextSearch : FulltextSearchBase
{
    public FulltextSearch(StoryblokStoriesClient storiesClient, StoryblokStoryClient storyClient, LuceneService luceneService) : base(storiesClient, storyClient, luceneService)
    {
    }

    protected override string GetTitle(StoryblokStory story)
    {
        return "";
    }

    protected override string GetContent(StoryblokStory story)
    {
        var sb = new StringBuilder();
        HandleComponent(sb, story.Content);
        return sb.ToString();
    }

    protected override string[] GetRoles(StoryblokStory story)
    {
        return Array.Empty<string>();
    }

    private void HandleComponent(StringBuilder sb, params StoryblokComponent?[]? components)
    {
        if (components == null) return;
        foreach (var component in components)
        {
            switch (component)
            {
                case null:
                    return;
                case PageComponent page:
                    HandleComponent(sb, page.Content);
                    break;
                case SectionComponent section:
                    HandleComponent(sb, section.Content);
                    break;
                case Grid1x1Component grid1x1:
                    HandleComponent(sb, grid1x1.Left);
                    HandleComponent(sb, grid1x1.Right);
                    break;
                case TableComponent table:
                {
                    if (table.Table != null)
                    {
                        foreach (var cell in table.Table.Header) sb.AppendLine(cell.ValueAsMarkdown.Value);
                        foreach (var row in table.Table.Body)
                        {
                            foreach (var cell in row.Columns) sb.AppendLine(cell.ValueAsMarkdown.Value);
                        }
                    }

                    break;
                }
            }
        }
    }
}
