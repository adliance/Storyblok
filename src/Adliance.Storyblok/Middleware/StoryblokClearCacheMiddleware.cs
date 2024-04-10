using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Adliance.Storyblok.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
public class StoryblokClearCacheMiddleware(RequestDelegate next)
{
#pragma warning disable CA1823
    // ReSharper disable once UnusedMember.Local
    private readonly RequestDelegate _next = next;
#pragma warning restore CA1823

    public async Task Invoke(HttpContext context, IMemoryCache cache, ILogger<StoryblokClearCacheMiddleware> logger)
    {
        logger.LogTrace("Clearing cache ...");
        ClearCache(cache, logger);
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await context.Response.WriteAsync("Cache cleared.");
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
