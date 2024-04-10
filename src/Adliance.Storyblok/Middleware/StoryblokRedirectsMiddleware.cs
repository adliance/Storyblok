using System;
using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Middleware;

/// <summary>
/// Reads HTTP redirects (from/to) from a Storyblok datasource and redirects the HTTP request accordingly.
/// </summary>
public class StoryblokRedirectsMiddleware(IOptions<StoryblokOptions> options, ILogger<StoryblokRedirectsMiddleware> logger, RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext, StoryblokDatasourceClient datasourceClient)
    {
        if (string.IsNullOrWhiteSpace(options.Value.RedirectsDatasourceName))
        {
            logger.LogTrace("No redirects data source configured, RedirectsMiddleware is doing nothing.");
            await next(httpContext);
            return;
        }

        var configuredRedirects = await datasourceClient.Datasource(options.Value.RedirectsDatasourceName);
        if (configuredRedirects == null)
        {
            logger.LogWarning($"RedirectsMiddleware is configured for data source {options.Value.RedirectsDatasourceName}, but it seems not to exist.");
            await next(httpContext);
            return;
        }

        var path = httpContext.Request.Path;
        logger.LogTrace($"{configuredRedirects.Entries.Count()} redirects configured in data source {options.Value.RedirectsDatasourceName}.");

        var matchingRedirect = configuredRedirects.Entries.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name.Equals(path, StringComparison.OrdinalIgnoreCase));
        if (matchingRedirect is { Value: not null })
        {
            logger.LogDebug($"Redirecting from {path} to {matchingRedirect.Value}.");
            httpContext.Response.Redirect(matchingRedirect.Value, true);
            return;
        }

        await next(httpContext);
    }
}
