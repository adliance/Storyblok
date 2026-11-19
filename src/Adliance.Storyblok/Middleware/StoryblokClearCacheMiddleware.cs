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

        // the story index is not part of the memory cache (an outdated index is kept while reloading), so it has to be cleared explicitly
        // after publishing, the old index might not contain new stories, so we don't keep it, but use the Storyblok API until the new index is loaded
        storyIndex.Clear();

        if (settings.UseStoryIndexForNotFound && !settings.IncludeDraftStories)
        {
            // load the story index in the background right away, so that it's available as soon as possible
            // we don't wait for it, the task never throws (errors are logged by the index)
            _ = storyIndex.Preload();
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
