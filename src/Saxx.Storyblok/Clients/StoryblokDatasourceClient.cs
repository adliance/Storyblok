using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Saxx.Storyblok.Clients
{
    public class StoryblokDatasourceClient : StoryblokBaseClient
    {
        public StoryblokDatasourceClient(
            IOptions<StoryblokOptions> settings,
            IHttpClientFactory clientFactory,
            IHttpContextAccessor httpContext,
            IMemoryCache memoryCache,
            ILogger<StoryblokBaseClient> logger) : base(settings, clientFactory, httpContext, memoryCache, logger)
        {
        }


        public async Task<StoryblokDatasource?> Datasource(string name, string? dimension)
        {
            var datasource = await Datasource(name);
            if (datasource == null)
            {
                return null;
            }

            datasource.Entries = datasource.Entries.Where(x => (x.Dimension == null && dimension == null) || (dimension != null && dimension.Equals(x.Dimension, StringComparison.OrdinalIgnoreCase))).ToList();
            return datasource;
        }

        public async Task<StoryblokDatasource?> Datasource(string name, CultureInfo dimension)
        {
            return await Datasource(name, IsDefaultCulture(dimension) ? null : dimension.ToString());
        }

        public async Task<StoryblokDatasource?> Datasource(string name)
        {
            if (IsInEditor)
            {
                return await LoadDatasourceFromStoryblok(name);
            }

            var cacheKey = $"datasource_{name}";
            if (MemoryCache.TryGetValue(cacheKey, out StoryblokDatasource cachedDatasource))
            {
                Logger.LogTrace($"Using cached datasource \"{name}\".");
                return cachedDatasource;
            }

            var cacheKeyUnavailable = "404_" + cacheKey;
            if (MemoryCache.TryGetValue(cacheKeyUnavailable, out _))
            {
                Logger.LogTrace($"Using cached 404 for datasource \"{name}\".");
                return null;
            }

            Logger.LogTrace($"Trying to load datasource \"{name}\".");
            var datasource = await LoadDatasourceFromStoryblok(name);
            if (datasource != null)
            {
                Logger.LogTrace($"Datasource \"{name}\" loaded.");
                MemoryCache.Set(cacheKey, datasource, TimeSpan.FromSeconds(Settings.CacheDurationSeconds));
                return datasource;
            }

            Logger.LogTrace($"Datasource \"{name}\" not found.");
            MemoryCache.Set(cacheKeyUnavailable, true, TimeSpan.FromSeconds(Settings.CacheDurationSeconds));
            return null;
        }


        private async Task<StoryblokDatasource?> LoadDatasourceFromStoryblok(string name)
        {
            var url = $"{Settings.BaseUrl}/datasource_entries?datasource={name}&token={ApiKey}";
            url += $"&cb={DateTime.UtcNow:yyyyMMddHHmmss}";

            var response = await Client.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<StoryblokDatasource>(responseString, JsonOptions);
            return result;
        }
    }
}
