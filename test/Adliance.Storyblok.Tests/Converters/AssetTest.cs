using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.Tests.Converters;

public class AssetTest
{
    private readonly StoryblokStoryClient _client;

    public AssetTest()
    {
        MockedWebApplicationFactory<MockedStartup> factory = new();
        factory.CreateClient();
        _client = factory.Services.GetRequiredService<StoryblokStoryClient>();
    }

    [Fact]
    public async Task Can_Load_Story_Without_Resolved_Assets()
    {
        var story = await _client.Story().WithSlug("/page-asset").Load<PageComponent>();
        var image = story?.Content?.Content?.First() as ImageComponent;
        Assert.NotNull(image);
        Assert.NotNull(image.Asset);
        Assert.Null(image.Asset?.Original);
        Assert.Equal("Original ALT Text", image.Asset?.Alt);
    }

    [Fact]
    public async Task Can_Load_Assets_Without_Assigned_File()
    {
        var story = await _client.Story().WithSlug("/page-asset").Load<PageComponent>();
        // we have two images on the page - the first image has a file assigned, the second one does not
        var firstImage = story?.Content?.Content?.First() as ImageComponent;
        var secondImage = story?.Content?.Content?.Skip(1).First() as ImageComponent;
        var thirdImage = story?.Content?.Content?.Skip(2).First() as ImageComponent;
        Assert.NotNull(firstImage);
        Assert.NotNull(firstImage.Asset);
        Assert.NotNull(secondImage);
        Assert.NotNull(secondImage.Asset);
        Assert.NotNull(thirdImage);
        Assert.Null(thirdImage.Asset);
    }

    [Fact(Skip = "Only works in Premium Plans :(")]
    public async Task Can_Load_Story_With_Resolved_Assets()
    {
        var story = await _client.Story().WithSlug("/page-asset").ResolveAssets().Load<PageComponent>();
        var image = story?.Content?.Content?.First() as ImageComponent;
        Assert.NotNull(image);
        Assert.NotNull(image.Asset);
        Assert.NotNull(image.Asset?.Original);
        Assert.Equal("Original ALT Text", image.Asset?.Alt);
        Assert.Equal("Updated ALT Text", image.Asset?.Original?.Alt);
    }
}
