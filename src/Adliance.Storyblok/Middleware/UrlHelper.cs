using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Adliance.Storyblok.Middleware;

// this is a copy of https://github.com/aspnet/AspNetCore/blob/master/src/Mvc/Mvc.Core/src/Routing/EndpointRoutingUrlHelper.cs
// we need an UrlHelper for TagHelpers to work in our views that are being called by the Middleware
public class UrlHelper(ActionContext actionContext, LinkGenerator linkGenerator) : UrlHelperBase(actionContext)
{
    private readonly LinkGenerator _linkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));

    /// <inheritdoc />
    public override string Action(UrlActionContext urlActionContext)
    {
        ArgumentNullException.ThrowIfNull(urlActionContext);

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
            fragment: new FragmentString(urlActionContext.Fragment == null ? "" : "#" + urlActionContext.Fragment));
        return GenerateUrl(urlActionContext.Protocol, urlActionContext.Host, path) ?? "";
    }

    /// <inheritdoc />
    public override string RouteUrl(UrlRouteContext routeContext)
    {
        ArgumentNullException.ThrowIfNull(routeContext);

        var path = _linkGenerator.GetPathByRouteValues(
            ActionContext.HttpContext,
            routeContext.RouteName,
            routeContext.Values,
            fragment: new FragmentString(routeContext.Fragment == null ? "" : "#" + routeContext.Fragment));
        return GenerateUrl(routeContext.Protocol, routeContext.Host, path) ?? "";
    }
}
