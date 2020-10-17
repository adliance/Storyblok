using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients
{
    public class StoryblokDatasourceClientTest
    {
        private readonly MockedWebApplicationFactory<MockedStartup> _factory;

        public StoryblokDatasourceClientTest()
        {
            _factory = new MockedWebApplicationFactory<MockedStartup>();
        }
        
        [Fact]
        public async Task Can_Load_Datasource()
        {
            _factory.CreateClient();
            var client = _factory.Services.GetRequiredService<StoryblokDatasourceClient>();
            
            var datasource = await client.Datasource("redirects");
            Assert.NotNull(datasource);
            Assert.True(datasource.Entries.Count() > 150);
        }

        [Fact]
        public async Task Can_Handle_Paged_Datasources()
        {
            _factory.CreateClient();
            var client = _factory.Services.GetRequiredService<StoryblokDatasourceClient>();
            client.PerPage = 10;

            var datasource = await client.Datasource("redirects");
            Assert.NotNull(datasource);
            Assert.True(datasource.Entries.Count() > 150);
        }
    }
}