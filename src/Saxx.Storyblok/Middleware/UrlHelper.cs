using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Saxx.Storyblok.Middleware
{
    // this is a copy of https://github.com/aspnet/AspNetCore/blob/master/src/Mvc/Mvc.Core/src/Routing/EndpointRoutingUrlHelper.cs
    // we need an UrlHelper for TagHelpers to work in our views that are being called by the Middleware
    public class UrlHelper : UrlHelperBase
    {
        private readonly LinkGenerator _linkGenerator;


        public UrlHelper(ActionContext actionContext, LinkGenerator linkGenerator) : base(actionContext)
        {
            _linkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
        }

        /// <inheritdoc />
        public override string Action(UrlActionContext urlActionContext)
        {
            if (urlActionContext == null)
            {
                throw new ArgumentNullException(nameof(urlActionContext));
            }

            var values = GetValuesDictionary(urlActionContext.Values);

            if (urlActionContext.Action == null)
            {
                if (!values.ContainsKey("action") &&
                    AmbientValues.TryGetValue("action", out var action))
                {
                    values["action"] = action;
                }
            }
            else
            {
                values["action"] = urlActionContext.Action;
            }

            if (urlActionContext.Controller == null)
            {
                if (!values.ContainsKey("controller") &&
                    AmbientValues.TryGetValue("controller", out var controller))
                {
                    values["controller"] = controller;
                }
            }
            else
            {
                values["controller"] = urlActionContext.Controller;
            }


            var path = _linkGenerator.GetPathByRouteValues(
                ActionContext.HttpContext,
                routeName: null,
                values,
                fragment: new FragmentString(urlActionContext.Fragment == null ? null : "#" + urlActionContext.Fragment));
            return GenerateUrl(urlActionContext.Protocol, urlActionContext.Host, path);
        }

        /// <inheritdoc />
        public override string RouteUrl(UrlRouteContext routeContext)
        {
            if (routeContext == null)
            {
                throw new ArgumentNullException(nameof(routeContext));
            }

            var path = _linkGenerator.GetPathByRouteValues(
                ActionContext.HttpContext,
                routeContext.RouteName,
                routeContext.Values,
                fragment: new FragmentString(routeContext.Fragment == null ? null : "#" + routeContext.Fragment));
            return GenerateUrl(routeContext.Protocol, routeContext.Host, path);
        }
    }
}
