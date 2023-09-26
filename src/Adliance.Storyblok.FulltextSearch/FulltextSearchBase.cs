using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Lucene.Net.Documents;
using Lucene.Net.Store;

namespace Adliance.Storyblok.FulltextSearch;

public abstract class FulltextSearchBase
{
    private readonly StoryblokStoriesClient _storiesClient;
    private readonly StoryblokStoryClient _storyClient;
    private readonly LuceneService _luceneService;

    public FulltextSearchBase(StoryblokStoriesClient storiesClient, StoryblokStoryClient storyClient, LuceneService luceneService)
    {
        _storiesClient = storiesClient;
        _storyClient = storyClient;
        _luceneService = luceneService;
    }

    public void DeleteFulltextIndex()
    {
        var indexPath = LuceneService.GetIndexDirectoryPath();
        if (System.IO.Directory.Exists(indexPath)) System.IO.Directory.Delete(indexPath, true);
    }

    public async Task<int?> UpdateFulltextIndex()
    {
        var stories = await _storiesClient.Stories().ForCurrentUiCulture().ExcludingFields("content").Load();
        var latestStoryDate = stories.Max(x => x.PublishedAt);
        var latestIndexDate = _luceneService.GetUpdatedDate();
        if (latestIndexDate != null && latestStoryDate != null && latestIndexDate >= latestStoryDate) return null;

        var documents = new List<Document>();
        foreach (var s in stories)
        {
            if (s.FullSlug == null) continue;
            var document = await HandleStory(s.FullSlug);
            if (document != null) documents.Add(document);
        }

        _luceneService.CreateIndex(documents);
        return documents.Count;
    }

    public SearchResult Query(string queryText, int numberOfResults)
    {
        return _luceneService.Query(queryText, numberOfResults);
    }

    protected virtual async Task<Document?> HandleStory(string slug)
    {
        var story = await _storyClient.Story().WithSlug(slug).Load();
        if (story == null) return null;

        var sb = new StringBuilder();
        HandleComponent(story.Content, sb);
        return CreateDocument(story.FullSlug!, "", sb.ToString());
    }

    protected abstract void HandleComponent(StoryblokComponent? component, StringBuilder content);

    protected void HandleComponent(IEnumerable<StoryblokComponent?>? components, StringBuilder content)
    {
        if (components == null) return;
        foreach (var c in components) HandleComponent(c, content);
    }

    private Document CreateDocument(string fullSlug, string title, string content)
    {
        return _luceneService.CreateDocument(fullSlug, title, content);
    }

    protected static void AddText(StringBuilder sb, string? s)
    {
        if (!string.IsNullOrWhiteSpace(s)) sb.AppendLine(s);
    }

    protected static void AddText(StringBuilder sb, Markdown? s)
    {
        AddText(sb, RemoveMarkdown(s?.Value));
    }

    protected static string? RemoveMarkdown(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        s = Regex.Replace(s, @"^\#{1,6}\W*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        s = Regex.Replace(s, @"\!?\[(.*)\]\W*\(.*\)", "$1", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<.*>", "", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\*\*", "", RegexOptions.IgnoreCase);
        return s;
    }
}