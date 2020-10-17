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
            Assert.NotEmpty(datasource.Entries);
        }
    }
}