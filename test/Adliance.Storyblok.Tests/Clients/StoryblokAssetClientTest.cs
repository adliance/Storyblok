using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients;

public class StoryblokAssetClientTest
{
    private readonly StoryblokAssetClient _client;

    public StoryblokAssetClientTest()
    {
        MockedWebApplicationFactory<MockedStartup> factory = new();
        factory.CreateClient();
        _client = factory.Services.GetRequiredService<StoryblokAssetClient>();
    }

    [Fact]
    public async Task Can_Load_Signed_Url_for_Private_Asset()
    {
        var assetUrl = "https://a.storyblok.com/f/114417/299x168/dd3573d706/private-asset.jpeg";
        var signedAssetUrl = await _client.LoadSignedAssetUrl(assetUrl);
        Assert.NotEmpty(signedAssetUrl ?? "");
    }

    [Fact]
    public async Task Can_Load_Private_Asset()
    {
        var assetUrl = "https://a.storyblok.com/f/114417/299x168/dd3573d706/private-asset.jpeg";
        var bytes = await _client.LoadAsset(assetUrl);
        Assert.InRange(bytes?.ContentLength ?? 0, 10_000, 15_000);
    }
    
    [Fact]
    public async Task Can_Load_Private_Asset_Cached()
    {
        var assetUrl = "https://a.storyblok.com/f/114417/299x168/dd3573d706/private-asset.jpeg";
        var bytes = await _client.LoadAsset(assetUrl);
        Assert.InRange(bytes?.ContentLength ?? 0, 10_000, 15_000);
        
        bytes = await _client.LoadAsset(assetUrl);
        Assert.InRange(bytes?.ContentLength ?? 0, 10_000, 15_000);
    }
    
    [Fact]
    public async Task Can_Load_Public_Asset()
    {
        var assetUrl = "https://a.storyblok.com/f/114417/200x200/0093d54a2a/technologieplauscherl.jpg";
        var bytes = await _client.LoadAsset(assetUrl);
        Assert.InRange(bytes?.ContentLength ?? 0, 3000, 5_000);
    }
    
    [Fact]
    public async Task Can_Load_Signed_Url_for_Public_Asset()
    {
        var assetUrl = "https://a.storyblok.com/f/114417/200x200/0093d54a2a/technologieplauscherl.jpg";
        var signedAssetUrl = await _client.LoadSignedAssetUrl(assetUrl);
        Assert.NotEmpty(signedAssetUrl ?? "");
    }
}