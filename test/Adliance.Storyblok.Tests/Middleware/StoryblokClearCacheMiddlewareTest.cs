using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Middleware
{
    public class StoryblokClearCacheMiddlewareTest 
    {
        private readonly MockedWebApplicationFactory<MockedStartup> _factory;

        public StoryblokClearCacheMiddlewareTest()
        {
            _factory = new MockedWebApplicationFactory<MockedStartup>();
        }

        [Fact]
        public async Task Responds_With_Cleared_Cache()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/clear-storyblok-cache");
            await AssertCacheCleared(response);
        }
        
        [Fact]
        public async Task Responds_With_Cleared_Cache_For_Configured_Slug()
        {
            var client = _factory.CreateClient();
            var options = _factory.Services.GetRequiredService<IOptions<StoryblokOptions>>();
            options.Value.SlugForClearingCache = "my-own-SLUG";
            
            var response = await client.GetAsync("/my-own-slug");
            await AssertCacheCleared(response);
        }

        [Fact]
        public async Task Does_Not_Respond_With_Cleared_Cache_If_Disabled()
        {
            var client = _factory.CreateClient();
            var options = _factory.Services.GetRequiredService<IOptions<StoryblokOptions>>();
            options.Value.SlugForClearingCache = "";

            await AssertNotFound(await client.GetAsync("/clear-storyblok-cache"));
        }

        [Theory]
        [InlineData("/seite")]
        [InlineData("/verzeichnis/sitemap.xml")]
        [InlineData("/sitemap/seite")]
        public async Task Does_Not_Respond_With_Cleared_Cache(string url)
        {
            var client = _factory.CreateClient();
            await AssertNotFound(await client.GetAsync(url));
        }

        private async Task AssertNotFound(HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.NotEqual("Cache cleared.", responseString);
        }
        
        private async Task AssertCacheCleared(HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Cache cleared.", responseString);
        }
    }
}