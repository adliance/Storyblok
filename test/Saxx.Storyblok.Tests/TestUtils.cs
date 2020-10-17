using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Saxx.Storyblok.Clients;

namespace Saxx.Storyblok.Tests
{
    public class TestUtils
    {
        public static StoryblokDatasourceClient GetDatasourceClient()
        {
            var options = new StoryblokOptions
            {
                // set view environment variable, useful in unit tests on build server, where the key comes from build secrets
                ApiKeyPublic = Environment.GetEnvironmentVariable("Adliance_Storyblok_Tests__ApiKeyPublic"),
                SupportedCultures = new[] {"de", "en"}
            };

            return new StoryblokDatasourceClient(
                Options.Create(options),
                new MockedHttpClientFactory(),
                null,
                new MemoryCache(Options.Create(new MemoryCacheOptions())),
                NullLogger<StoryblokBaseClient>.Instance);
        }
    }
}