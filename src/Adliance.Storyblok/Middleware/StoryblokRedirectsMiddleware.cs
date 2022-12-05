using System;
using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Middleware;

/// <summary>
/// Reads HTTP redirects (from/to) from a Storyblok datasource and redirects the HTTP request accordingly.
/// </summary>
public class StoryblokRedirectsMiddleware
{
    private readonly IOptions<StoryblokOptions> _options;
    private readonly RequestDelegate _next;

    public StoryblokRedirectsMiddleware(IOptions<StoryblokOptions> options, RequestDelegate next)
    {
        _options = options;
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext, StoryblokDatasourceClient datasourceClient)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.RedirectsDatasourceName))
        {
            await _next(httpContext);
            return;
        }

        var configuredRedirects = await datasourceClient.Datasource(_options.Value.RedirectsDatasourceName);

        var matchingRedirect = configuredRedirects?.Entries.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name.Equals(httpContext.Request.Path, StringComparison.OrdinalIgnoreCase));
        if (matchingRedirect is { Value: { } })
        {
            httpContext.Response.Redirect(matchingRedirect.Value, true);
            return;
        }

        await _next(httpContext);
    }
}