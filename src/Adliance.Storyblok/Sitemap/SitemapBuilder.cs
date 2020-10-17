﻿using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.AspNetCore.Http;

namespace Adliance.Storyblok.Sitemap
{
    public class SitemapBuilder
    {
        private readonly StoryblokStoriesClient _client;
        private readonly IHttpContextAccessor? _httpContext;

        public SitemapBuilder(StoryblokStoriesClient client, IHttpContextAccessor? httpContext)
        {
            _client = client;
            _httpContext = httpContext;
        }

        public async Task<SitemapResult> Build()
        {
            var result = new SitemapResult();

            var stories = (await _client
                .Stories()
                .ForCurrentUiCulture()
                .ExcludingFields("content")
                .Load<StoryblokComponent>()).ToList();

            foreach (var s in stories)
            {
                result.Locations.Add(new SitemapResult.SitemapLocation(GetFullUrl(s.FullSlug), s.PublishedAt ?? s.CreatedAt));
            }

            return result;
        }

        public async Task<string> BuildXml()
        {
            return BuildXml(await Build());
        }

        public string BuildXml(SitemapResult sitemap)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            foreach (var l in sitemap.Locations)
            {
                sb.AppendLine("\t<url>");
                sb.AppendLine($"\t\t<loc>{l.Url}</loc>");
                sb.AppendLine($"\t\t<lastmod>{l.LastModified.ToUniversalTime():yyyy-MM-ddTHH:mm:sszzz}</lastmod>");
                sb.AppendLine($"\t\t<changefreq>{l.ChangeFrequency.ToString().ToLower()}</changefreq>");
                sb.AppendLine($"\t\t<priority>{l.Priority.ToString(CultureInfo.InvariantCulture)}</priority>");
                sb.AppendLine("\t</url>");
            }

            sb.AppendLine("</urlset>");
            return sb.ToString();
        }

        private string GetFullUrl(string? fullSlug)
        {
            var request = _httpContext?.HttpContext?.Request;
            if (request != null)
            {
                return string.Concat(
                    request.Scheme,
                    "://",
                    request.Host.ToUriComponent(),
                    "/",
                    fullSlug);
            }

            return "/" + fullSlug;
        }
    }
}