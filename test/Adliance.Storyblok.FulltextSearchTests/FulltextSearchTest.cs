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
    public async Task Can_Fill_Index_And_Query_In_English()
    {
        _sut.DeleteFulltextIndex("en");

        var updatedPages = await _sut.UpdateFulltextIndex("en");
        Assert.NotNull(updatedPages);
        Assert.InRange(updatedPages.Value, 8, 8);

        var searchResults = _sut.Query("en", "Content", 1);
        Assert.InRange(searchResults.TotalResults,2,2);
        Assert.InRange(searchResults.Results.Count, 1, 1);

        searchResults = _sut.Query("en", "Inhalt", 1);
        Assert.InRange(searchResults.TotalResults,0,0);
    }

    [Fact]
    public async Task Can_Fill_Index_And_Query_In_German()
    {
        _sut.DeleteFulltextIndex("de");

        var updatedPages = await _sut.UpdateFulltextIndex("de");
        Assert.NotNull(updatedPages);
        Assert.InRange(updatedPages.Value, 8, 8);

        var searchResults = _sut.Query("de", "Inhalt", 1);
        Assert.InRange(searchResults.TotalResults,1,1);
        Assert.InRange(searchResults.Results.Count, 1, 1);

        searchResults = _sut.Query("de", "Content", 1);
        Assert.InRange(searchResults.TotalResults,1,1);
    }
}
