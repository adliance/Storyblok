using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.FulltextSearch;

public class LuceneService
{
    private readonly ILogger<LuceneService> _logger;
    private const string SlugField = "slug";
    private const string TitleField = "title";
    private const string ContentField = "content";
    private const string UpdatedField = "updated";
    private const string SpecialSlugNameForMetadataDocument = "___metdata";
    private const LuceneVersion LuceneVersion = Lucene.Net.Util.LuceneVersion.LUCENE_48;
    private readonly Analyzer _analyzer;

    public LuceneService(ILogger<LuceneService> logger, IOptions<StoryblokOptions> storyblokOptions)
    {
        _logger = logger;

        // we need to decide on an Analyzer.
        // and we use the language version based on the first (primary) language
        // this is just a very basic heuristic and needs to be changed in the future (different index per culture, maybe?), but should be enough for now
        var supportedCultures = storyblokOptions.Value.SupportedCultures;
        if (supportedCultures.Any() && supportedCultures.First().StartsWith("de", StringComparison.OrdinalIgnoreCase))
        {
            _analyzer = new GermanAnalyzer(LuceneVersion);
        }
        else if (supportedCultures.Any() && supportedCultures.First().StartsWith("es", StringComparison.OrdinalIgnoreCase))
        {
            _analyzer = new SpanishAnalyzer(LuceneVersion);
        }
        else
        {
            _analyzer = new EnglishAnalyzer(LuceneVersion);
        }
    }

    internal static string GetIndexDirectoryPath()
    {
        var indexName = "___storyblok_lucene_index";
        var indexPath = Path.Combine(Path.GetTempPath(), indexName);
        return indexPath;
    }
    
    private static FSDirectory OpenIndexDirectory()
    {
        var indexPath = GetIndexDirectoryPath();
        if (!System.IO.Directory.Exists(indexPath)) System.IO.Directory.CreateDirectory(indexPath);
        return FSDirectory.Open(indexPath);
    }

    public Document CreateDocument(string slug, string title, string content)
    {
        var document = new Document();
        document.AddStringField(SlugField, slug, Field.Store.YES);
        document.AddTextField(TitleField, title, Field.Store.YES);
        document.AddTextField(ContentField, content, Field.Store.YES);
        document.AddTextField(UpdatedField, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), Field.Store.YES);
        return document;
    }

    public void CreateIndex(IList<Document> documents)
    {
        using var indexDirectory = OpenIndexDirectory();

        var indexConfig = new IndexWriterConfig(LuceneVersion, _analyzer)
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

    public DateTime? GetUpdatedDate()
    {
        try
        {
            using var indexDirectory = OpenIndexDirectory();

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

    public SearchResult Query(string queryText, int numberOfResults)
    {
        using var indexDirectory = OpenIndexDirectory();

        using var reader = DirectoryReader.Open(indexDirectory);
        var searcher = new IndexSearcher(reader);

        var queryParser = new MultiFieldQueryParser(LuceneVersion, new[]
        {
            TitleField,
            ContentField
        }, _analyzer);

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
                var title = doc.Get(TitleField, CultureInfo.CurrentCulture) ?? "";
                if (string.IsNullOrWhiteSpace(title)) title = slug;
                var content = doc.Get(ContentField, CultureInfo.CurrentCulture) ?? "";

                var bestFragments = highlighter.GetBestFragments(_analyzer, ContentField, content, 1);

                result.Results.Add(new SearchResultItem
                {
                    Slug = slug,
                    Title = title,
                    Preview = string.Join(" ", bestFragments)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to query.");
        }

        return result;
    }
}
