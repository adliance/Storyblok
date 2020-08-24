using System;
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
using Microsoft.Extensions.Options;
using Saxx.Storyblok.Clients;
using Saxx.Storyblok.Extensions;

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

        // ReSharper disable once UnusedMember.Global
        public async Task Invoke(HttpContext context, StoryblokStoryClient storyblokClient, IOptions<StoryblokOptions> options, ILogger<StoryblokMiddleware> logger)
        {
            var settings = options.Value;

            var slug = context.Request.Path.ToString();
            if (string.IsNullOrWhiteSpace(slug))
            {
                logger.LogTrace("Ignoring request, because no slug available.");
                await _next.Invoke(context);
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.SlugForClearingCache) && settings.SlugForClearingCache.Trim('/').Equals(slug.Trim('/'), StringComparison.InvariantCultureIgnoreCase))
            {
                storyblokClient.ClearCache();
                context.Response.StatusCode = (int) HttpStatusCode.OK;
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

            var isInStoryblokEditor = context.Request.Query.IsInStoryblokEditor(settings);
            if (isInStoryblokEditor)
            {
                // make sure we can display inside of the Storyblok iframe
                context.Response.Headers.Add("Content-Security-Policy", "frame-ancestors 'self' app.storyblok.com");
            }

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

            StoryblokStory? story = null;

            // special handling of Storyblok preview URLs that contain the language, like ~/de/home vs. ~/home
            // if we have such a URL, we also change the current culture accordingly
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            foreach (var supportedCulture in settings.SupportedCultures)
            {
                if (slug.StartsWith($"{supportedCulture}/", StringComparison.OrdinalIgnoreCase) || slug.Equals(supportedCulture, StringComparison.OrdinalIgnoreCase))
                {
                    var slugWithoutCulture = slug.Substring(supportedCulture.Length).Trim('/');

                    if (slugWithoutCulture.Equals("") && !string.IsNullOrWhiteSpace(settings.HandleRootWithSlug))
                    {
                        logger.LogTrace($"Swapping slug from \"{slug}\" to \"{settings.HandleRootWithSlug}\", because it's the root URL.");
                        slugWithoutCulture = settings.HandleRootWithSlug;
                    }

                    logger.LogTrace($"Trying to load story for slug \"{slugWithoutCulture}\" for culture {supportedCulture}.");
                    currentCulture = new CultureInfo(supportedCulture);
                    story = await storyblokClient.Story().WithCulture(currentCulture).WithSlug(slugWithoutCulture).Load();
                    break;
                }
            }

            // we don't have the language in the URL, so we force the default language
            if (story == null)
            {
                currentCulture = new CultureInfo(settings.SupportedCultures.First());
                story = await storyblokClient.Story().WithCulture(currentCulture).WithSlug(slug).Load();
            }

            // load the story with the current culture (usually set by request localization
            story ??= await storyblokClient.Story().WithCulture(CultureInfo.CurrentUICulture).WithSlug(slug).Load();

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
                logger.LogTrace($"Ignoring request, because no view specified on component of type \"{componentMapping.Type.FullName}\".");
                await _next.Invoke(context);
                return;
            }

            // we have a story, yay! Lets render it and stop with the middleware chain
            logger.LogTrace($"Rendering slug \"{slug}\" with view \"{componentMapping.View}\".");
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = currentCulture; // set the thread culture to match the story
            var result = new ViewResult {ViewName = componentMapping.View};
            var modelMetadata = new EmptyModelMetadataProvider();
            var modelDefinition = typeof(StoryblokStory<>).MakeGenericType(componentMapping.Type);
            var model = Activator.CreateInstance(modelDefinition, story);

            result.ViewData = new ViewDataDictionary(modelMetadata, new ModelStateDictionary())
            {
                Model = model
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