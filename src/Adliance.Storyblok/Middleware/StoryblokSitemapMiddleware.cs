using System.Globalization;
using System.Threading.Tasks;
using Adliance.Storyblok.Sitemap;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Adliance.Storyblok.Middleware
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StoryblokSitemapMiddleware
    {
        private readonly RequestDelegate _next;

        public StoryblokSitemapMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // ReSharper disable once UnusedMember.Global
        public async Task Invoke(HttpContext context, SitemapBuilder sitemapBuilder, ILogger<StoryblokSitemapMiddleware> logger)
        {
            logger.LogTrace("Responding with sitemap XML ...");
            var xml = await sitemapBuilder.BuildXml();
            context.Response.ContentType = "application/xml";
            context.Response.Headers.Add("cache-control", $"public; max-age={(60 * 60 * 12).ToString(CultureInfo.InvariantCulture)}");
            await context.Response.WriteAsync(xml);
        }
    }
}