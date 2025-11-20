using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Adliance.Storyblok.Clients;

namespace Adliance.Storyblok.Extensions;

public interface IAsset
{
    string? Url { get; set; }
}

public static class AssetExtensions
{
    // please note that SVG is not supported in ImageService
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp"];

    public static string? ResizeByWidth(this IAsset image, int newWidth)
    {
        return image.IsSupported()
            ? image.Url + $"/m/{newWidth}x0"
            : image.Url;
    }

    public static string? ResizeByHeight(this IAsset image, int newHeight)
    {
        return image.IsSupported()
            ? image.Url + $"/m/0x{newHeight}"
            : image.Url;
    }

    public static string? Resize(this IAsset image, int newWidth, int newHeight, bool smart = true)
    {
        return image.IsSupported()
            ? image.Url + $"/m/{newWidth}x{newHeight}{(smart ? "/smart" : "")}"
            : image.Url;
    }

    public static string? FitIn(this IAsset image, int newWidth, int newHeight, string? fillWithColor = null)
    {
        if (!image.IsSupported()) return image.Url;

        if (!string.IsNullOrWhiteSpace(fillWithColor))
        {
            return $"/m/fit-in/{newWidth}x{newHeight}/filters:fill({fillWithColor})";
        }

        return $"/m/fit-in/{newWidth}x{newHeight}/filters:fill(transparent):format(png)";
    }

    public static bool IsSupported(this IAsset image)
    {
        if (string.IsNullOrWhiteSpace(image.Url)) return false;
        return ImageExtensions.Any(x => image.Url.EndsWith(x, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task EnsureSignedUrl(this IAsset image, StoryblokAssetClient assetClient)
    {
        var url = image.Url;
        if (string.IsNullOrWhiteSpace(url)) return;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            if (query["X-Amz-Signature"] != null) return; // if we already have a signed url, return it}
            image.Url = await assetClient.LoadSignedAssetUrl(url);
        }
    }

    public static async Task EnsureSignedUrl(this IAsset image, StoryblokAssetClient assetClient, Func<bool> requiresSignedUrlCallback)
    {
        await EnsureSignedUrl(image, assetClient, requiresSignedUrlCallback.Invoke());
    }
    
    public static async Task EnsureSignedUrl(this IAsset image, StoryblokAssetClient assetClient, bool requiresSignedUrl)
    {
        if (requiresSignedUrl) await EnsureSignedUrl(image, assetClient);
    }
}