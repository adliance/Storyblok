using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Adliance.Storyblok.Middleware
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StoryblokClearCacheMiddleware
    {
        private readonly RequestDelegate _next;

        public StoryblokClearCacheMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        // ReSharper disable once UnusedMember.Global
        public async Task Invoke(HttpContext context, IMemoryCache cache, ILogger<StoryblokClearCacheMiddleware> logger)
        {
            logger.LogTrace("Clearing cache ...");
            ClearCache(cache, logger);
            context.Response.StatusCode = (int) HttpStatusCode.OK;
            await context.Response.WriteAsync("Cache cleared.");
        }

        private void ClearCache(IMemoryCache cache, ILogger<StoryblokClearCacheMiddleware> logger)
        {
            try
            {
                // this is sloow, but I was not able to find any other way to clear the memory cache
                var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    // ReSharper disable PossibleNullReferenceException
                    var entriesCollection = field.GetValue(cache);
                    if (entriesCollection != null)
                    {
                        var clearMethod = entriesCollection.GetType().GetMethod("Clear");
                        if (clearMethod != null)
                        {
                            clearMethod.Invoke(entriesCollection, null);
                        }
                    }
                }
                // ReSharper restore PossibleNullReferenceException
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to clear cache.");
            }
        }
    }
}