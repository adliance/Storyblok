using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients
{
    public class StoryblokDatasourceClientTest
    {
        [Fact]
        public async Task Can_Load_Datasource()
        {
            var client = TestUtils.GetDatasourceClient();

            var datasource = await client.Datasource("redirects");
            Assert.NotNull(datasource);
            Assert.True(datasource.Entries.Count() > 150);
        }

        [Fact]
        public async Task Can_Handle_Paged_Datasources()
        {
            var client = TestUtils.GetDatasourceClient();
            client.PerPage = 10;

            var datasource = await client.Datasource("redirects");
            Assert.NotNull(datasource);
            Assert.True(datasource.Entries.Count() > 150);
        }
    }
}