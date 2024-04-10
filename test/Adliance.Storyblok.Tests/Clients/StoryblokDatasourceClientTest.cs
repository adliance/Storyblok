using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients;

public class StoryblokDatasourceClientTest
{
    private readonly MockedWebApplicationFactory<MockedStartup> _factory = new();

    [Fact]
    public async Task Can_Load_Datasource()
    {
        _factory.CreateClient();
        var client = _factory.Services.GetRequiredService<StoryblokDatasourceClient>();

        var datasource = await client.Datasource("datasource-many-entries");
        Assert.NotNull(datasource);
        Assert.Equal(350, datasource?.Entries.Count());
    }

    [Fact]
    public async Task Can_Handle_Paged_Datasources()
    {
        _factory.CreateClient();
        var client = _factory.Services.GetRequiredService<StoryblokDatasourceClient>();
        client.PerPage = 10;

        var datasource = await client.Datasource("datasource-many-entries");
        Assert.NotNull(datasource);
        Assert.Equal(350, datasource?.Entries.Count());
    }
}
