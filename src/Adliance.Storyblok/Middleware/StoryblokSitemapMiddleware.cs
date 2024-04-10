using System.Globalization;
using System.Threading.Tasks;
using Adliance.Storyblok.Sitemap;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Adliance.Storyblok.Middleware;

// ReSharper disable once ClassNeverInstantiated.Global
public class StoryblokSitemapMiddleware(RequestDelegate next)
{
#pragma warning disable CA1823
    // ReSharper disable once UnusedMember.Local
    private readonly RequestDelegate _next = next;
#pragma warning restore CA1823

    // ReSharper disable once UnusedMember.Global
    public async Task Invoke(HttpContext context, SitemapBuilder sitemapBuilder, ILogger<StoryblokSitemapMiddleware> logger)
    {
        logger.LogTrace("Responding with sitemap XML ...");
        var xml = await sitemapBuilder.BuildXml();
        context.Response.ContentType = "application/xml";
        context.Response.Headers.Append("cache-control", $"public; max-age={(60 * 60 * 12).ToString(CultureInfo.InvariantCulture)}");
        await context.Response.WriteAsync(xml);
    }
}
