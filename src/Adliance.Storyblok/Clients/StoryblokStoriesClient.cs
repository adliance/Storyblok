using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Adliance.Storyblok.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Clients
{
    public class StoryblokStoriesClient : StoryblokBaseClient
    {
        public StoryblokStoriesClient(
            IOptions<StoryblokOptions> settings,
            IHttpClientFactory clientFactory,
            IHttpContextAccessor? httpContext,
            IMemoryCache memoryCache,
            ILogger<StoryblokBaseClient> logger) : base(settings, clientFactory, httpContext, memoryCache, logger)
        {
        }

        /// <summary>
        /// Gets or sets the number of items requested for each page when loading the stories.
        /// </summary>
        public int PerPage { get; set; } = 100;

        // ReSharper disable once UnusedMember.Global
        public StoryblokStoriesQuery Stories()
        {
            return new StoryblokStoriesQuery(this, Settings);
        }


        internal async Task<IList<StoryblokStory<T>>> LoadStories<T>(string parameters) where T : StoryblokComponent
        {
            // if we only want stories of a specific type, we should add the corresponding component filter to only load the required components from Storyblok
            var attribute = typeof(T).GetCustomAttribute<StoryblokComponentAttribute>();
            if (attribute != null)
            {
                parameters += $"&filter_query[component][in]={attribute.Name}";
            }

            var stories = await LoadStories(parameters);
            return stories.Select(x => new StoryblokStory<T>(x)).ToList();
        }

        internal async Task<IList<StoryblokStory>> LoadStories(string parameters)
        {
            var cacheKey = $"stories_{parameters}";
            if (MemoryCache.TryGetValue(cacheKey, out IList<StoryblokStory>? cachedStories) && cachedStories != null)
            {
                Logger.LogTrace($"Using cached stories for \"{parameters}\".");
                return cachedStories;
            }

            var url = $"{Settings.BaseUrl}/stories";
            url += $"?token={ApiKey}&{parameters.Trim('&')}&page=1&per_page={PerPage}";
            if (Settings.IncludeDraftStories || IsInEditor)
            {
                url += "&version=draft";
            }

            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            Logger.LogTrace($"Trying to load stories for \"{parameters}\".");

            var page = 1;
            var maxPage = 1;
            var result = new List<StoryblokStory>();

            while (page <= maxPage)
            {
                var urlWithPage = url + $"&page={page}";

                var response = await Client.GetAsync(urlWithPage);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var stories = JsonSerializer.Deserialize<StoryblokStoriesContainer>(responseString, JsonOptions);

                if (stories == null)
                {
                    throw new Exception($"Unable to deserialize {responseString}.");
                }

                var currentPageStories = stories.Stories.ToList();
                currentPageStories.ForEach(x => x.LoadedAt = DateTime.UtcNow);
                if (!currentPageStories.Any())
                {
                    // bail out if we didn't get any stories, maybe we did something wrong in page calculation
                    break;
                }

                result.AddRange(currentPageStories);

                var total = int.Parse(response.Headers.GetValues("Total").First());
                maxPage = (int)Math.Ceiling(total / (double)PerPage);
                page++;
            }

            Logger.LogTrace($"Stories loaded for \"{parameters}\".");
            MemoryCache.Set(cacheKey, result, TimeSpan.FromSeconds(Settings.CacheDurationSeconds));
            return result;
        }
    }
}