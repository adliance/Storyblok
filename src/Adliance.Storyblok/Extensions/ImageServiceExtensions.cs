using System;
using System.Linq;

namespace Adliance.Storyblok.Extensions;

public interface IImageService
{
    string? Url { get; }
}

public static class ImageServiceExtensions
{
    // please note that SVG is not supported in ImageService
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp"];

    public static string? ResizeByWidth(this IImageService image, int newWidth)
    {
        return image.IsSupported()
            ? image.Url + $"/m/{newWidth}x0"
            : image.Url;
    }

    public static string? ResizeByHeight(this IImageService image, int newHeight)
    {
        return image.IsSupported()
            ? image.Url + $"/m/0x{newHeight}"
            : image.Url;
    }

    public static string? Resize(this IImageService image, int newWidth, int newHeight, bool smart = true)
    {
        return image.IsSupported()
            ? image.Url + $"/m/{newWidth}x{newHeight}{(smart ? "/smart" : "")}"
            : image.Url;
    }

    public static string? FitIn(this IImageService image, int newWidth, int newHeight, string? fillWithColor = null)
    {
        if (!image.IsSupported()) return image.Url;

        if (!string.IsNullOrWhiteSpace(fillWithColor))
        {
            return $"/m/fit-in/{newWidth}x{newHeight}/filters:fill({fillWithColor})";
        }

        return $"/m/fit-in/{newWidth}x{newHeight}/filters:fill(transparent):format(png)";
    }

    public static bool IsSupported(this IImageService image)
    {
        if (string.IsNullOrWhiteSpace(image.Url)) return false;
        return ImageExtensions.Any(x => image.Url.EndsWith(x, StringComparison.OrdinalIgnoreCase));
    }
}
