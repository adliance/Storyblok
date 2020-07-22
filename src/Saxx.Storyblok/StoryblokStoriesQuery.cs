using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global

namespace Saxx.Storyblok
{
    public class StoryblokStoriesQuery
    {
        private readonly StoryblokClient _client;

        private readonly IList<string> _excludingFields = new List<string>();
        private readonly IList<Filter> _filters = new List<Filter>();
        private string _startsWith = "";
        internal const int PerPage = 50;

        public StoryblokStoriesQuery(StoryblokClient client)
        {
            _client = client;
        }

        public StoryblokStoriesQuery StartingWith(string startingWith)
        {
            if (startingWith != null)
            {
                _startsWith = startingWith;
            }

            return this;
        }

        public StoryblokStoriesQuery ExcludingFields(params string[] fields)
        {
            if (fields != null)
            {
                foreach (var s in fields)
                {
                    _excludingFields.Add(s);
                }
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
            return await _client.LoadStories<T>(GetParameters());
        }

        // ReSharper disable once UnusedMember.Global
        public async Task<IList<StoryblokStory>> Load()
        {
            return await _client.LoadStories(GetParameters());
        }

        private string GetParameters()
        {
            var result = $"&per_page={PerPage}";
            
            if (!string.IsNullOrWhiteSpace(_startsWith))
            {
                result += $"&starts_with={(_startsWith ?? "").TrimStart('/')}";
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

        private class Filter
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
}