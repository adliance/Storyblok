using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Adliance.Storyblok.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Clients;

public class StoryblokAssetClient(
    IOptions<StoryblokOptions> settings,
    IHttpClientFactory clientFactory,
    IHttpContextAccessor? httpContext,
    IMemoryCache memoryCache,
    ILogger<StoryblokBaseClient> logger)
    : StoryblokBaseClient(settings, clientFactory, httpContext, memoryCache, logger)
{
    public async Task<StoryblokAssetOriginal?> LoadAsset(string? assetUrl)
    {
        if (string.IsNullOrWhiteSpace(assetUrl)) return null;
        if (string.IsNullOrWhiteSpace(Settings.AssetKey)) throw new Exception("No Asset Key configured.");

        var response = await Client.GetAsync($"https://api.storyblok.com/v2/cdn/assets/me?token={Settings.AssetKey}&filename={assetUrl}");
        var responseString = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<StoryblokAssetContainer>(responseString)?.Asset;
    }

    public async Task<string?> LoadSignedAssetUrl(IImageService? asset)
    {
        return await LoadSignedAssetUrl(asset?.Url);
    }

    public async Task<string?> LoadSignedAssetUrl(string? assetUrl)
    {
        var asset = await LoadAsset(assetUrl);
        return asset?.SignedUrl;
    }

    public async Task<byte[]?> LoadSignedAssetBytes(IImageService? asset)
    {
        return await LoadSignedAssetBytes(asset?.Url);
    }

    public async Task<byte[]?> LoadSignedAssetBytes(string? assetUrl)
    {
        var signedAssetUrl = await LoadSignedAssetUrl(assetUrl);
        if (string.IsNullOrWhiteSpace(signedAssetUrl)) return null;

        var bytes = await Client.GetByteArrayAsync(signedAssetUrl);
        return bytes;
    }
}