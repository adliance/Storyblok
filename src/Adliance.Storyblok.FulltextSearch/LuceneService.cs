using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.De;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Es;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;

namespace Adliance.Storyblok.FulltextSearch;

public class LuceneService(ILogger<LuceneService> logger)
{
    private const string SlugField = "slug";
    private const string RolesField = "roles";
    private const string TitleField = "title";
    private const string ContentField = "content";
    private const string UpdatedField = "updated";
    private const string SpecialSlugNameForMetadataDocument = "___metdata";
    private const LuceneVersion LuceneVersion = Lucene.Net.Util.LuceneVersion.LUCENE_48;

    private static Analyzer GetAnalyzer(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) return new EnglishAnalyzer(LuceneVersion);
        if (culture.StartsWith("de", StringComparison.OrdinalIgnoreCase)) return new GermanAnalyzer(LuceneVersion);
        if (culture.StartsWith("es", StringComparison.OrdinalIgnoreCase)) return new SpanishAnalyzer(LuceneVersion);
        return new EnglishAnalyzer(LuceneVersion);
    }

    internal static string GetIndexDirectoryPath(string culture)
    {
        var indexName = $"___storyblok_lucene_index_{culture.ToLowerInvariant()}";
        var indexPath = Path.Combine(Path.GetTempPath(), indexName);
        return indexPath;
    }

    private static FSDirectory OpenIndexDirectory(string culture)
    {
        var indexPath = GetIndexDirectoryPath(culture);
        if (!System.IO.Directory.Exists(indexPath)) System.IO.Directory.CreateDirectory(indexPath);
        return FSDirectory.Open(indexPath);
    }

    public Document CreateDocument(string slug, string title, string[] roles, string content)
    {
        var document = new Document();
        document.AddStringField(SlugField, slug, Field.Store.YES);
        document.AddStringField(RolesField, JsonSerializer.Serialize(roles), Field.Store.YES);
        document.AddTextField(TitleField, title, Field.Store.YES);
        document.AddTextField(ContentField, content, Field.Store.YES);
        document.AddTextField(UpdatedField, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), Field.Store.YES);
        return document;
    }

    public void CreateIndex(string culture, IList<Document> documents)
    {
        using var indexDirectory = OpenIndexDirectory(culture);
        using var analyzer = GetAnalyzer(culture);

        var indexConfig = new IndexWriterConfig(LuceneVersion, analyzer)
        {
            OpenMode = OpenMode.CREATE
        };
        using var writer = new IndexWriter(indexDirectory, indexConfig);
        writer.DeleteAll();

        foreach (var d in documents) writer.AddDocument(d);

        var metadataDocument = new Document();
        metadataDocument.AddStringField(SlugField, SpecialSlugNameForMetadataDocument, Field.Store.YES);
        metadataDocument.AddTextField(UpdatedField, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), Field.Store.YES);
        writer.AddDocument(metadataDocument);

        writer.Commit();
    }

    public DateTime? GetUpdatedDate(string culture)
    {
        try
        {
            using var indexDirectory = OpenIndexDirectory(culture);

            using var reader = DirectoryReader.Open(indexDirectory);
            var searcher = new IndexSearcher(reader);
            var query = new TermQuery(new Term(SlugField, SpecialSlugNameForMetadataDocument));
            var hits = searcher.Search(query, null, n: 1);
            if (hits is { TotalHits: > 0 })
            {
                var metadataDocument = searcher.Doc(hits.ScoreDocs.First().Doc);
                if (DateTime.TryParse(metadataDocument.Get(UpdatedField, CultureInfo.CurrentCulture) ?? "", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d)) return d;
            }
        }
        catch
        {
            // do nothing here
        }

        return null;
    }

    public SearchResult Query(string culture, string queryText, string[] userRoles, int numberOfResults)
    {
        using var indexDirectory = OpenIndexDirectory(culture);
        using var analyzer = GetAnalyzer(culture);

        using var reader = DirectoryReader.Open(indexDirectory);
        var searcher = new IndexSearcher(reader);

        var queryParser = new MultiFieldQueryParser(LuceneVersion, [
            TitleField,
            ContentField
        ], analyzer);

        var result = new SearchResult();

        try
        {
            var formatter = new SimpleHTMLFormatter("**", "**");
            var fragmenter = new SimpleFragmenter(300);

            var query = queryParser.Parse(queryText);
            var scorer = new QueryScorer(query);
            var hits = searcher.Search(query, null, n: numberOfResults, Sort.RELEVANCE);
            result.TotalResults = hits.TotalHits;

            var highlighter = new Highlighter(formatter, scorer)
            {
                TextFragmenter = fragmenter
            };

            foreach (var scoreDoc in hits.ScoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);

                var slug = doc.Get(SlugField, CultureInfo.CurrentCulture) ?? "";
                var rolesJson = doc.Get(RolesField, CultureInfo.InvariantCulture);
                var title = doc.Get(TitleField, CultureInfo.CurrentCulture) ?? "";
                if (string.IsNullOrWhiteSpace(title)) title = slug;
                var content = doc.Get(ContentField, CultureInfo.CurrentCulture) ?? "";

                var bestFragments = highlighter.GetBestFragments(analyzer, ContentField, content, 1);
                string[]? documentRoles = null;
                if (!string.IsNullOrWhiteSpace(rolesJson))
                {
                    try
                    {
                        documentRoles = JsonSerializer.Deserialize<string[]>(rolesJson);
                    }
                    catch
                    {
                        // do nothing here
                    }
                }

                // please note that there's still a problem here with this implementation of a role filter
                // because if a user requests X results, and it will be filtered via role, the returned number of results will be lower EVEN if there would be more matches
                // this could be fixed by doing a better search filter via the index itself, or a second query run that requests a corrected number of results
                // but for now I'm okay with the current limited functionality because it's sufficient for my usecases

                var hasAllRequiredRoles = true;
                if (documentRoles != null && documentRoles.Any())
                {
                    foreach (var documentRole in documentRoles)
                    {
                        if (!userRoles.Any(x => x.Equals(documentRole, StringComparison.OrdinalIgnoreCase)))
                        {
                            hasAllRequiredRoles = false;
                            break;
                        }
                    }
                }

                if (hasAllRequiredRoles)
                {
                    result.Results.Add(new SearchResultItem
                    {
                        Slug = slug,
                        Title = title,
                        Roles = documentRoles,
                        Preview = string.Join(" ", bestFragments)
                    });
                }
                else
                {
                    result.TotalResults--;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to query.");
        }

        return result;
    }
}
