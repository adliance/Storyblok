using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.FulltextSearch.Tests;

public class FulltextSearchTest
{
    private readonly FulltextSearchBase _sut;

    public FulltextSearchTest()
    {
        MockedWebApplicationFactory<MockedStartup> factory = new();
        factory.CreateClient();
        _sut = factory.Services.GetRequiredService<FulltextSearchBase>();
    }

    [Fact]
    public async Task Can_Fill_Index_And_Query()
    {
        _sut.DeleteFulltextIndex();
        
        var updatedPages = await _sut.UpdateFulltextIndex();
        Assert.NotNull(updatedPages);
        Assert.InRange(updatedPages.Value, 8, 8);

        var searchResults = _sut.Query("Content", 1);
        Assert.InRange(searchResults.TotalResults,2,2);
        Assert.InRange(searchResults.Results.Count, 1, 1);
    }
}