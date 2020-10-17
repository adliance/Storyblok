using System.Threading.Tasks;
using Adliance.Storyblok.Sitemap;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.Tests.Sitemap
{
    public class SitemapBuilderTest
    {
        private readonly MockedWebApplicationFactory<MockedStartup> _factory;

        public SitemapBuilderTest()
        {
            _factory = new MockedWebApplicationFactory<MockedStartup>();
        }

        [Fact]
        public async Task Can_Build_Sitemap()
        {
            _factory.CreateClient();
            var builder = _factory.Services.GetRequiredService<SitemapBuilder>();

            var sitemap = await builder.Build();
            Assert.NotNull(sitemap);
            Assert.True(sitemap.Locations.Count > 125);
        }

        [Fact]
        public async Task Can_Build_Sitemap_Xml()
        {
            _factory.CreateClient();
            var builder = _factory.Services.GetRequiredService<SitemapBuilder>();

            var sitemapXml = await builder.BuildXml();
            Assert.NotNull(sitemapXml);
            Assert.True(sitemapXml.Length > 125 * 150);
        }
    }
}