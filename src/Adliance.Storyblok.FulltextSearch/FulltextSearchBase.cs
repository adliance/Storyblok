using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Lucene.Net.Documents;

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
        if (story?.FullSlug == null) return null;
        
        var title = GetTitle(story);
        var content = GetContent(story);
        return _luceneService.CreateDocument(story.FullSlug, title, content);
    }

    protected abstract string GetTitle(StoryblokStory story);
    protected abstract string GetContent(StoryblokStory story);
    
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