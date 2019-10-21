using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saxx.Storyblok.Extensions;
using Saxx.Storyblok.Settings;

namespace Saxx.Storyblok.Middleware
{
    public class StoryblokMiddleware
    {
        private readonly RequestDelegate _next;

        public StoryblokMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, StoryblokClient storyblokClient, StoryblokSettings settings, IUrlHelperFactory urlHelperFactory)
        {
            var slug = context.Request.Path.ToString();
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new Exception("No slug available.");
            }

            if (!string.IsNullOrWhiteSpace(settings.HandleRootWithSlug) && slug.Equals("/", StringComparison.InvariantCultureIgnoreCase))
            {
                slug = settings.HandleRootWithSlug;
            }
            else if (slug.Equals("/", StringComparison.InvariantCultureIgnoreCase))
            {
                // we are on the root path, and we shouldn't handle it - so we bail out
                await _next.Invoke(context);
                return;
            }

            StoryblokStory story = null;
            var cultureMappings = settings.CultureMappings ?? new Dictionary<CultureInfo, CultureInfo>();

            // special handling of Storyblok preview URLs that contain the language, like ~/de/home vs. ~/home
            // if we have such a URL, we also change the current culture accordingly
            foreach (var cultureMapping in cultureMappings)
            {
                if (slug.StartsWith($"/{cultureMapping.Value}/"))
                {
                    CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = cultureMapping.Value;
                    story = await storyblokClient.LoadStory(cultureMapping.Value, slug.Substring(cultureMapping.Value.ToString().Length + 2));
                    break;
                }
            }

            // we're in the editor, but we don't have the language in the URL
            // so we force the default language
            if (story == null && context.Request.Query.IsInStoryblokEditor(settings))
            {
                var defaultCulture = settings.DefaultCulture ?? CultureInfo.CurrentUICulture;
                CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = defaultCulture;
                story = await storyblokClient.LoadStory(defaultCulture, slug);
            }

            // load the story with the current culture (usually set by request localization
            if (story == null)
            {
                story = await storyblokClient.LoadStory(CultureInfo.CurrentUICulture, slug);
            }

            // that's not a story, lets continue down the middleware chain
            if (story == null)
            {
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
            var result = new ViewResult { ViewName = componentMapping.View };
            var modelMetadata = new EmptyModelMetadataProvider();
            result.ViewData = new ViewDataDictionary(modelMetadata, new ModelStateDictionary())
            {
                Model = story
            };
            await WriteResultAsync(context, result, urlHelperFactory);
        }

        private static readonly RouteData EmptyRouteData = new RouteData();
        private static readonly ActionDescriptor EmptyActionDescriptor = new ActionDescriptor();

        public static Task WriteResultAsync<TResult>(HttpContext context, TResult result, IUrlHelperFactory urlHelperFactory) where TResult : IActionResult
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

            if (!context.Items.TryGetValue(typeof(IUrlHelper), out _))
            {
                var linkGenerator = context.RequestServices.GetRequiredService<LinkGenerator>();
                context.Items.Add(typeof(IUrlHelper), new UrlHelper(actionContext, linkGenerator));
            }

            return executor.ExecuteAsync(actionContext, result);
        }
    }
}
