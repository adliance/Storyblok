using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Lucene.Net.Documents;
using Microsoft.Extensions.Logging;

namespace Adliance.Storyblok.FulltextSearch;

public abstract class FulltextSearchBase(ILogger logger, StoryblokStoriesClient storiesClient, StoryblokStoryClient storyClient, LuceneService luceneService)
{
    public void DeleteFulltextIndex(string culture)
    {
        var indexPath = LuceneService.GetIndexDirectoryPath(culture);
        if (System.IO.Directory.Exists(indexPath)) System.IO.Directory.Delete(indexPath, true);
    }

    public async Task<int?> UpdateFulltextIndex(string culture)
    {
        logger.LogTrace($"Loading all stories for fulltext index (culture: {culture}) ...");
        var stories = await storiesClient.Stories().ForCulture(new CultureInfo(culture)).ExcludingFields("content").Load();
        var latestStoryDate = stories.Max(x => x.PublishedAt);
        var latestIndexDate = luceneService.GetUpdatedDate(culture);
        if (latestIndexDate != null && latestStoryDate != null && latestIndexDate >= latestStoryDate)
        {
            logger.LogTrace($"Fulltext index is up-to-date, as latest story date is {latestStoryDate} and latest index date is {latestIndexDate}.");
            return null;
        }

        logger.LogInformation($"{stories.Count} stories loaded, latest story date is {latestStoryDate}, latest index date is {latestIndexDate}.");

        var documents = new List<Document>();
        foreach (var s in stories)
        {
            if (s.FullSlug == null)
            {
                logger.LogTrace($"Story {s.Slug} does not have a full slug.");
                continue;
            }

            var document = await HandleStory(s.FullSlug); // we don't need to send the culture here, because it's included in FullSlug
            if (document != null)
            {
                logger.LogTrace($"Adding story {s.FullSlug} to fulltext index ...");
                documents.Add(document);
            }
        }

        luceneService.CreateIndex(culture, documents);
        return documents.Count;
    }

    public SearchResult Query(string culture, string queryText, string[] userRoles, int numberOfResults)
    {
        return luceneService.Query(culture, queryText, userRoles, numberOfResults);
    }

    public SearchResult Query(string culture, string queryText, int numberOfResults)
    {
        return luceneService.Query(culture, queryText, [], numberOfResults);
    }

    public SearchResult Query(string queryText, int numberOfResults)
    {
        return Query(CultureInfo.CurrentUICulture.ToString(), queryText, numberOfResults);
    }

    protected virtual async Task<Document?> HandleStory(string fullSlug)
    {
        logger.LogTrace($"Handling story {fullSlug} ...");

        // please note that we don't need to call .WithCulture() here because it's already included in FullSlug
        var story = await storyClient.Story().WithSlug(fullSlug).Load();
        if (story?.FullSlug == null) return null;

        var title = GetTitle(story);
        var roles = GetRoles(story);
        var content = GetContent(story);
        return luceneService.CreateDocument(story.FullSlug, title, roles, content);
    }

    protected abstract string GetTitle(StoryblokStory story);
    protected abstract string GetContent(StoryblokStory story);
    protected abstract string[] GetRoles(StoryblokStory story);

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
