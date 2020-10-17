using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Middleware
{
    public class StoryblokSitemapMiddlewareTest
    {
        private readonly MockedWebApplicationFactory<MockedStartup> _factory;

        public StoryblokSitemapMiddlewareTest()
        {
            _factory = new MockedWebApplicationFactory<MockedStartup>();
        }

        [Theory]
        [InlineData("/sitemap.xml")]
        [InlineData("/SITEMAP.XML")]
        [InlineData("/sIteMaP.xMl")]
        public async Task Responds_With_Sitemap(string url)
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("<?xml", responseString);
            Assert.Equal("application/xml", response.Content.Headers.ContentType.MediaType);
            Assert.Contains("public", response.Headers.GetValues("cache-control").First());
        }

        [Theory]
        [InlineData("/sitemap.xml")]
        [InlineData("/SITEMAP.XML")]
        [InlineData("/sIteMaP.xMl")]
        public async Task Does_Not_Respond_With_Sitemap_If_Disabled(string url)
        {
            var client = _factory.CreateClient();
            var options = _factory.Services.GetRequiredService<IOptions<StoryblokOptions>>();
            options.Value.EnableSitemap = false;

            await AssertNotFound(await client.GetAsync(url));
        }

        [Theory]
        [InlineData("/seite")]
        [InlineData("/verzeichnis/sitemap.xml")]
        [InlineData("/sitemap/seite")]
        public async Task Does_Not_Respond_With_Sitemap(string url)
        {
            var client = _factory.CreateClient();
            await AssertNotFound(await client.GetAsync(url));
        }

        private async Task AssertNotFound(HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain("<?xml", responseString);
        }
    }
}