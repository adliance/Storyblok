using System;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Tests
{
    public class TestUtils
    {
        public static StoryblokDatasourceClient GetDatasourceClient()
        {
            return new StoryblokDatasourceClient(
                GetOptions(),
                new MockedHttpClientFactory(),
                null,
                new MemoryCache(Options.Create(new MemoryCacheOptions())),
                NullLogger<StoryblokBaseClient>.Instance);
        }

        public static StoryblokStoriesClient GetStoriesClient()
        {
            return new StoryblokStoriesClient(
                GetOptions(),
                new MockedHttpClientFactory(),
                null,
                new MemoryCache(Options.Create(new MemoryCacheOptions())),
                NullLogger<StoryblokBaseClient>.Instance);
        }

        private static IOptions<StoryblokOptions> GetOptions()
        {
            var options = new StoryblokOptions
            {
                // set view environment variable, useful in unit tests on build server, where the key comes from build secrets
                ApiKeyPublic = Environment.GetEnvironmentVariable("Adliance_Storyblok_Tests__ApiKeyPublic"),
                SupportedCultures = new[] {"de", "en"}
            };
            return Options.Create(options);
        }
    }
}