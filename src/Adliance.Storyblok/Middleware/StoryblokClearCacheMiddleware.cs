using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
public class StoryblokClearCacheMiddleware(RequestDelegate next)
{
#pragma warning disable CA1823
    // ReSharper disable once UnusedMember.Local
    private readonly RequestDelegate _next = next;
#pragma warning restore CA1823

    public async Task Invoke(HttpContext context, IMemoryCache cache, IOptions<StoryblokOptions> options, StoryblokStoryIndex storyIndex, ILogger<StoryblokClearCacheMiddleware> logger)
    {
        var settings = options.Value;
        if (!IsAllowed(context.Connection.RemoteIpAddress, settings, logger))
        {
            logger.LogWarning($"Not clearing cache, because IP address \"{context.Connection.RemoteIpAddress}\" is not allowed.");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return;
        }

        logger.LogTrace("Clearing cache ...");
        ClearCache(cache, logger);

        if (settings.UseStoryIndexForNotFound && !settings.IncludeDraftStories)
        {
            // pre-warm the story index, so that the first visitor after publishing does not have to wait for loading the full story list
            _ = Task.Run(async () =>
            {
                try
                {
                    await storyIndex.Preload();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Unable to preload story index.");
                }
            });
        }

        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await context.Response.WriteAsync("Cache cleared.");
    }

    private static bool IsAllowed(IPAddress? remoteIpAddress, StoryblokOptions settings, ILogger logger)
    {
        var allowed = settings.ClearCacheAllowedIpAddresses.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (!allowed.Any())
        {
            return true;
        }

        if (remoteIpAddress == null)
        {
            return false;
        }

        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            remoteIpAddress = remoteIpAddress.MapToIPv4();
        }

        foreach (var entry in allowed)
        {
            if (entry.Contains('/'))
            {
                if (IPNetwork.TryParse(entry.Trim(), out var network))
                {
                    if (network.Contains(remoteIpAddress))
                    {
                        return true;
                    }
                }
                else
                {
                    logger.LogWarning($"Invalid network \"{entry}\" in {nameof(StoryblokOptions.ClearCacheAllowedIpAddresses)}.");
                }
            }
            else if (IPAddress.TryParse(entry.Trim(), out var address))
            {
                if (address.IsIPv4MappedToIPv6)
                {
                    address = address.MapToIPv4();
                }

                if (address.Equals(remoteIpAddress))
                {
                    return true;
                }
            }
            else
            {
                logger.LogWarning($"Invalid IP address \"{entry}\" in {nameof(StoryblokOptions.ClearCacheAllowedIpAddresses)}.");
            }
        }

        return false;
    }

    private static void ClearCache(IMemoryCache cache, ILogger<StoryblokClearCacheMiddleware> logger)
    {
        try
        {
            if (cache is MemoryCache memoryCache)
            {
                memoryCache.Compact(1.0);
            }
            else
            {
                throw new Exception("Only MemoryCache supported currently.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to clear cache.");
        }
    }
}
