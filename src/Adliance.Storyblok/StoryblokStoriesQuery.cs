using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;

// ReSharper disable UnusedMember.Global

namespace Adliance.Storyblok;

public class StoryblokStoriesQuery(StoryblokStoriesClient client, StoryblokOptions options)
{
    private readonly IList<string> _excludingFields = new List<string>();
    private readonly IList<Filter> _filters = new List<Filter>();
    private string _startsWith = "";
    private string _culture = "";

    public StoryblokStoriesQuery StartingWith(string startingWith)
    {
        _startsWith = startingWith;
        return this;
    }

    public StoryblokStoriesQuery ForCurrentUiCulture()
    {
        return ForCulture(CultureInfo.CurrentUICulture);
    }

    public StoryblokStoriesQuery ForCulture(CultureInfo culture)
    {
        _culture = culture.ToString();
        if (options.SupportedCultures.First().Equals(_culture, StringComparison.OrdinalIgnoreCase))
        {
            _culture = "";
        }
        else if (!options.SupportedCultures.Any(x => x.Equals(_culture, StringComparison.OrdinalIgnoreCase)))
        {
            _culture = "";
        }

        return this;
    }

    public StoryblokStoriesQuery ExcludingFields(params string[] fields)
    {
        foreach (var s in fields)
        {
            _excludingFields.Add(s);
        }
        return this;
    }

    public StoryblokStoriesQuery Having(string field, FilterOperation operation, string value)
    {
        if (!string.IsNullOrWhiteSpace(field))
        {
            _filters.Add(new Filter
            {
                Field = field,
                Operation = operation,
                Value = value
            });
        }

        return this;
    }

    public async Task<IList<StoryblokStory<T>>> Load<T>() where T : StoryblokComponent
    {
        return await client.LoadStories<T>(GetParameters());
    }

    // ReSharper disable once UnusedMember.Global
    public async Task<IList<StoryblokStory>> Load()
    {
        return await client.LoadStories(GetParameters());
    }

    private string GetParameters()
    {
        var result = "";

        if (!string.IsNullOrWhiteSpace(_culture) && string.IsNullOrWhiteSpace(_startsWith))
        {
            result += $"&starts_with={_culture}/*";
        }
        else if (!string.IsNullOrWhiteSpace(_culture) && !string.IsNullOrWhiteSpace(_startsWith))
        {
            result += $"&starts_with={_culture}/{_startsWith.TrimStart('/')}";
        }
        else if (!string.IsNullOrWhiteSpace(_startsWith))
        {
            result += $"&starts_with={_startsWith.TrimStart('/')}";
        }

        if (_excludingFields.Any())
        {
            result += $"&excluding_fields={string.Join(", ", _excludingFields)}";
        }

        foreach (var f in _filters)
        {
            result += f.ToString();
        }

        return result;
    }

    private sealed class Filter
    {
        public string? Field { get; set; }
        public FilterOperation Operation { get; set; }
        public string? Value { get; set; }

        public override string ToString()
        {
            string operation;
            switch (Operation)
            {
                case FilterOperation.In:
                    operation = "in";
                    break;
                case FilterOperation.NotIn:
                    operation = "not_in";
                    break;
                case FilterOperation.GreaterThanInt:
                    operation = "gt-int";
                    break;
                case FilterOperation.LessThanInt:
                    operation = "lt-int";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return $"&filter_query[{Field}][{operation}]={Value}";
        }
    }
}

public enum FilterOperation
{
    In,
    NotIn,
    GreaterThanInt,
    LessThanInt
}
