using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saxx.Storyblok.Extensions;
using Saxx.Storyblok.Settings;

namespace Saxx.Storyblok.Middleware
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StoryblokMiddleware
    {
        private readonly RequestDelegate _next;

        public StoryblokMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, StoryblokClient storyblokClient, StoryblokSettings settings, ILogger<StoryblokMiddleware> logger)
        {
            var slug = context.Request.Path.ToString();
            if (string.IsNullOrWhiteSpace(slug))
            {
                logger.LogTrace("Ignoring request, because no slug available.");
                await _next.Invoke(context);
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.SlugForClearingCache) && settings.SlugForClearingCache.Equals(slug.Trim('/'), StringComparison.InvariantCultureIgnoreCase))
            {
                storyblokClient.ClearCache();
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("Cache cleared.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.HandleRootWithSlug) && slug.Equals("/", StringComparison.InvariantCultureIgnoreCase))
            {
                logger.LogTrace($"Swapping slug from \"{slug}\" to \"{settings.HandleRootWithSlug}\", because it's the root URL.");
                slug = settings.HandleRootWithSlug;
            }
            else if (slug.Equals("/", StringComparison.InvariantCultureIgnoreCase))
            {
                // we are on the root path, and we shouldn't handle it - so we bail out
                logger.LogTrace("Ignoring request, because it's the root URL which is configured to be ignored.");
                await _next.Invoke(context);
                return;
            }

            slug = slug.Trim('/');


            if (settings.IgnoreSlugs != null)
            {
                if (settings.IgnoreSlugs.Any(x => slug.Equals(x.Trim('/'), StringComparison.InvariantCultureIgnoreCase)))
                {
                    // don't handle this slug in the middleware, because exact match of URL
                    logger.LogTrace($"Ignoring request \"{slug}\", because it's configured to be ignored (exact match).");
                    await _next.Invoke(context);
                    return;
                }
                if (settings.IgnoreSlugs.Any(x => x.EndsWith("*", StringComparison.InvariantCultureIgnoreCase) && slug.StartsWith(x.TrimEnd('*').Trim('/'), StringComparison.InvariantCultureIgnoreCase)))
                {
                    // don't handle this slug in the middleware, because the configuration ends with a *, which means we compare via StartsWith
                    logger.LogTrace($"Ignoring request \"{slug}\", because it's configured to be ignored (partial match).");
                    await _next.Invoke(context);
                    return;
                }
            }

            StoryblokStory story = null;
            var cultureMappings = settings.CultureMappings ?? new Dictionary<CultureInfo, CultureInfo>();

            // special handling of Storyblok preview URLs that contain the language, like ~/de/home vs. ~/home
            // if we have such a URL, we also change the current culture accordingly
            foreach (var cultureMapping in cultureMappings)
            {
                if (slug.StartsWith($"/{cultureMapping.Value}/"))
                {
                    var slugWithoutCulture = slug.Substring(cultureMapping.Value.ToString().Length + 2);
                    logger.LogTrace($"Trying to load story for slug \"{slugWithoutCulture}\" for culture {cultureMapping.Value}.");
                    CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureMapping.Value;
                    story = await storyblokClient.Story().WithCulture(cultureMapping.Value).WithSlug(slugWithoutCulture).Load();
                    break;
                }
            }

            // we're in the editor, but we don't have the language in the URL
            // so we force the default language
            if (story == null && context.Request.Query.IsInStoryblokEditor(settings))
            {
                var defaultCulture = settings.DefaultCulture ?? CultureInfo.CurrentUICulture;
                CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = defaultCulture;
                story = await storyblokClient.Story().WithCulture(defaultCulture).WithSlug(slug).Load();
            }

            // load the story with the current culture (usually set by request localization
            if (story == null)
            {
                story = await storyblokClient.Story().WithCulture(CultureInfo.CurrentUICulture).WithSlug(slug).Load();
            }

            // that's not a story, lets continue down the middleware chain
            if (story == null)
            {
                logger.LogTrace("Ignoring request, because no matching story found.");
                await _next.Invoke(context);
                return;
            }

            var componentName = story.Content.Component;
            var componentMappings = StoryblokMappings.Mappings;
            if (!componentMappings.ContainsKey(componentName))
            {
                throw new Exception($"No component mapping found for a component '{componentName}'.");
            }

            var componentMapping = componentMappings[componentName];
            if (string.IsNullOrWhiteSpace(componentMapping.View))
            {
                throw new Exception($"No view specified on component of type '{componentMapping.Type.FullName}'.");
            }

            // we have a story, yay! Lets render it and stop with the middleware chain
            logger.LogTrace($"Rendering slug \"{slug}\" with view \"{componentMapping.View}\".");
            var result = new ViewResult { ViewName = componentMapping.View };
            var modelMetadata = new EmptyModelMetadataProvider();
            result.ViewData = new ViewDataDictionary(modelMetadata, new ModelStateDictionary())
            {
                Model = story
            };
            await WriteResultAsync(context, result);
        }

        private static readonly RouteData EmptyRouteData = new RouteData();
        private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

        private static Task WriteResultAsync<TResult>(HttpContext context, TResult result) where TResult : IActionResult
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = context.RequestServices.GetRequiredService<IActionResultExecutor<TResult>>();
            if (executor == null)
            {
                throw new InvalidOperationException($"No result executor for '{typeof(TResult).FullName}' has been registered.");
            }

            var routeData = context.GetRouteData() ?? EmptyRouteData;
            var actionContext = new ActionContext(context, routeData, EmptyActionDescriptor);

            // support the injection of an ActionContext via IActionContextAccessor in views generated by the middleware
            var actionContextAccessor = context.RequestServices.GetService<IActionContextAccessor>();
            if (actionContextAccessor != null && actionContextAccessor.ActionContext == null)
            {
                actionContextAccessor.ActionContext = actionContext;
            }

            // support the UrlHelper in views generated by the middleware
            if (!context.Items.TryGetValue(typeof(IUrlHelper), out _))
            {
                var linkGenerator = context.RequestServices.GetRequiredService<LinkGenerator>();
                context.Items.Add(typeof(IUrlHelper), new UrlHelper(actionContext, linkGenerator));
            }

            return executor.ExecuteAsync(actionContext, result);
        }
    }
}
