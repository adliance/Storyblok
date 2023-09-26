using System.Text;
using Adliance.Storyblok.Clients;

namespace Adliance.Storyblok.FulltextSearch.Tests;

public class FulltextSearch : FulltextSearchBase
{
    public FulltextSearch(StoryblokStoriesClient storiesClient, StoryblokStoryClient storyClient, LuceneService luceneService) : base(storiesClient, storyClient, luceneService)
    {
    }
    
    protected override void HandleComponent(StoryblokComponent? component, StringBuilder content)
    {
        if (component == null) return;
        if (component is PageComponent page)
        {
            HandleComponent(page.Content, content);
        }
        else if (component is SectionComponent section)
        {
            HandleComponent(section.Content, content);
        }
        else if (component is Grid1x1Component grid1x1)
        {
            HandleComponent(grid1x1.Left, content);
            HandleComponent(grid1x1.Right, content);
        }
        else if (component is TableComponent table)
        {
            if (table.Table != null)
            {
                foreach (var cell in table.Table.Header) AddText(content, cell.ValueAsMarkdown);
                foreach (var row in table.Table.Body)
                {
                    foreach (var cell in row.Columns) AddText(content, cell.ValueAsMarkdown);
                }
            }
        }
    }
}